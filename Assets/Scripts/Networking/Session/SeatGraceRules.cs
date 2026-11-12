using System;
using AlienFarms.Core;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Pure rules of the reconnection grace on the server: what a disconnection does to a seat, which reservations
    /// expired and how much grace is left. Times are <c>Time.realtimeSinceStartupAsDouble</c> seconds.
    /// </summary>
    public static class SeatGraceRules
    {
        /// <summary>
        /// A voluntary leave (the client sent the notice) always frees the seat as <see cref="SeatDropOutcome.Left"/>; a
        /// silent drop keeps it only while the grace is enabled.
        /// </summary>
        public static SeatDropOutcome OnDisconnect(bool noticedLeaving, bool graceEnabled)
        {
            if (noticedLeaving)
            {
                return SeatDropOutcome.Left;
            }

            return graceEnabled ? SeatDropOutcome.Reserved : SeatDropOutcome.Released;
        }

        /// <summary>Seconds left before the reservation of <paramref name="seat"/> expires; 0 when it awaits nothing or expired.</summary>
        public static float GraceLeft(in PlayerSlot seat, double now, float graceSeconds)
        {
            if (!seat.IsAwaitingReconnect)
            {
                return 0f;
            }

            double left = graceSeconds - (now - seat.DisconnectedAt);
            return left > 0d ? (float)left : 0f;
        }

        /// <summary>
        /// Writes into <paramref name="output"/> the seats whose reservation expired at <paramref name="now"/>. Returns
        /// how many were written (bounded by the output length). No allocation.
        /// </summary>
        public static int CollectExpired(ReadOnlySpan<PlayerSlot> seats, double now, float graceSeconds, Span<PlayerId> output)
        {
            int count = 0;
            for (int i = 0; i < seats.Length && count < output.Length; i++)
            {
                if (ApprovalRules.IsReservationExpired(seats[i], now, graceSeconds))
                {
                    output[count++] = seats[i].Id;
                }
            }

            return count;
        }
    }
}
