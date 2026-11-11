using AlienFarms.Core;
using AlienFarms.Data;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Server connection approval (version 1.5): parses the <see cref="ConnectionPayload"/>, applies
    /// <see cref="ApprovalRules"/> and seats the client in the <see cref="ServerSlotDirectory"/> (Player1, then Player2).
    /// No player object is created by NGO: the gameplay scene's roster spawns it. Also runs for the host's own client.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ConnectionApprovalHandler : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Protocol version and max players.")]
        private NetworkSettings _settings;

        [Header("References")]
        [SerializeField, Tooltip("NetworkManager on this root.")]
        private NetworkManager _manager;

        private readonly ServerSlotDirectory _slots = new ServerSlotDirectory();
        private System.Action<NetworkManager.ConnectionApprovalRequest, NetworkManager.ConnectionApprovalResponse> _approvalCallback;

        /// <summary>Seats of the current session (server only; empty on clients).</summary>
        public ServerSlotDirectory Slots => _slots;

        private void OnEnable()
        {
            if (_manager == null || _settings == null)
            {
                Debug.LogError($"[{nameof(ConnectionApprovalHandler)}] Missing NetworkManager or NetworkSettings on '{name}'; approval disabled.", this);
                return;
            }

            _approvalCallback ??= HandleApproval;
            _manager.ConnectionApprovalCallback = _approvalCallback;
            _manager.OnClientDisconnectCallback += HandleClientDisconnected;
            _manager.OnServerStopped += HandleServerStopped;
        }

        private void OnDisable()
        {
            if (_manager == null)
            {
                return;
            }

            if (_approvalCallback != null && _manager.ConnectionApprovalCallback == _approvalCallback)
            {
                _manager.ConnectionApprovalCallback = null;
            }

            _manager.OnClientDisconnectCallback -= HandleClientDisconnected;
            _manager.OnServerStopped -= HandleServerStopped;
        }

        private void HandleApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.CreatePlayerObject = false;
            response.Pending = false;

            bool parsed = ConnectionPayload.TryParse(request.Payload, out ConnectionPayload payload);
            int maxPlayers = Mathf.Min(_settings.MaxPlayers, ServerSlotDirectory.Capacity);
            ApprovalDecision decision = ApprovalRules.Evaluate(parsed, payload, _settings.ProtocolVersion, _slots.ConnectedCount, maxPlayers);

            if (decision.Approved && !_slots.TryAssign(request.ClientNetworkId, payload, out PlayerId id))
            {
                decision = ApprovalDecision.Reject(ApprovalRules.ReasonRoomFull);
            }

            response.Approved = decision.Approved;
            response.Reason = decision.Reason;

            if (decision.Approved)
            {
                _slots.TryGetByClient(request.ClientNetworkId, out PlayerSlot slot);
                Debug.Log($"[{nameof(ConnectionApprovalHandler)}] Client {request.ClientNetworkId} approved as {slot.Id} ('{slot.DisplayName}').", this);
            }
            else
            {
                Debug.Log($"[{nameof(ConnectionApprovalHandler)}] Client {request.ClientNetworkId} rejected: {decision.Reason}.", this);
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (!_manager.IsServer)
            {
                return;
            }

            if (_slots.Release(clientId, out PlayerId id))
            {
                Debug.Log($"[{nameof(ConnectionApprovalHandler)}] Client {clientId} left; seat {id} freed.", this);
            }
        }

        private void HandleServerStopped(bool wasHost)
        {
            _slots.Clear();
        }

        private void OnValidate()
        {
            if (_manager == null)
            {
                TryGetComponent(out _manager);
            }

            if (_settings == null)
            {
                Debug.LogWarning($"[{nameof(ConnectionApprovalHandler)}] Missing NetworkSettings on '{name}'.", this);
            }

            if (_manager != null && !_manager.NetworkConfig.ConnectionApproval)
            {
                Debug.LogWarning($"[{nameof(ConnectionApprovalHandler)}] Connection Approval is off on '{_manager.name}'; clients are never seated.", this);
            }
        }
    }
}
