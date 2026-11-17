using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Match;
using AlienFarms.Networking.Data;
using AlienFarms.Networking.Player;
using AlienFarms.Networking.Session;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Match
{
    /// <summary>
    /// Server-only composition of the reconnection grace in the gameplay scene (under OnlineModeRoot). While it lives the
    /// approval handler keeps a dropped player's seat (<see cref="ConnectionApprovalHandler.SetGraceEnabled"/>):
    /// <list type="bullet">
    /// <item>Seat reserved: the match is suspended (only in the phases <see cref="MatchSuspendRules.SuspendsIn"/>) and its
    /// round clock frozen.</item>
    /// <item>Seat restored (its avatar spawned again), released as Left or TimedOut: once no seat awaits anybody the clock
    /// resumes and the suspension ends (the match then aborts by itself if a player is missing). A timed-out member is
    /// removed from the online session.</item>
    /// <item>Spectators join and leave without suspending anything.</item>
    /// <item>The host leaving on purpose (<see cref="NetworkRootLifetime.HostLeaveStarted"/>): the match is suspended and its
    /// clock frozen for good, so nothing ends or scores while the clients get the leaving notice.</item>
    /// </list>
    /// After every change the seat presence board is rebuilt from the seat table and written to
    /// <see cref="NetworkSeatPresence"/>, the single emitter of <see cref="GameEvents.OnPlayerConnectionChanged"/>.
    /// Disabled off the server.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ReconnectGraceCoordinator : MonoBehaviour
    {
        private const int SeatCount = ServerSlotDirectory.Capacity;

        [Header("Config")]
        [SerializeField, Tooltip("Runtime anchor of the NetworkRoot (approval handler, seats, session service).")]
        private NetworkRuntimeAnchor _anchor;

        [SerializeField, Tooltip("Reconnection grace length.")]
        private NetworkSettings _settings;

        [Header("References")]
        [SerializeField, Tooltip("Match (suspended while a seat is reserved).")]
        private MatchManager _match;

        [SerializeField, Tooltip("Replicated match state on MatchNetwork (round clock freeze / resume).")]
        private NetworkMatchState _matchState;

        [SerializeField, Tooltip("Replicated seat presence on MatchNetwork.")]
        private NetworkSeatPresence _presence;

        [SerializeField, Tooltip("Online roster under OnlineModeRoot (tells when a returning player's avatar is back).")]
        private NetworkPlayerRoster _roster;

        // How each seat was last released (Connected = never); index = (int)PlayerId - 1.
        private readonly PlayerConnectionStatus[] _lastRelease = new PlayerConnectionStatus[SeatCount];

        // UGS id of each reserved seat: the seat is already empty when SeatReleased(TimedOut) arrives.
        private readonly string[] _reservedUgsIds = new string[SeatCount];

        private ConnectionApprovalHandler _approval;
        private NetworkManager _manager;
        private NetworkRootLifetime _root;
        private bool _hostLeaving;
        private SeatPresenceBoard _written;
        private bool _active;

        private void Start()
        {
            if (_anchor == null || !_anchor.IsServer)
            {
                // Pure clients (and a scene opened without a session): the server runs the grace.
                enabled = false;
                return;
            }

            _approval = _anchor.Approval;
            _manager = _anchor.Manager;
            if (_approval == null || _manager == null || _settings == null || _match == null || _matchState == null || _presence == null || _roster == null)
            {
                Debug.LogError($"[{nameof(ReconnectGraceCoordinator)}] Missing approval handler, NetworkManager or a serialized reference on '{name}'; the reconnection grace stays off.", this);
                enabled = false;
                return;
            }

            _active = true;
            _approval.SeatReserved += HandleSeatReserved;
            _approval.SeatReleased += HandleSeatReleased;
            _roster.PlayerSpawned += HandlePlayerSpawned;
            _manager.OnClientConnectedCallback += HandleClientConnected;
            _manager.OnClientDisconnectCallback += HandleClientDisconnected;
            _approval.SetGraceEnabled(true);

            _root = _anchor.Root;
            if (_root != null)
            {
                _root.HostLeaveStarted += HandleHostLeaveStarted;
            }

            WriteBoard();
        }

        private void OnDestroy()
        {
            if (!_active)
            {
                return;
            }

            _active = false;
            if (_root != null)
            {
                _root.HostLeaveStarted -= HandleHostLeaveStarted;
                _root = null;
            }

            if (_approval != null)
            {
                _approval.SeatReserved -= HandleSeatReserved;
                _approval.SeatReleased -= HandleSeatReleased;
                _approval.SetGraceEnabled(false);
            }

            if (_roster != null)
            {
                _roster.PlayerSpawned -= HandlePlayerSpawned;
            }

            if (_manager != null)
            {
                _manager.OnClientConnectedCallback -= HandleClientConnected;
                _manager.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }

        private void HandleSeatReserved(PlayerId id)
        {
            // Defensive: the approval handler frees seats without reserving them while the server stops.
            if (IsServerStopping)
            {
                return;
            }

            ServerSlotDirectory slots = _anchor.Slots;
            int index = (int)id - 1;
            if (slots != null && index >= 0 && index < SeatCount && slots.TryGet(id, out PlayerSlot seat))
            {
                // One string per reservation (not per frame).
                _reservedUgsIds[index] = seat.UgsPlayerId.Length > 0 ? seat.UgsPlayerId.ToString() : null;
            }

            MatchPhase phase = _match.CurrentPhase;
            if (MatchSuspendRules.SuspendsIn(phase))
            {
                _match.SetSuspended(true);
                _matchState.ServerFreezeClock();
            }

            Debug.Log($"[{nameof(ReconnectGraceCoordinator)}] {id} dropped in {phase}: seat reserved{(MatchSuspendRules.SuspendsIn(phase) ? ", match suspended" : string.Empty)}.", this);
            WriteBoard();
        }

        private void HandleSeatReleased(PlayerId id, PlayerConnectionStatus status)
        {
            // Defensive, as above: nothing is reported, resumed or removed from the session while the host leaves.
            if (IsServerStopping)
            {
                return;
            }

            int index = (int)id - 1;
            string ugsId = null;
            if (index >= 0 && index < SeatCount)
            {
                _lastRelease[index] = status;
                ugsId = _reservedUgsIds[index];
                _reservedUgsIds[index] = null;
            }

            Debug.Log($"[{nameof(ReconnectGraceCoordinator)}] {id} released ({status}).", this);
            WriteBoard();
            TryResume();

            if (status == PlayerConnectionStatus.TimedOut && !string.IsNullOrEmpty(ugsId))
            {
                RemoveFromSession(ugsId);
            }
        }

        private void HandlePlayerSpawned(PlayerId id, bool restored)
        {
            int index = (int)id - 1;
            if (index >= 0 && index < SeatCount)
            {
                // Held again: a later release of this seat is a new story.
                _lastRelease[index] = PlayerConnectionStatus.Connected;
                _reservedUgsIds[index] = null;
            }

            if (restored)
            {
                Debug.Log($"[{nameof(ReconnectGraceCoordinator)}] {id} is back at its saved pose.", this);
            }

            WriteBoard();
            TryResume();
        }

        // Spectators (and players, before their avatar spawns) only change the board.
        private void HandleClientConnected(ulong clientId)
        {
            WriteBoard();
        }

        // Runs after the approval handler (subscribed earlier on the NetworkRoot): the seat table is already updated.
        private void HandleClientDisconnected(ulong clientId)
        {
            if (IsServerStopping)
            {
                return;
            }

            WriteBoard();
        }

        // The host started leaving (root notice) or NGO is shutting down: the run is over for the grace.
        private bool IsServerStopping => _hostLeaving || _manager.ShutdownInProgress || (_approval != null && _approval.IsServerStopping);

        // The host is leaving: freeze the match until the root stops NGO (never resumed: the scene unloads after).
        private void HandleHostLeaveStarted()
        {
            _hostLeaving = true;
            MatchPhase phase = _match.CurrentPhase;
            if (MatchSuspendRules.SuspendsIn(phase))
            {
                _match.SetSuspended(true);
                _matchState.ServerFreezeClock();
            }

            Debug.Log($"[{nameof(ReconnectGraceCoordinator)}] Host leaving in {phase}: match frozen.", this);
        }

        // Ends the suspension once no seat awaits its player. Both calls are idempotent.
        private void TryResume()
        {
            if (_hostLeaving)
            {
                return;
            }

            ServerSlotDirectory slots = _anchor.Slots;
            if (slots != null && ReconnectGraceRules.AnyAwaiting(slots.Seats))
            {
                return;
            }

            _matchState.ServerResumeClock();
            _match.SetSuspended(false);
        }

        private void WriteBoard()
        {
            ServerSlotDirectory slots = _anchor.Slots;
            if (slots == null)
            {
                return;
            }

            double serverNow = _manager.ServerTime.Time;
            _written = ReconnectGraceRules.BuildBoard(
                slots.Seats, slots.Spectators, _lastRelease, _written, serverNow, _approval.Now, _settings.ReconnectGraceSeconds);
            _presence.ServerWrite(_written, serverNow);
        }

        private async void RemoveFromSession(string ugsId)
        {
            NetworkRootLifetime root = _anchor.Root;
            OnlineSessionService service = root != null ? root.SessionService : null;
            if (service == null)
            {
                return;
            }

            // Best-effort and never throws (see OnlineSessionService.TryRemovePlayerAsync).
            await service.TryRemovePlayerAsync(ugsId);
        }

        private void OnValidate()
        {
            if (_anchor == null)
            {
                Debug.LogWarning($"[{nameof(ReconnectGraceCoordinator)}] Missing NetworkRuntimeAnchor on '{name}'.", this);
            }

            if (_settings == null)
            {
                Debug.LogWarning($"[{nameof(ReconnectGraceCoordinator)}] Missing NetworkSettings on '{name}'.", this);
            }

            if (_match == null || _matchState == null || _presence == null || _roster == null)
            {
                Debug.LogWarning($"[{nameof(ReconnectGraceCoordinator)}] Missing MatchManager, NetworkMatchState, NetworkSeatPresence or NetworkPlayerRoster on '{name}'.", this);
            }
        }
    }
}
