using System;
using System.Threading.Tasks;
using AlienFarms.Core;
using AlienFarms.Data;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Server connection approval: parses the <see cref="ConnectionPayload"/> (name sanitized), verifies in an online
    /// session that the declared UGS player id is a member of it (<see cref="ApprovalRules.CheckMembership"/>), applies
    /// <see cref="ApprovalRules"/> (protocol, one connection per UGS player, reconnection, Player1 then Player2, spectator
    /// when allowed) and records the result in the <see cref="ServerSlotDirectory"/>. A rejection carries its
    /// <see cref="DisconnectReasons"/> key in the response. No player object is created by NGO: the gameplay scene's
    /// roster spawns it. Also runs for the host's own client (never verified nor deferred: NGO approves it synchronously).
    /// <para>
    /// Membership race: a client joins the lobby (server side) before it can read the Relay code and connect, but the
    /// host's copy of the player list is updated asynchronously (lobby events or polling), so the NGO request may arrive
    /// first. An id not listed yet leaves the approval pending (ConnectionApprovalResponse.Pending): the session is
    /// refreshed from the service once and the list re-checked until the id is listed or the membership wait passes
    /// (then rejected as <see cref="DisconnectReasons.NotSessionMember"/>). The seat is decided only once confirmed,
    /// against the table as it is then.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ConnectionApprovalHandler : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Protocol version, max players, spectator and reconnection grace.")]
        private NetworkSettings _settings;

        [SerializeField, Range(1f, 9f), Tooltip("Seconds an online client whose UGS id the session does not list yet waits (approval pending) before it is rejected. Keep it under the NetworkManager's Client Connection Buffer Timeout.")]
        private float _membershipWaitSeconds = 5f;

        [Header("References")]
        [SerializeField, Tooltip("NetworkManager on this root.")]
        private NetworkManager _manager;

        [SerializeField, Tooltip("Sessions + Relay connection on this root: in an online session the declared UGS id must be one of its members. Without it ids are not verified.")]
        private RelaySessionConnection _sessionConnection;

        private readonly ServerSlotDirectory _slots = new ServerSlotDirectory();
        // Re-check interval of a pending membership (the lobby events may update the player list meanwhile).
        private const int MembershipPollMilliseconds = 200;

        private Action<NetworkManager.ConnectionApprovalRequest, NetworkManager.ConnectionApprovalResponse> _approvalCallback;

        // Bumped when the server stops or this component is disabled: pending verifications of that run are dropped.
        private int _generation;

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
            _generation++;
        }

        private void HandleApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.CreatePlayerObject = false;
            response.Pending = false;

            ulong clientId = request.ClientNetworkId;
            bool parsed = ConnectionPayload.TryParse(request.Payload, out ConnectionPayload payload);
            if (parsed)
            {
                payload = payload.WithSanitizedName();
            }

            ApprovalPolicy policy = ApprovalPolicy.From(_settings);
            if (ApprovalRules.NeedsIdentity(parsed, payload, policy))
            {
                SessionMembership membership = CheckMembership(clientId, payload, false);
                if (membership == SessionMembership.Unconfirmed)
                {
                    response.Pending = true;
                    Debug.Log($"[{nameof(ConnectionApprovalHandler)}] Client {clientId}: UGS id not listed by the session yet; approval pending.", this);
                    ConfirmMembershipAsync(clientId, payload, response, _generation);
                    return;
                }

                if (membership == SessionMembership.Rejected)
                {
                    Respond(clientId, payload, response, ApprovalDecision.Reject(ApprovalRules.ReasonNotSessionMember));
                    return;
                }
            }

            Respond(clientId, payload, response, Decide(clientId, parsed, payload, policy));
        }

        private SessionMembership CheckMembership(ulong clientId, in ConnectionPayload payload, bool waitExpired)
        {
            bool online = _sessionConnection != null && _sessionConnection.IsOnline;
            bool listed = online && payload.HasUgsPlayerId && _sessionConnection.IsSessionMember(payload.UgsPlayerId.ToString());
            return ApprovalRules.CheckMembership(clientId == NetworkManager.ServerClientId, online, payload.HasUgsPlayerId, listed, waitExpired);
        }

        // Waits for the session to list the client's UGS id (one refresh from the service, then the local list), then
        // finishes the pending approval. Runs on the main thread (Unity synchronization context).
        private async void ConfirmMembershipAsync(ulong clientId, ConnectionPayload payload, NetworkManager.ConnectionApprovalResponse response, int generation)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + _membershipWaitSeconds;
            bool refreshed = false;
            try
            {
                while (true)
                {
                    if (!IsStillPending(clientId, generation))
                    {
                        // Server stopped or client gone meanwhile: nothing to seat.
                        response.Approved = false;
                        response.Pending = false;
                        return;
                    }

                    bool expired = Time.realtimeSinceStartupAsDouble >= deadline;
                    SessionMembership membership = CheckMembership(clientId, payload, expired);
                    if (membership != SessionMembership.Unconfirmed)
                    {
                        ApprovalDecision decision = membership == SessionMembership.Rejected
                            ? ApprovalDecision.Reject(ApprovalRules.ReasonNotSessionMember)
                            : Decide(clientId, true, payload, ApprovalPolicy.From(_settings));
                        Respond(clientId, payload, response, decision);
                        return;
                    }

                    if (!refreshed && _sessionConnection.HasSession)
                    {
                        refreshed = true;
                        Task refresh = _sessionConnection.RefreshSessionAsync();
                        double remaining = Math.Max(0d, deadline - Time.realtimeSinceStartupAsDouble);
                        await Task.WhenAny(refresh, Task.Delay(TimeSpan.FromSeconds(remaining)));
                        continue;
                    }

                    await Task.Delay(MembershipPollMilliseconds);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                response.Approved = false;
                response.Reason = ApprovalRules.ReasonNotSessionMember;
                response.Pending = false;
            }
        }

        private bool IsStillPending(ulong clientId, int generation)
        {
            return this != null
                && generation == _generation
                && _manager != null
                && _manager.IsListening
                && !_manager.ShutdownInProgress
                && _manager.PendingClients.ContainsKey(clientId);
        }

        private ApprovalDecision Decide(ulong clientId, bool parsed, in ConnectionPayload payload, in ApprovalPolicy policy)
        {
            ApprovalDecision decision = ApprovalRules.Evaluate(parsed, payload, policy, _slots.Seats, _slots.Spectators, Time.realtimeSinceStartupAsDouble);
            if (decision.Approved && !_slots.TryApply(decision, clientId, payload))
            {
                // The rules saw a free place the table refused (e.g. a reused client id): never admit without a seat.
                decision = ApprovalDecision.Reject(ApprovalRules.ReasonRoomFull);
            }

            return decision;
        }

        private void Respond(ulong clientId, in ConnectionPayload payload, NetworkManager.ConnectionApprovalResponse response, in ApprovalDecision decision)
        {
            response.Approved = decision.Approved;
            response.Reason = decision.Reason;
            response.Pending = false;

            switch (decision.Outcome)
            {
                case ApprovalOutcome.AssignSeat:
                case ApprovalOutcome.Reconnect:
                    Debug.Log($"[{nameof(ConnectionApprovalHandler)}] Client {clientId} approved as {decision.Seat} ({decision.Outcome}, '{payload.DisplayName}').", this);
                    break;
                case ApprovalOutcome.Spectator:
                    Debug.Log($"[{nameof(ConnectionApprovalHandler)}] Client {clientId} approved as spectator ('{payload.DisplayName}').", this);
                    break;
                default:
                    Debug.Log($"[{nameof(ConnectionApprovalHandler)}] Client {clientId} rejected: {decision.Reason}.", this);
                    break;
            }
        }

        // Version 2.3: a dropped player's seat is freed at once (the reconnection grace, MarkDisconnected, comes in 2.7).
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
            else if (_slots.ReleaseSpectator(clientId))
            {
                Debug.Log($"[{nameof(ConnectionApprovalHandler)}] Spectator {clientId} left.", this);
            }
        }

        private void HandleServerStopped(bool wasHost)
        {
            _generation++;
            _slots.Clear();
        }

        private void OnValidate()
        {
            if (_manager == null)
            {
                TryGetComponent(out _manager);
            }

            if (_sessionConnection == null)
            {
                TryGetComponent(out _sessionConnection);
            }

            if (_settings == null)
            {
                Debug.LogWarning($"[{nameof(ConnectionApprovalHandler)}] Missing NetworkSettings on '{name}'.", this);
            }

            if (_sessionConnection == null)
            {
                Debug.LogWarning($"[{nameof(ConnectionApprovalHandler)}] No RelaySessionConnection on '{name}': online clients' UGS ids are not verified.", this);
            }

            if (_manager != null && _membershipWaitSeconds >= _manager.NetworkConfig.ClientConnectionBufferTimeout)
            {
                Debug.LogWarning($"[{nameof(ConnectionApprovalHandler)}] Membership wait ({_membershipWaitSeconds}s) is not below the Client Connection Buffer Timeout ({_manager.NetworkConfig.ClientConnectionBufferTimeout}s) of '{_manager.name}'; NGO may drop a pending client first.", this);
            }

            if (_manager != null && !_manager.NetworkConfig.ConnectionApproval)
            {
                Debug.LogWarning($"[{nameof(ConnectionApprovalHandler)}] Connection Approval is off on '{_manager.name}'; clients are never seated.", this);
            }
        }
    }
}
