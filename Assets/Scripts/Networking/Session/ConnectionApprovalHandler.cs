using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AlienFarms.Core;
using AlienFarms.Data;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Server connection approval: parses the <see cref="ConnectionPayload"/> (name sanitized), verifies in an online
    /// session that the declared UGS player id is a member of it (<see cref="ApprovalRules.CheckMembership"/>), applies
    /// <see cref="ApprovalRules"/> (protocol, one connection per UGS player, reconnection, Player1 then Player2, then room full)
    /// and records the result in the <see cref="ServerSlotDirectory"/>. A rejection carries its
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
        [SerializeField, Tooltip("Protocol version and reconnection grace.")]
        private NetworkSettings _settings;

        [SerializeField, Range(1f, 9f), Tooltip("Seconds an online client whose UGS id the session does not list yet waits (approval pending) before it is rejected. Keep it under the NetworkManager's Client Connection Buffer Timeout.")]
        private float _membershipWaitSeconds = 5f;

        [Header("References")]
        [SerializeField, Tooltip("NetworkManager on this root.")]
        private NetworkManager _manager;

        [SerializeField, Tooltip("Sessions + Relay connection on this root: in an online session the declared UGS id must be one of its members. Without it ids are not verified.")]
        private RelaySessionConnection _sessionConnection;

        private readonly ServerSlotDirectory _slots = new ServerSlotDirectory();
        // Clients that announced a voluntary leave ("AF.Leaving"); preallocated, an entry is removed when that client drops.
        private readonly HashSet<ulong> _leaving = new HashSet<ulong>(ServerSlotDirectory.Capacity);
        private readonly PlayerId[] _expiredBuffer = new PlayerId[ServerSlotDirectory.Capacity];
        private bool _graceEnabled;
        // Re-check interval of a pending membership (the lobby events may update the player list meanwhile).
        private const int MembershipPollMilliseconds = 200;

        private Action<NetworkManager.ConnectionApprovalRequest, NetworkManager.ConnectionApprovalResponse> _approvalCallback;

        // Bumped when the server stops or this component is disabled: pending verifications of that run are dropped.
        private int _generation;

        // Set by the root when the host starts leaving (before NGO's own shutdown flag); lasts one run (ServerStopLatch).
        private readonly ServerStopLatch _stop = new ServerStopLatch();

        /// <summary>
        /// Server: the run is ending (the root started the host's leave, see <see cref="BeginServerStop"/>, or NGO is
        /// shutting down). Drops then only free their place: no reservation, no grace, no Left, no lobby member removal.
        /// </summary>
        public bool IsServerStopping => _stop.IsRequested || (_manager != null && _manager.ShutdownInProgress);

        /// <summary>Seats of the current session (server only; empty on clients).</summary>
        public ServerSlotDirectory Slots => _slots;

        /// <summary>
        /// True while a silent drop keeps the seat for <see cref="NetworkSettings.ReconnectGraceSeconds"/>
        /// (<see cref="SeatReserved"/>). Off by default: in the MainMenu room a drop frees the seat at once.
        /// </summary>
        public bool GraceEnabled => _graceEnabled;

        /// <summary>Clock of the reservations (<see cref="Time.realtimeSinceStartupAsDouble"/>).</summary>
        public double Now => Time.realtimeSinceStartupAsDouble;

        /// <summary>Server: a seat is now held for its dropped player (grace on, no leave notice).</summary>
        public event Action<PlayerId> SeatReserved;

        /// <summary>
        /// Server: a seat was freed with <see cref="PlayerConnectionStatus.Left"/> (leave notice, or the member left the
        /// session while its seat was reserved) or <see cref="PlayerConnectionStatus.TimedOut"/> (grace over).
        /// </summary>
        public event Action<PlayerId, PlayerConnectionStatus> SeatReleased;

        /// <summary>
        /// Turns the reconnection grace on or off (the gameplay scene's coordinator turns it on while it lives). Turning
        /// it off does not free seats already reserved: they still expire or reconnect.
        /// </summary>
        public void SetGraceEnabled(bool enabled)
        {
            if (_graceEnabled == enabled)
            {
                return;
            }

            _graceEnabled = enabled;
            Debug.Log($"[{nameof(ConnectionApprovalHandler)}] Reconnection grace {(enabled ? "enabled" : "disabled")}.", this);
        }

        /// <summary>
        /// Server: <paramref name="clientId"/> announced it is leaving on purpose (<see cref="LeaveNoticeMessenger"/>);
        /// its upcoming disconnection frees the seat at once as <see cref="PlayerConnectionStatus.Left"/>.
        /// </summary>
        public void NotifyLeaving(ulong clientId)
        {
            if (_manager == null || !_manager.IsServer || clientId == NetworkManager.ServerClientId)
            {
                return;
            }

            if (_leaving.Add(clientId))
            {
                Debug.Log($"[{nameof(ConnectionApprovalHandler)}] Client {clientId} announced it is leaving.", this);
            }
        }

        /// <summary>
        /// Server, backup of the leave notice: the online session reports that member <paramref name="ugsPlayerId"/>
        /// left. If a seat awaits its reconnection it is freed as <see cref="PlayerConnectionStatus.Left"/>; a connected
        /// seat is left alone (its NGO disconnection decides).
        /// </summary>
        public void ReleaseLeftMember(string ugsPlayerId)
        {
            if (_manager == null || !_manager.IsServer || string.IsNullOrEmpty(ugsPlayerId) || IsServerStopping)
            {
                return;
            }

            var ugsId = new FixedString64Bytes();
            ugsId.CopyFromTruncated(ugsPlayerId);
            if (_slots.TryFindAwaiting(ugsId, out PlayerId id) && _slots.ReleaseSeat(id))
            {
                Debug.Log($"[{nameof(ConnectionApprovalHandler)}] Seat {id} released (Left): its player left the session.", this);
                SeatReleased?.Invoke(id, PlayerConnectionStatus.Left);
            }
        }

        /// <summary>
        /// Server: the host is leaving (<see cref="NetworkRootLifetime.ShutdownSessionAsync"/>). From now until the next
        /// server start the coming disconnections are part of the shutdown, not drops. Idempotent.
        /// </summary>
        public void BeginServerStop()
        {
            if (_manager == null || !_stop.Request(_manager.IsServer))
            {
                return;
            }

            Debug.Log($"[{nameof(ConnectionApprovalHandler)}] Server stopping: disconnections free their places without grace.", this);
        }

        /// <summary>
        /// A new session is about to start on this (possibly reused) root: a previous run's stop no longer applies. Called
        /// before StartHost / StartClient, because NGO approves the host's own connection before OnServerStarted.
        /// </summary>
        public void ResetServerStop()
        {
            _stop.Reset();
        }

        /// <summary>Seconds of grace left for the dropped player of <paramref name="id"/>; 0 when its seat awaits nothing.</summary>
        public float GraceLeft(PlayerId id)
        {
            if (_settings == null || !_slots.TryGet(id, out PlayerSlot seat))
            {
                return 0f;
            }

            return SeatGraceRules.GraceLeft(seat, Now, _settings.ReconnectGraceSeconds);
        }

        // Only does work while a seat awaits its player (two seats scanned, no allocation).
        private void Update()
        {
            if (_settings == null || _slots.CountAwaiting == 0 || IsServerStopping)
            {
                return;
            }

            int expired = SeatGraceRules.CollectExpired(_slots.Seats, Now, _settings.ReconnectGraceSeconds, _expiredBuffer);
            for (int i = 0; i < expired; i++)
            {
                PlayerId id = _expiredBuffer[i];
                if (_slots.ReleaseSeat(id))
                {
                    Debug.Log($"[{nameof(ConnectionApprovalHandler)}] Seat {id} released (TimedOut): no reconnection within {_settings.ReconnectGraceSeconds:0.#}s.", this);
                    SeatReleased?.Invoke(id, PlayerConnectionStatus.TimedOut);
                }
            }
        }

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
            _manager.OnServerStarted += HandleServerStarted;
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
            _manager.OnServerStarted -= HandleServerStarted;
            _generation++;
        }

        private void HandleApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.CreatePlayerObject = false;
            response.Pending = false;

            ulong clientId = request.ClientNetworkId;

            // The host is leaving: nobody new is seated in a run that is about to end (never the host itself).
            if (ServerStopLatch.ShouldRejectJoin(clientId, IsServerStopping))
            {
                response.Approved = false;
                response.Reason = DisconnectReasons.HostDisconnected;
                Debug.Log($"[{nameof(ConnectionApprovalHandler)}] Client {clientId} rejected: the host is leaving.", this);
                return;
            }

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
                        // Server stopped or client gone meanwhile: nothing to seat. Cut by the host leaving, the client
                        // shows the host left instead of a lost connection.
                        response.Approved = false;
                        response.Reason = IsServerStopping ? DisconnectReasons.HostDisconnected : null;
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
                && !IsServerStopping
                && _manager.PendingClients.ContainsKey(clientId);
        }

        private ApprovalDecision Decide(ulong clientId, bool parsed, in ConnectionPayload payload, in ApprovalPolicy policy)
        {
            ApprovalDecision decision = ApprovalRules.Evaluate(parsed, payload, policy, _slots.Seats, Time.realtimeSinceStartupAsDouble);
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
                default:
                    Debug.Log($"[{nameof(ConnectionApprovalHandler)}] Client {clientId} rejected: {decision.Reason}.", this);
                    // Never the host's own client: removing it would drop the host from its own session.
                    if (_sessionConnection != null && _sessionConnection.IsOnline
                        && clientId != NetworkManager.ServerClientId
                        && SeatGraceRules.ShouldRemoveRejectedMember(decision.Reason == ApprovalRules.ReasonRoomFull,
                            IsServerStopping, payload.HasUgsPlayerId, _slots.IsUgsIdInUse(payload.UgsPlayerId)))
                    {
                        _ = _sessionConnection.TryRemovePlayerAsync(payload.UgsPlayerId.ToString());
                    }

                    break;
            }
        }

        // A voluntary leave frees the seat as Left; a silent drop keeps it while the grace is on (SeatReserved), and
        // frees it otherwise.
        private void HandleClientDisconnected(ulong clientId)
        {
            if (!_manager.IsServer)
            {
                return;
            }

            bool noticed = _leaving.Remove(clientId);
            if (_slots.TryGetByClient(clientId, out PlayerSlot seat))
            {
                PlayerId id = seat.Id;

                // The server is stopping (every client drops at once): nothing will be reconnected to this run.
                bool stopping = IsServerStopping;
                if (stopping)
                {
                    _slots.Release(clientId, out _);
                    Debug.Log($"[{nameof(ConnectionApprovalHandler)}] Client {clientId} disconnected by the shutdown; seat {id} freed.", this);
                    return;
                }

                switch (SeatGraceRules.OnDisconnect(noticed, _graceEnabled, stopping))
                {
                    case SeatDropOutcome.Left:
                        _slots.Release(clientId, out _);
                        Debug.Log($"[{nameof(ConnectionApprovalHandler)}] Client {clientId} left; seat {id} released (Left).", this);
                        SeatReleased?.Invoke(id, PlayerConnectionStatus.Left);
                        break;
                    case SeatDropOutcome.Reserved:
                        _slots.MarkDisconnected(clientId, Now, out _);
                        Debug.Log($"[{nameof(ConnectionApprovalHandler)}] Client {clientId} dropped; seat {id} reserved for {_settings.ReconnectGraceSeconds:0.#}s.", this);
                        SeatReserved?.Invoke(id);
                        break;
                    default:
                        _slots.Release(clientId, out _);
                        Debug.Log($"[{nameof(ConnectionApprovalHandler)}] Client {clientId} left; seat {id} freed.", this);
                        break;
                }
            }
        }

        // A new run: the previous one's stop no longer applies.
        private void HandleServerStarted()
        {
            _stop.Reset();
        }

        // The table is cleared first: a disconnection NGO still reports after this (the host's own client) finds no seat
        // to reserve, so the stop can end with the run (the root survives a Leave in the lobby and hosts again).
        private void HandleServerStopped(bool wasHost)
        {
            _generation++;
            _slots.Clear();
            _leaving.Clear();
            _stop.OnServerStopped();
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
