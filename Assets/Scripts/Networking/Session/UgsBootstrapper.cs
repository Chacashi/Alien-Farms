using System;
using System.Threading.Tasks;
using AlienFarms.Core;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Initializes Unity Gaming Services (environment from <c>NetworkSettings</c>, one Authentication profile per
    /// Multiplayer Play Mode instance, see <see cref="UgsProfileResolver"/>) and signs in anonymously. Idempotent:
    /// concurrent calls share the pending attempt and a signed-in player returns at once; after a failure the next call
    /// retries. Plain class owned by <see cref="OnlineSessionService"/>; the UGS services themselves are global.
    /// </summary>
    public sealed class UgsBootstrapper
    {
        // Authentication's own default profile name.
        private const string DefaultProfile = "default";

        private readonly string _environmentName;
        private readonly string _profile;

        private Task<bool> _pending;

        public UgsBootstrapper(string environmentName, string profile)
        {
            _environmentName = string.IsNullOrWhiteSpace(environmentName) ? null : environmentName.Trim();
            _profile = profile;
        }

        /// <summary>Authentication profile used by this process (null = default profile).</summary>
        public string Profile => _profile;

        /// <summary>Error key (<see cref="OnlineSessionErrorKeys"/>) of the last failed attempt; null after a success.</summary>
        public string LastErrorKey { get; private set; }

        public bool IsSignedIn => Authentication != null && Authentication.IsSignedIn;

        /// <summary>Authentication service once the services are initialized (null before).</summary>
        public IAuthenticationService Authentication => UnityServices.State == ServicesInitializationState.Initialized
            ? AuthenticationService.Instance
            : null;

        /// <summary>Unity Authentication player id (null until signed in).</summary>
        public string PlayerId => IsSignedIn ? AuthenticationService.Instance.PlayerId : null;

        /// <summary>Profile for this process from its command line and the MPPM main-editor flag.</summary>
        public static string ResolveProcessProfile()
        {
            bool isMainEditor = true;
#if UNITY_EDITOR
            isMainEditor = Unity.Multiplayer.PlayMode.CurrentPlayer.IsMainEditor;
#endif
            return UgsProfileResolver.Resolve(Environment.GetCommandLineArgs(), isMainEditor);
        }

        /// <summary>Initializes the services if needed and signs in. True when signed in.</summary>
        public Task<bool> EnsureSignedInAsync()
        {
            if (IsSignedIn)
            {
                LastErrorKey = null;
                return Task.FromResult(true);
            }

            if (_pending == null || _pending.IsCompleted)
            {
                _pending = SignInAsync();
            }

            return _pending;
        }

        private async Task<bool> SignInAsync()
        {
            LastErrorKey = null;
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    var options = new InitializationOptions();
                    if (_environmentName != null)
                    {
                        options.SetEnvironmentName(_environmentName);
                    }

                    if (_profile != null)
                    {
                        options.SetProfile(_profile);
                    }

                    await UnityServices.InitializeAsync(options);
                }

                IAuthenticationService auth = AuthenticationService.Instance;
                if (auth == null)
                {
                    return Fail(OnlineSessionErrorKeys.ServicesUnavailable, "Authentication service is not available.");
                }

                // Services initialized earlier (another play session without domain reload) may use another profile.
                string expectedProfile = _profile ?? DefaultProfile;
                if (!auth.IsSignedIn && !string.Equals(auth.Profile, expectedProfile, StringComparison.Ordinal))
                {
                    auth.SwitchProfile(expectedProfile);
                }

                if (!auth.IsSignedIn)
                {
                    await auth.SignInAnonymouslyAsync();
                }

                Debug.Log($"[{nameof(UgsBootstrapper)}] Signed in as {auth.PlayerId} (profile '{auth.Profile}', environment '{_environmentName ?? "default"}').");
                return true;
            }
            catch (Exception exception)
            {
                string key = OnlineSessionErrorMapper.FromException(exception, OnlineSessionErrorKeys.SignInFailed) ?? OnlineSessionErrorKeys.SignInFailed;
                return Fail(key, exception.Message);
            }
        }

        private bool Fail(string errorKey, string message)
        {
            LastErrorKey = errorKey;
            Debug.LogWarning($"[{nameof(UgsBootstrapper)}] Sign-in failed ({errorKey}): {message}");
            return false;
        }
    }
}
