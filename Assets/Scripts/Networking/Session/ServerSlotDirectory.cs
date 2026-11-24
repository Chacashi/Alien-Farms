using System;
using AlienFarms.Core;
using Unity.Collections;
using UnityEngine;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Server-side seat table: which client holds Player1 / Player2. Plain C#, owned by the approval
    /// handler and shared with the roster and the room through the runtime anchor. A seat is freed with
    /// <see cref="Release"/>, or kept for its player with <see cref="MarkDisconnected"/> until it reconnects
    /// (<see cref="ApprovalOutcome.Reconnect"/>) or <see cref="ReleaseExpired"/> drops it (grace, from 2.7).
    /// </summary>
    public sealed class ServerSlotDirectory
    {
        public const int Capacity = 2;

        // Index = (int)PlayerId - 1.
        private readonly PlayerSlot[] _slots = new PlayerSlot[Capacity];

        /// <summary>Player seats by index ((int)PlayerId - 1), for <see cref="ApprovalRules.Evaluate"/>.</summary>
        public ReadOnlySpan<PlayerSlot> Seats => _slots;

        /// <summary>Seats currently held by a connected client.</summary>
        public int ConnectedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _slots.Length; i++)
                {
                    if (_slots[i].IsOccupied && _slots[i].Connected)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>Seats held for a dropped player that may still reconnect (expired or not: see <see cref="ReleaseExpired"/>).</summary>
        public int CountAwaiting
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _slots.Length; i++)
                {
                    if (_slots[i].IsAwaitingReconnect)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>
        /// Carries out an approved <paramref name="decision"/> for <paramref name="clientId"/>. False (nothing changed)
        /// when it is a rejection or no longer fits the table.
        /// </summary>
        public bool TryApply(in ApprovalDecision decision, ulong clientId, in ConnectionPayload payload)
        {
            switch (decision.Outcome)
            {
                case ApprovalOutcome.AssignSeat:
                    return TryAssign(decision.Seat, clientId, payload);
                case ApprovalOutcome.Reconnect:
                    return TryReconnect(decision.Seat, clientId, payload);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Gives <paramref name="clientId"/> the seat <paramref name="id"/>, empty or held by a dropped player (whose
        /// reservation the caller found expired: its character and pose are discarded). False when the seat is invalid,
        /// held by a connected client, or <paramref name="clientId"/> is already known. The seat starts without a
        /// character: the online room sets it through <see cref="TrySetCharacter"/>.
        /// </summary>
        public bool TryAssign(PlayerId id, ulong clientId, in ConnectionPayload payload)
        {
            int index = (int)id - 1;
            if (index < 0 || index >= _slots.Length || (_slots[index].IsOccupied && _slots[index].Connected) || IsKnown(clientId))
            {
                return false;
            }

            _slots[index] = new PlayerSlot
            {
                Id = id,
                ClientId = clientId,
                DisplayName = payload.DisplayName,
                CharacterIndex = PlayerSlot.NoCharacter,
                Connected = true,
                UgsPlayerId = payload.UgsPlayerId
            };
            return true;
        }

        /// <summary>
        /// Gives the dropped seat <paramref name="id"/> back to its player, now as <paramref name="clientId"/>: character
        /// and saved pose are kept, the name is refreshed. False unless the seat awaits a reconnection from the same
        /// <see cref="ConnectionPayload.UgsPlayerId"/> and <paramref name="clientId"/> is not already known.
        /// </summary>
        public bool TryReconnect(PlayerId id, ulong clientId, in ConnectionPayload payload)
        {
            int index = (int)id - 1;
            if (index < 0 || index >= _slots.Length || !_slots[index].IsAwaitingReconnect || !payload.HasUgsPlayerId
                || !_slots[index].UgsPlayerId.Equals(payload.UgsPlayerId) || IsKnown(clientId))
            {
                return false;
            }

            _slots[index].ClientId = clientId;
            _slots[index].Connected = true;
            _slots[index].DisconnectedAt = 0d;
            _slots[index].DisplayName = payload.DisplayName;
            return true;
        }

        /// <summary>Sets the character of the seat <paramref name="id"/>. False when that seat is empty.</summary>
        public bool TrySetCharacter(PlayerId id, byte characterIndex)
        {
            int index = (int)id - 1;
            if (index < 0 || index >= _slots.Length || !_slots[index].IsOccupied)
            {
                return false;
            }

            _slots[index].CharacterIndex = characterIndex;
            return true;
        }

        /// <summary>Stores where the player of seat <paramref name="id"/> stood, to restore it on reconnection.</summary>
        public bool TrySavePose(PlayerId id, Vector3 position, float facingYaw)
        {
            int index = (int)id - 1;
            if (index < 0 || index >= _slots.Length || !_slots[index].IsOccupied)
            {
                return false;
            }

            _slots[index].HasSavedPose = true;
            _slots[index].SavedPosition = position;
            _slots[index].SavedFacingYaw = facingYaw;
            return true;
        }

        /// <summary>Forgets the saved pose of seat <paramref name="id"/> (used once restored). False when it had none.</summary>
        public bool ClearSavedPose(PlayerId id)
        {
            int index = (int)id - 1;
            if (index < 0 || index >= _slots.Length || !_slots[index].IsOccupied || !_slots[index].HasSavedPose)
            {
                return false;
            }

            _slots[index].HasSavedPose = false;
            _slots[index].SavedPosition = Vector3.zero;
            _slots[index].SavedFacingYaw = 0f;
            return true;
        }

        /// <summary>
        /// The seat awaiting a reconnection from <paramref name="ugsPlayerId"/> (e.g. to release it when that member left
        /// the online session). False for an empty id or when no dropped seat belongs to it.
        /// </summary>
        public bool TryFindAwaiting(in FixedString64Bytes ugsPlayerId, out PlayerId id)
        {
            if (ugsPlayerId.Length > 0)
            {
                for (int i = 0; i < _slots.Length; i++)
                {
                    if (_slots[i].IsAwaitingReconnect && _slots[i].UgsPlayerId.Equals(ugsPlayerId))
                    {
                        id = _slots[i].Id;
                        return true;
                    }
                }
            }

            id = PlayerId.None;
            return false;
        }

        /// <summary>Whether this UGS id holds a connected or reserved player seat.</summary>
        public bool IsUgsIdInUse(in FixedString64Bytes ugsPlayerId)
        {
            if (ugsPlayerId.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].IsOccupied && _slots[i].UgsPlayerId.Equals(ugsPlayerId))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Seat of the connected client <paramref name="clientId"/> (a dropped seat no longer matches).</summary>
        public bool TryGetByClient(ulong clientId, out PlayerSlot slot)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].IsOccupied && _slots[i].Connected && _slots[i].ClientId == clientId)
                {
                    slot = _slots[i];
                    return true;
                }
            }

            slot = default;
            return false;
        }

        public bool TryGet(PlayerId id, out PlayerSlot slot)
        {
            int index = (int)id - 1;
            slot = index >= 0 && index < _slots.Length ? _slots[index] : default;
            return slot.IsOccupied;
        }

        /// <summary>
        /// Keeps the seat of <paramref name="clientId"/> for its player: no longer connected, dropped at
        /// <paramref name="now"/>. False when it held no connected seat.
        /// </summary>
        public bool MarkDisconnected(ulong clientId, double now, out PlayerId id)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].IsOccupied && _slots[i].Connected && _slots[i].ClientId == clientId)
                {
                    _slots[i].Connected = false;
                    _slots[i].DisconnectedAt = now;
                    id = _slots[i].Id;
                    return true;
                }
            }

            id = PlayerId.None;
            return false;
        }

        /// <summary>Frees the seat of <paramref name="clientId"/>. False when it held none.</summary>
        public bool Release(ulong clientId, out PlayerId id)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].IsOccupied && _slots[i].Connected && _slots[i].ClientId == clientId)
                {
                    id = _slots[i].Id;
                    _slots[i] = default;
                    return true;
                }
            }

            id = PlayerId.None;
            return false;
        }

        /// <summary>
        /// Frees the seat <paramref name="id"/>, connected or awaiting a reconnection (e.g. its dropped player left the
        /// session for good). False when the seat is invalid or empty.
        /// </summary>
        public bool ReleaseSeat(PlayerId id)
        {
            int index = (int)id - 1;
            if (index < 0 || index >= _slots.Length || !_slots[index].IsOccupied)
            {
                return false;
            }

            _slots[index] = default;
            return true;
        }

        /// <summary>Frees the dropped seats whose <paramref name="graceSeconds"/> passed. Returns how many.</summary>
        public int ReleaseExpired(double now, float graceSeconds)
        {
            int released = 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (ApprovalRules.IsReservationExpired(_slots[i], now, graceSeconds))
                {
                    _slots[i] = default;
                    released++;
                }
            }

            return released;
        }

        public void Clear()
        {
            Array.Clear(_slots, 0, _slots.Length);
        }

        // Holds a connected seat.
        private bool IsKnown(ulong clientId)
        {
            return TryGetByClient(clientId, out _);
        }
    }
}
