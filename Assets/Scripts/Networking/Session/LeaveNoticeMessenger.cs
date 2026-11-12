using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Voluntary-leave notice over an NGO named message: a pure client calls <see cref="SendLeavingNotice"/> right before
    /// it shuts down, so the server frees its seat at once (<see cref="ConnectionApprovalHandler.NotifyLeaving"/>) instead
    /// of holding it for the reconnection grace. Best-effort: NGO flushes its send queue before closing; if the notice is
    /// lost, the session's PlayerHasLeft (<see cref="ConnectionApprovalHandler.ReleaseLeftMember"/>) or the grace frees
    /// the seat. The handler is registered only on the server (clients just send), when it starts, and removed when it stops.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LeaveNoticeMessenger : MonoBehaviour
    {
        /// <summary>Named message a client sends to the server when it leaves on purpose (no body besides a version byte).</summary>
        public const string LeavingMessageName = "AF.Leaving";

        // Body: one version byte, so the message is never empty and can grow later.
        private const byte MessageVersion = 1;
        private const int MessageSize = sizeof(byte);

        [Header("References")]
        [SerializeField, Tooltip("NetworkManager on this root.")]
        private NetworkManager _manager;

        [SerializeField, Tooltip("Connection approval on this root: frees the seat of the client that sent the notice.")]
        private ConnectionApprovalHandler _approval;

        private CustomMessagingManager _registeredOn;

        private void OnEnable()
        {
            if (_manager == null || _approval == null)
            {
                Debug.LogError($"[{nameof(LeaveNoticeMessenger)}] Missing NetworkManager or ConnectionApprovalHandler on '{name}'; leave notices disabled.", this);
                return;
            }

            _manager.OnServerStarted += HandleServerStarted;
            _manager.OnServerStopped += HandleServerStopped;

            // Enabled while a server already runs (e.g. component toggled in Play Mode).
            if (_manager.IsListening && _manager.IsServer)
            {
                Register();
            }
        }

        private void OnDisable()
        {
            if (_manager != null)
            {
                _manager.OnServerStarted -= HandleServerStarted;
                _manager.OnServerStopped -= HandleServerStopped;
            }

            Unregister();
        }

        /// <summary>
        /// Pure client: tells the server this player is leaving on purpose. False when not connected as a pure client
        /// (host, offline, shutting down).
        /// </summary>
        public bool SendLeavingNotice()
        {
            if (_manager == null || !_manager.IsConnectedClient || _manager.IsServer || _manager.ShutdownInProgress
                || _manager.CustomMessagingManager == null)
            {
                return false;
            }

            var writer = new FastBufferWriter(MessageSize, Allocator.Temp);
            try
            {
                writer.WriteValueSafe(MessageVersion);
                _manager.CustomMessagingManager.SendNamedMessage(LeavingMessageName, NetworkManager.ServerClientId, writer, NetworkDelivery.ReliableSequenced);
            }
            finally
            {
                writer.Dispose();
            }

            Debug.Log($"[{nameof(LeaveNoticeMessenger)}] Leave notice sent to the server.", this);
            return true;
        }

        private void HandleServerStarted()
        {
            Register();
        }

        private void HandleServerStopped(bool wasHost)
        {
            Unregister();
        }

        private void Register()
        {
            CustomMessagingManager messaging = _manager.CustomMessagingManager;
            if (messaging == null || _registeredOn == messaging)
            {
                return;
            }

            Unregister();
            messaging.RegisterNamedMessageHandler(LeavingMessageName, HandleLeavingMessage);
            _registeredOn = messaging;
        }

        private void Unregister()
        {
            CustomMessagingManager messaging = _registeredOn;
            _registeredOn = null;
            if (messaging != null)
            {
                messaging.UnregisterNamedMessageHandler(LeavingMessageName);
            }
        }

        // Server: the body is not needed (the sender id is what matters); its content is not trusted.
        private void HandleLeavingMessage(ulong senderClientId, FastBufferReader reader)
        {
            if (_manager == null || !_manager.IsServer || senderClientId == NetworkManager.ServerClientId)
            {
                return;
            }

            _approval.NotifyLeaving(senderClientId);
        }

        private void OnValidate()
        {
            if (_manager == null)
            {
                TryGetComponent(out _manager);
            }

            if (_approval == null)
            {
                TryGetComponent(out _approval);
            }
        }
    }
}
