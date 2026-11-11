using System;
using AlienFarms.Core;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Pure rules of the online room, applied by the server on a small mirror of the replicated seats (never on the
    /// NetworkList itself). Requests are processed one at a time, so when two players ask for the same character the
    /// first one wins and the second gets <see cref="LobbyRequestResult.RejectedTaken"/>.
    /// </summary>
    public static class LobbyRules
    {
        /// <summary>Requested index meaning "release my pick".</summary>
        public const int Release = -1;

        /// <summary>
        /// Evaluates a character request of <paramref name="senderClientId"/> without changing anything.
        /// <paramref name="requestedIndex"/> is a roster index or a negative value to release the current pick.
        /// </summary>
        public static LobbyRequestResult EvaluateRequest(LobbySlotEntry[] entries, int count, ulong senderClientId,
            int requestedIndex, int rosterCount, bool isOpen)
        {
            if (!isOpen)
            {
                return LobbyRequestResult.RejectedClosed;
            }

            int seat = IndexOfClient(entries, count, senderClientId);
            if (seat < 0)
            {
                return LobbyRequestResult.RejectedNotSeated;
            }

            LobbySlotEntry entry = entries[seat];
            if (requestedIndex < 0)
            {
                return entry.HasCharacter ? LobbyRequestResult.Released : LobbyRequestResult.Unchanged;
            }

            if (requestedIndex >= rosterCount || requestedIndex >= PlayerSlot.NoCharacter)
            {
                return LobbyRequestResult.RejectedOutOfRange;
            }

            var index = (byte)requestedIndex;
            if (entry.CharacterIndex == index)
            {
                return LobbyRequestResult.Unchanged;
            }

            // Picking is being ready: a held pick is locked until it is released (same as the local selection).
            if (entry.HasCharacter)
            {
                return LobbyRequestResult.RejectedLocked;
            }

            return IsTaken(entries, count, index, entry.Id) ? LobbyRequestResult.RejectedTaken : LobbyRequestResult.Accepted;
        }

        /// <summary>
        /// Evaluates the request and, when it is <see cref="LobbyRequestResult.Accepted"/> or
        /// <see cref="LobbyRequestResult.Released"/>, writes the new pick into <paramref name="entries"/>.
        /// </summary>
        public static LobbyRequestResult ApplyRequest(LobbySlotEntry[] entries, int count, ulong senderClientId,
            int requestedIndex, int rosterCount, bool isOpen)
        {
            LobbyRequestResult result = EvaluateRequest(entries, count, senderClientId, requestedIndex, rosterCount, isOpen);
            if (result != LobbyRequestResult.Accepted && result != LobbyRequestResult.Released)
            {
                return result;
            }

            int seat = IndexOfClient(entries, count, senderClientId);
            byte character = result == LobbyRequestResult.Accepted ? (byte)requestedIndex : PlayerSlot.NoCharacter;
            entries[seat] = entries[seat].WithCharacter(character);
            return result;
        }

        /// <summary>True when a seat other than <paramref name="except"/> already holds character <paramref name="index"/>.</summary>
        public static bool IsTaken(LobbySlotEntry[] entries, int count, byte index, PlayerId except)
        {
            if (index == PlayerSlot.NoCharacter || entries == null)
            {
                return false;
            }

            count = ClampCount(entries, count);
            for (int i = 0; i < count; i++)
            {
                if (entries[i].Id != except && entries[i].CharacterIndex == index)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// True when exactly <paramref name="requiredSeats"/> seats are taken and every one has a pick, all distinct.
        /// </summary>
        public static bool CanStart(LobbySlotEntry[] entries, int count, int requiredSeats)
        {
            if (entries == null || requiredSeats <= 0)
            {
                return false;
            }

            count = ClampCount(entries, count);
            if (count != requiredSeats)
            {
                return false;
            }

            for (int i = 0; i < count; i++)
            {
                if (!entries[i].HasCharacter)
                {
                    return false;
                }

                for (int j = i + 1; j < count; j++)
                {
                    if (entries[j].CharacterIndex == entries[i].CharacterIndex)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Rebuilds the room from the server seats into <paramref name="output"/> (seat order, occupied and connected
        /// seats only). A pick survives only while the same client keeps the same seat; a new client or a freed seat
        /// starts without a character. Returns the number of entries written.
        /// </summary>
        public static int Reconcile(LobbySlotEntry[] current, int count, ReadOnlySpan<PlayerSlot> seats, LobbySlotEntry[] output)
        {
            if (output == null)
            {
                return 0;
            }

            count = current != null ? ClampCount(current, count) : 0;
            int written = 0;
            for (int i = 0; i < seats.Length && written < output.Length; i++)
            {
                PlayerSlot seat = seats[i];
                if (!seat.IsOccupied || !seat.Connected)
                {
                    continue;
                }

                byte character = PlayerSlot.NoCharacter;
                for (int j = 0; j < count; j++)
                {
                    if (current[j].Id == seat.Id && current[j].ClientId == seat.ClientId)
                    {
                        character = current[j].CharacterIndex;
                        break;
                    }
                }

                output[written++] = new LobbySlotEntry(seat.Id, seat.ClientId, character, seat.DisplayName);
            }

            for (int i = written; i < output.Length; i++)
            {
                output[i] = default;
            }

            return written;
        }

        /// <summary>Localization key shown for a refused request (<see cref="OnlineSessionErrorKeys"/>); null when not refused.</summary>
        public static string ToErrorKey(LobbyRequestResult result)
        {
            switch (result)
            {
                case LobbyRequestResult.Accepted:
                case LobbyRequestResult.Released:
                case LobbyRequestResult.Unchanged:
                // Locked pick: the menu disables the cards while holding one, so only a race or a tampered client gets
                // here. Refused silently (no error to show, the seat keeps its pick).
                case LobbyRequestResult.RejectedLocked:
                    return null;
                case LobbyRequestResult.RejectedNotSeated:
                    return OnlineSessionErrorKeys.NotSeated;
                case LobbyRequestResult.RejectedTaken:
                    return OnlineSessionErrorKeys.CharacterTaken;
                case LobbyRequestResult.RejectedClosed:
                    return OnlineSessionErrorKeys.Busy;
                default:
                    // RejectedOutOfRange (index outside the roster) only comes from a stale or tampered client: no
                    // dedicated message, the generic error is enough.
                    return OnlineSessionErrorKeys.Unknown;
            }
        }

        /// <summary>True for the results that refuse the request.</summary>
        public static bool IsRejected(LobbyRequestResult result)
        {
            return result >= LobbyRequestResult.RejectedNotSeated;
        }

        private static int IndexOfClient(LobbySlotEntry[] entries, int count, ulong clientId)
        {
            if (entries == null)
            {
                return -1;
            }

            count = ClampCount(entries, count);
            for (int i = 0; i < count; i++)
            {
                if (entries[i].ClientId == clientId && entries[i].Id != PlayerId.None)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int ClampCount(LobbySlotEntry[] entries, int count)
        {
            return count < 0 ? 0 : Math.Min(count, entries.Length);
        }
    }
}
