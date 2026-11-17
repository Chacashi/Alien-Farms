using System;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Workarounds for com.unity.services.multiplayer 2.4.1. <c>ISession.LeaveAsync()</c> first unsubscribes from the
    /// lobby's deleted / kicked events and then removes the player; when the lobby is already gone that call throws
    /// (404) before the session reaches Disconnected or raises <c>RemovedFromSession</c>, and the SDK only drops a
    /// session from <see cref="IMultiplayerService.Sessions"/> on those events. The orphan keeps its session type
    /// registered, so every later create / join is refused as "already registered". <see cref="ReleaseOrphanAsync"/>
    /// raises the session's own (private) removal handler, which makes the SDK release its local state and the
    /// registration exactly as if the player had been kicked. If the private API is missing (another SDK version) it
    /// logs one warning and does nothing. Remove once the SDK fixes LeaveAsync.
    /// </summary>
    internal static class SessionSdkWorkarounds
    {
        /// <summary>Full name of the SDK's (internal) ISession implementation that owns the removal handler.</summary>
        internal const string SessionHandlerTypeName = "Unity.Services.Multiplayer.SessionHandler";

        // Private SessionHandler method subscribed to the lobby's KickedFromLobby event (SDK 2.4.1).
        private const string RemovedHandlerName = "OnRemovedFromSession";
        private const int PollMilliseconds = 100;

        private static MethodInfo s_removedHandler;
        private static Type s_removedHandlerOwner;
        private static bool s_warned;

        /// <summary>
        /// True when the installed SDK still has the private API the workaround relies on (the SessionHandler type and
        /// its parameterless instance <c>OnRemovedFromSession()</c>). Covered by an EditMode test so an SDK update fails
        /// loudly instead of silently disabling the workaround.
        /// </summary>
        internal static bool IsSupported
        {
            get
            {
                Type handlerType = typeof(ISession).Assembly.GetType(SessionHandlerTypeName, false);
                return handlerType != null && typeof(ISession).IsAssignableFrom(handlerType) && FindRemovedHandler(handlerType) != null;
            }
        }

        /// <summary>True while <paramref name="session"/> is the SDK's registered session of its type.</summary>
        public static bool IsRegistered(ISession session)
        {
            return session != null && ReferenceEquals(GetRegistered(session.Type), session);
        }

        /// <summary>The SDK's registered session of <paramref name="sessionType"/> (null when none or no service).</summary>
        public static ISession GetRegistered(string sessionType)
        {
            IMultiplayerService service = MultiplayerService.Instance;
            if (service == null || service.Sessions == null || string.IsNullOrEmpty(sessionType))
            {
                return null;
            }

            return service.Sessions.TryGetValue(sessionType, out ISession registered) ? registered : null;
        }

        /// <summary>Waits until <paramref name="session"/> is no longer registered. False on timeout.</summary>
        public static async Task<bool> WaitUntilUnregisteredAsync(ISession session, float timeoutSeconds)
        {
            float deadline = Time.realtimeSinceStartup + Mathf.Max(0f, timeoutSeconds);
            while (IsRegistered(session))
            {
                if (!Application.isPlaying || Time.realtimeSinceStartup >= deadline)
                {
                    return false;
                }

                await Task.Delay(PollMilliseconds);
            }

            return true;
        }

        /// <summary>
        /// Frees the session type held by an orphaned <paramref name="session"/> (see the class remarks) and waits up
        /// to <paramref name="timeoutSeconds"/> for the SDK to unregister it. True when it is no longer registered.
        /// Never throws.
        /// </summary>
        public static async Task<bool> ReleaseOrphanAsync(ISession session, float timeoutSeconds)
        {
            if (!IsRegistered(session))
            {
                return true;
            }

            // Outside Play Mode (e.g. an end finishing after exiting) the SDK is being torn down: leave it alone.
            if (!Application.isPlaying)
            {
                return false;
            }

            MethodInfo handler = ResolveRemovedHandler(session.GetType());
            if (handler == null)
            {
                return false;
            }

            try
            {
                Debug.LogWarning($"[{nameof(SessionSdkWorkarounds)}] Releasing session '{session.Type}' still registered in the SDK after a leave / drop (host left or connection lost) (SDK 2.4.1 workaround).");
                handler.Invoke(session, null);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[{nameof(SessionSdkWorkarounds)}] Releasing the orphaned session failed: {exception.GetBaseException().Message}");
                return false;
            }

            // The SDK cleans up asynchronously before removing the registration.
            return await WaitUntilUnregisteredAsync(session, timeoutSeconds);
        }

        private static MethodInfo ResolveRemovedHandler(Type sessionType)
        {
            if (s_removedHandlerOwner == sessionType)
            {
                return s_removedHandler;
            }

            s_removedHandlerOwner = sessionType;
            s_removedHandler = FindRemovedHandler(sessionType);

            if (s_removedHandler == null && !s_warned)
            {
                s_warned = true;
                Debug.LogWarning($"[{nameof(SessionSdkWorkarounds)}] '{sessionType.FullName}.{RemovedHandlerName}()' not found: the orphaned-session workaround is disabled for this SDK version.");
            }

            return s_removedHandler;
        }

        private static MethodInfo FindRemovedHandler(Type sessionType)
        {
            return sessionType.GetMethod(
                RemovedHandlerName,
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_warned = false;
        }
    }
}
