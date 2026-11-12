using System;
using AlienFarms.Core;

namespace AlienFarms.Networking.Match
{
    /// <summary>
    /// Pure rules of <see cref="NetworkSeatPresence"/>: which <see cref="PresenceChange"/>s a board transition publishes.
    /// The spectator place never publishes anything (it does not suspend nor show a banner). No allocation.
    /// </summary>
    public static class SeatPresenceRules
    {
        /// <summary>Most changes one transition can produce (one per seat): size of the caller's buffer.</summary>
        public const int MaxChanges = 2;

        /// <summary>
        /// Writes into <paramref name="output"/> what changed from <paramref name="previous"/> to
        /// <paramref name="current"/> at server time <paramref name="now"/>, Player1 first. A seat publishes when its
        /// status changes, when a new reservation starts (other client or other grace end) and when a different client
        /// takes it while connected. Returns how many were written (bounded by the output length).
        /// </summary>
        public static int Diff(in SeatPresenceBoard previous, in SeatPresenceBoard current, double now, Span<PresenceChange> output)
        {
            int count = 0;
            count = DiffSeat(PlayerId.Player1, previous.Player1, current.Player1, now, output, count);
            count = DiffSeat(PlayerId.Player2, previous.Player2, current.Player2, now, output, count);
            return count;
        }

        /// <summary>
        /// What a late instance publishes on spawn so it shows the present state from its first frame: every held seat
        /// (Connected or Reconnecting). Past releases (Left / TimedOut) are not replayed.
        /// </summary>
        public static int Current(in SeatPresenceBoard board, double now, Span<PresenceChange> output)
        {
            int count = 0;
            count = CurrentSeat(PlayerId.Player1, board.Player1, now, output, count);
            count = CurrentSeat(PlayerId.Player2, board.Player2, now, output, count);
            return count;
        }

        /// <summary>Seconds of grace left for <paramref name="seat"/> at server time <paramref name="now"/>; 0 unless reconnecting.</summary>
        public static float GraceLeft(in SeatPresence seat, double now)
        {
            if (seat.Status != PlayerConnectionStatus.Reconnecting)
            {
                return 0f;
            }

            double left = seat.GraceEndTime - now;
            return left > 0d ? (float)left : 0f;
        }

        /// <summary>True when going from <paramref name="previous"/> to <paramref name="current"/> must be published.</summary>
        public static bool Publishes(in SeatPresence previous, in SeatPresence current)
        {
            if (previous.Equals(current))
            {
                return false;
            }

            if (previous.Status != current.Status)
            {
                return true;
            }

            switch (current.Status)
            {
                case PlayerConnectionStatus.Reconnecting:
                    // A new reservation (dropped again, or a different client) restarts the count.
                    return previous.ClientId != current.ClientId || !previous.GraceEndTime.Equals(current.GraceEndTime);
                case PlayerConnectionStatus.Connected:
                    // Someone (else) took the seat: a fresh seat goes from never-held to held.
                    return current.IsSeated && (!previous.IsSeated || previous.ClientId != current.ClientId);
                default:
                    return false;
            }
        }

        private static int DiffSeat(PlayerId id, in SeatPresence previous, in SeatPresence current, double now, Span<PresenceChange> output, int count)
        {
            if (count >= output.Length || !Publishes(previous, current))
            {
                return count;
            }

            output[count] = new PresenceChange(id, current.Status, GraceLeft(current, now));
            return count + 1;
        }

        private static int CurrentSeat(PlayerId id, in SeatPresence seat, double now, Span<PresenceChange> output, int count)
        {
            if (count >= output.Length || !seat.IsSeated)
            {
                return count;
            }

            output[count] = new PresenceChange(id, seat.Status, GraceLeft(seat, now));
            return count + 1;
        }
    }
}
