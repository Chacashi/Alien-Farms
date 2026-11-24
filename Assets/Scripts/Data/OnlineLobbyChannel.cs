using System;
using AlienFarms.Core;
using UnityEngine;

namespace AlienFarms.Data
{
    /// <summary>
    /// Runtime channel between the online room menu (requests, views) and the network lobby (state). The menu only calls
    /// the <c>Request*</c> methods and reads the state; the network lobby listens to the requests on every peer and is
    /// the only writer. Primitive data only, so the menu never references networking.
    /// Nothing is serialized: state from a previous play session is discarded through a generation counter.
    /// </summary>
    [CreateAssetMenu(fileName = "OnlineLobbyChannel", menuName = "Alien Farms/Networking/Online Lobby Channel (Runtime)")]
    public sealed class OnlineLobbyChannel : ScriptableObject
    {
        /// <summary>Player seats of the room.</summary>
        public const int MaxSeats = 2;

        /// <summary>Character index that releases the current pick.</summary>
        public const int ReleaseCharacter = OnlineLobbySeat.NoCharacter;

        private static int s_generation;

        [NonSerialized] private readonly OnlineLobbySeat[] _seats = new OnlineLobbySeat[MaxSeats];
        [NonSerialized] private int _generation = -1;
        [NonSerialized] private OnlineLobbyPhase _phase;
        [NonSerialized] private int _seatCount;
        [NonSerialized] private PlayerId _localPlayerId;
        [NonSerialized] private bool _canStart;
        [NonSerialized] private string _lastRejectKey;

        /// <summary>Raised after any state change (phase, seats, local role, start flag, reject).</summary>
        public event Action Changed;

        /// <summary>Lobby side: this machine asks for a character (roster index, <see cref="ReleaseCharacter"/> to release).</summary>
        public event Action<int> CharacterRequested;

        /// <summary>Lobby side (server): the host asks to start the match.</summary>
        public event Action StartRequested;

        public OnlineLobbyPhase Phase => IsCurrent ? _phase : OnlineLobbyPhase.Unavailable;

        /// <summary>Occupied seats (0..<see cref="MaxSeats"/>).</summary>
        public int SeatCount => IsCurrent ? _seatCount : 0;

        /// <summary>Seat held by this machine; <see cref="PlayerId.None"/> while not seated yet.</summary>
        public PlayerId LocalPlayerId => IsCurrent ? _localPlayerId : PlayerId.None;

        /// <summary>True when the match can start (computed by the publisher: two seats with distinct picks).</summary>
        public bool CanStart => IsCurrent && _canStart;

        /// <summary>Localization key of the last refused request (<see cref="OnlineSessionErrorKeys"/>), null when none.</summary>
        public string LastRejectKey => IsCurrent ? _lastRejectKey : null;

        public bool HasReject => !string.IsNullOrEmpty(LastRejectKey);

        private bool IsCurrent => _generation == s_generation;

        private void OnEnable()
        {
            // Referenced from the MainMenu and the networked lobby prefab: keep the runtime state across scene loads.
            hideFlags |= HideFlags.DontUnloadUnusedAsset;
        }

        /// <summary>Seat at <paramref name="index"/> (0..<see cref="SeatCount"/>-1), in seat order.</summary>
        public OnlineLobbySeat GetSeat(int index)
        {
            if ((uint)index >= (uint)SeatCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _seats[index];
        }

        /// <summary>Seat held by <paramref name="id"/>. False when that seat is empty.</summary>
        public bool TryGetSeat(PlayerId id, out OnlineLobbySeat seat)
        {
            if (id != PlayerId.None)
            {
                int count = SeatCount;
                for (int i = 0; i < count; i++)
                {
                    if (_seats[i].Id == id)
                    {
                        seat = _seats[i];
                        return true;
                    }
                }
            }

            seat = default;
            return false;
        }

        // ---- Requests (menu side) ----

        /// <summary>
        /// Asks the lobby for a character (roster index) or to release the pick (any negative value). The lobby validates
        /// it and reports refusals through <see cref="SetReject"/>. False (with an error key) when no lobby is listening.
        /// </summary>
        public bool RequestCharacter(int characterIndex)
        {
            if (CharacterRequested == null)
            {
                return Reject(OnlineSessionErrorKeys.NotConfigured);
            }

            ClearReject();
            CharacterRequested.Invoke(characterIndex < 0 ? ReleaseCharacter : characterIndex);
            return true;
        }

        /// <summary>Asks the lobby (server) to start the match. False (with an error key) when no lobby is listening.</summary>
        public bool RequestStart()
        {
            if (StartRequested == null)
            {
                return Reject(OnlineSessionErrorKeys.NotConfigured);
            }

            ClearReject();
            StartRequested.Invoke();
            return true;
        }

        // ---- State (network lobby side) ----

        /// <summary>
        /// Sets the room phase. <see cref="OnlineLobbyPhase.Unavailable"/> also clears the seats, the local role and the
        /// last refusal (a stale room refusal must never hide a session error).
        /// </summary>
        public void SetPhase(OnlineLobbyPhase phase)
        {
            EnsureCurrent();
            bool changed = _phase != phase;
            _phase = phase;
            if (phase == OnlineLobbyPhase.Unavailable)
            {
                changed |= ClearSeatData();
                changed |= _lastRejectKey != null;
                _lastRejectKey = null;
            }

            if (changed)
            {
                Changed?.Invoke();
            }
        }

        /// <summary>
        /// Replaces the seats with the first <paramref name="count"/> entries (at most <see cref="MaxSeats"/>) and the
        /// local seat (None while not seated yet).
        /// </summary>
        public void SetSeats(OnlineLobbySeat[] seats, int count, PlayerId localPlayerId, bool canStart)
        {
            EnsureCurrent();
            if (seats == null)
            {
                count = 0;
            }

            count = Mathf.Clamp(count, 0, Mathf.Min(MaxSeats, seats?.Length ?? 0));
            bool changed = count != _seatCount
                || localPlayerId != _localPlayerId
                || canStart != _canStart;
            for (int i = 0; i < count; i++)
            {
                if (!changed && !_seats[i].Equals(seats[i]))
                {
                    changed = true;
                }

                _seats[i] = seats[i];
            }

            for (int i = count; i < MaxSeats; i++)
            {
                _seats[i] = default;
            }

            _seatCount = count;
            _localPlayerId = localPlayerId;
            _canStart = canStart;
            if (changed)
            {
                Changed?.Invoke();
            }
        }

        /// <summary>Records a refused request as a localization key (<see cref="OnlineSessionErrorKeys"/>).</summary>
        public void SetReject(string rejectKey)
        {
            EnsureCurrent();
            if (string.Equals(_lastRejectKey, rejectKey, StringComparison.Ordinal))
            {
                return;
            }

            _lastRejectKey = rejectKey;
            Changed?.Invoke();
        }

        public void ClearReject()
        {
            SetReject(null);
        }

        private bool Reject(string rejectKey)
        {
            SetReject(rejectKey);
            return false;
        }

        /// <summary>Clears seats and local role; true when anything was set.</summary>
        private bool ClearSeatData()
        {
            bool changed = _seatCount != 0 || _localPlayerId != PlayerId.None || _canStart;
            for (int i = 0; i < MaxSeats; i++)
            {
                _seats[i] = default;
            }

            _seatCount = 0;
            _localPlayerId = PlayerId.None;
            _canStart = false;
            return changed;
        }

        private void EnsureCurrent()
        {
            if (IsCurrent)
            {
                return;
            }

            _generation = s_generation;
            _phase = OnlineLobbyPhase.Unavailable;
            _lastRejectKey = null;
            ClearSeatData();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_generation++;
        }
    }
}
