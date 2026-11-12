using System;
using AlienFarms.Core;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Match
{
    /// <summary>
    /// Replicated <see cref="SeatPresenceBoard"/> on the in-scene MatchNetwork object, written by the server (the
    /// <see cref="ReconnectGraceCoordinator"/>). Single emitter of <see cref="GameEvents.OnPlayerConnectionChanged"/>,
    /// once per change and per instance:
    /// <list type="bullet">
    /// <item>Server: raises when it writes a different board.</item>
    /// <item>Pure clients: raise in Update, comparing against the last applied board. On spawn they publish the present
    /// state (<see cref="SeatPresenceRules.Current"/>), so a late spectator sees a reservation from its first frame.</item>
    /// </list>
    /// Also tells a pure client whether it is the spectator (<see cref="IsLocalSpectator"/>).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkSeatPresence : NetworkBehaviour
    {
        private readonly NetworkVariable<SeatPresenceBoard> _board = new NetworkVariable<SeatPresenceBoard>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // Preallocated: a transition never produces more than one change per seat.
        private readonly PresenceChange[] _changes = new PresenceChange[SeatPresenceRules.MaxChanges];

        // Server: last board written (kept while not spawned yet, pushed on spawn). Pure client: last board applied.
        private SeatPresenceBoard _applied;
        private bool _isLocalSpectator;

        /// <summary>Raised on a pure client when it becomes (true) or stops being (false) the spectator.</summary>
        public event Action<bool> LocalSpectatorChanged;

        /// <summary>Present board: the replicated one once spawned, otherwise the last one the server wrote.</summary>
        public SeatPresenceBoard Board => IsSpawned ? _board.Value : _applied;

        /// <summary>True on a pure client admitted as spectator (no seat, no avatar).</summary>
        public bool IsLocalSpectator => _isLocalSpectator;

        /// <summary>
        /// Server: publishes <paramref name="board"/> (raising its changes at server time <paramref name="serverNow"/>).
        /// Ignored when unchanged. Callable before this object spawns: the board is replicated on spawn.
        /// </summary>
        public void ServerWrite(in SeatPresenceBoard board, double serverNow)
        {
            if (IsSpawned && !IsServer)
            {
                Debug.LogWarning($"[{nameof(NetworkSeatPresence)}] Only the server writes the seat presence ('{name}').", this);
                return;
            }

            if (board.Equals(_applied))
            {
                return;
            }

            int count = SeatPresenceRules.Diff(_applied, board, serverNow, _changes);
            _applied = board;
            if (IsSpawned)
            {
                _board.Value = board;
            }

            Raise(count);
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                if (!_board.Value.Equals(_applied))
                {
                    _board.Value = _applied;
                }

                return;
            }

            SeatPresenceBoard board = _board.Value;
            _applied = board;
            Raise(SeatPresenceRules.Current(board, ServerNow, _changes));
            RefreshLocalSpectator(board);
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                return;
            }

            _applied = default;
            SetLocalSpectator(false);
        }

        private void Update()
        {
            if (!IsSpawned || IsServer)
            {
                return;
            }

            SeatPresenceBoard board = _board.Value;
            if (board.Equals(_applied))
            {
                return;
            }

            int count = SeatPresenceRules.Diff(_applied, board, ServerNow, _changes);
            _applied = board;
            Raise(count);
            RefreshLocalSpectator(board);
        }

        private double ServerNow => NetworkManager != null ? NetworkManager.ServerTime.Time : 0d;

        private void Raise(int count)
        {
            for (int i = 0; i < count; i++)
            {
                PresenceChange change = _changes[i];
                GameEvents.RaisePlayerConnectionChanged(change.Id, change.Status, change.GraceLeft);
            }
        }

        private void RefreshLocalSpectator(in SeatPresenceBoard board)
        {
            NetworkManager manager = NetworkManager;
            SetLocalSpectator(manager != null && board.IsSpectator(manager.LocalClientId));
        }

        private void SetLocalSpectator(bool spectator)
        {
            if (_isLocalSpectator == spectator)
            {
                return;
            }

            _isLocalSpectator = spectator;
            LocalSpectatorChanged?.Invoke(spectator);
        }
    }
}
