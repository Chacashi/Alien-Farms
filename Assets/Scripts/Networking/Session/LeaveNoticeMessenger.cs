using System;
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
    /// <para>The other way round, a host leaving on purpose sends <see cref="SendHostLeavingNotice"/> to every client while
    /// their connections are still open (reliable, so it is resent until delivered), and only disconnects them after a
    /// short wait (<see cref="NetworkRootLifetime.ShutdownSessionAsync"/>). A pure client registers that handler when it
    /// starts and keeps <see cref="HostAnnouncedLeaving"/> until its next start, so its drop resolves to
    /// <see cref="DisconnectReasons.HostDisconnected"/> even when NGO's reason message is lost with the connection
    /// (<see cref="DisconnectReasons.Resolve"/>).</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LeaveNoticeMessenger : MonoBehaviour
    {
        /// <summary>Named message a client sends to the server when it leaves on purpose (no body besides a version byte).</summary>
        public const string LeavingMessageName = "AF.Leaving";

        /// <summary>Named message the server sends to every client when the host leaves on purpose (version byte only).</summary>
        public const string HostLeavingMessageName = "AF.HostLeaving";

        // Body: one version byte, so the message is never empty and can grow later.
        private const byte MessageVersion = 1;
        private const int MessageSize = sizeof(byte);

        [Header("References")]
        [SerializeField, Tooltip("NetworkManager on this root.")]
        private NetworkManager _manager;

        [SerializeField, Tooltip("Connection approval on this root: frees the seat of the client that sent the notice.")]
        private ConnectionApprovalHandler _approval;

        private CustomMessagingManager _registeredOn;
        private CustomMessagingManager _clientRegisteredOn;
        private bool _hostAnnouncedLeaving;

        /// <summary>
        /// Pure client: the server announced the host is leaving on purpose during this client run (reset when the client
        /// starts again; kept after it stops, so the drop callbacks can still read it).
        /// </summary>
        public bool HostAnnouncedLeaving => _hostAnnouncedLeaving;

        /// <summary>
        /// Pure client: raised once per client run when the server's leaving notice arrives (before the drop). The stats
        /// recorder closes the round as a win here, before any replicated round end or local exit can count it.
        /// </summary>
        public event Action HostLeavingAnnounced;

        private void OnEnable()
        {
            if (_manager == null || _approval == null)
            {
                Debug.LogError($"[{nameof(LeaveNoticeMessenger)}] Missing NetworkManager or ConnectionApprovalHandler on '{name}'; leave notices disabled.", this);
                return;
            }

            _manager.OnServerStarted += HandleServerStarted;
            _manager.OnServerStopped += HandleServerStopped;
            _manager.OnClientStarted += HandleClientStarted;
            _manager.OnClientStopped += HandleClientStopped;

            // Enabled while NGO already runs (e.g. component toggled in Play Mode).
            if (_manager.IsListening && _manager.IsServer)
            {
                Register();
            }
            else if (_manager.IsListening && _manager.IsClient)
            {
                RegisterClient();
            }
        }

        private void OnDisable()
        {
            if (_manager != null)
            {
                _manager.OnServerStarted -= HandleServerStarted;
                _manager.OnServerStopped -= HandleServerStopped;
                _manager.OnClientStarted -= HandleClientStarted;
                _manager.OnClientStopped -= HandleClientStopped;
            }

            Unregister();
            UnregisterClient();
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

        /// <summary>
        /// Server: tells every remote client the host is leaving on purpose. Returns how many clients it was sent to
        /// (0 when not a listening server, shutting down, or alone).
        /// </summary>
        public int SendHostLeavingNotice()
        {
            if (_manager == null || !_manager.IsListening || !_manager.IsServer || _manager.ShutdownInProgress
                || _manager.CustomMessagingManager == null)
            {
                return 0;
            }

            int sent = 0;
            var writer = new FastBufferWriter(MessageSize, Allocator.Temp);
            try
            {
                writer.WriteValueSafe(MessageVersion);
                var clients = _manager.ConnectedClientsIds;
                for (int i = 0; i < clients.Count; i++)
                {
                    ulong clientId = clients[i];
                    if (clientId == NetworkManager.ServerClientId)
                    {
                        continue;
                    }

                    _manager.CustomMessagingManager.SendNamedMessage(HostLeavingMessageName, clientId, writer, NetworkDelivery.ReliableSequenced);
                    sent++;
                }
            }
            finally
            {
                writer.Dispose();
            }

            if (sent > 0)
            {
                Debug.Log($"[{nameof(LeaveNoticeMessenger)}] Host leaving notice sent to {sent} client(s).", this);
            }

            return sent;
        }

        private void HandleClientStarted()
        {
            ResetAnnouncement();
            if (!_manager.IsServer)
            {
                RegisterClient();
            }
        }

        private void HandleClientStopped(bool wasHost)
        {
            UnregisterClient();
        }

        private void RegisterClient()
        {
            CustomMessagingManager messaging = _manager.CustomMessagingManager;
            if (messaging == null || _clientRegisteredOn == messaging || _manager.IsServer)
            {
                return;
            }

            UnregisterClient();
            messaging.RegisterNamedMessageHandler(HostLeavingMessageName, HandleHostLeavingMessage);
            _clientRegisteredOn = messaging;
        }

        private void UnregisterClient()
        {
            CustomMessagingManager messaging = _clientRegisteredOn;
            _clientRegisteredOn = null;
            if (messaging != null)
            {
                messaging.UnregisterNamedMessageHandler(HostLeavingMessageName);
            }
        }

        // Pure client: only the server's notice counts; the body is not needed.
        private void HandleHostLeavingMessage(ulong senderClientId, FastBufferReader reader)
        {
            if (_manager == null || _manager.IsServer || senderClientId != NetworkManager.ServerClientId)
            {
                return;
            }

            ReceiveHostLeavingNotice();
        }

        // A new client run: an earlier session's announcement no longer applies.
        internal void ResetAnnouncement()
        {
            _hostAnnouncedLeaving = false;
        }

        // Sets the flag and raises the event once per run (a repeated notice is ignored).
        internal void ReceiveHostLeavingNotice()
        {
            if (_hostAnnouncedLeaving)
            {
                return;
            }

            _hostAnnouncedLeaving = true;
            Debug.Log($"[{nameof(LeaveNoticeMessenger)}] The host announced it is leaving.", this);
            try
            {
                HostLeavingAnnounced?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
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
