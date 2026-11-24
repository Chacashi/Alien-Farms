using System;
using AlienFarms.Core;
using AlienFarms.Networking.Session;

namespace AlienFarms.Networking.Match
{
    /// <summary>
    /// Pure rules of the <see cref="ReconnectGraceCoordinator"/>: the seat table (realtime clock) turned into the
    /// replicated <see cref="SeatPresenceBoard"/> (server clock). No allocation.
    /// </summary>
    public static class ReconnectGraceRules
    {
        /// <summary>
        /// Server time when a reservation made at <paramref name="disconnectedAt"/> (realtime) expires, given both clocks
        /// read now. Translates the reservation's realtime deadline to the clock clients know.
        /// </summary>
        public static double GraceEndServerTime(double serverNow, double realtimeNow, double disconnectedAt, float grace)
        {
            double left = grace - (realtimeNow - disconnectedAt);
            return serverNow + (left > 0d ? left : 0d);
        }

        /// <summary>True while any seat waits for its dropped player.</summary>
        public static bool AnyAwaiting(ReadOnlySpan<PlayerSlot> seats)
        {
            for (int i = 0; i < seats.Length; i++)
            {
                if (seats[i].IsAwaitingReconnect)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Presence of one seat: Connected while held, Reconnecting (with its server-time grace end) while awaiting, and
        /// otherwise <paramref name="lastRelease"/> (Left / TimedOut; Connected = never released, an empty seat).
        /// </summary>
        public static SeatPresence BuildSeat(in PlayerSlot seat, PlayerConnectionStatus lastRelease, double serverNow, double realtimeNow, float grace)
        {
            if (!seat.IsOccupied)
            {
                return lastRelease == PlayerConnectionStatus.Left || lastRelease == PlayerConnectionStatus.TimedOut
                    ? SeatPresence.Released(lastRelease)
                    : SeatPresence.Empty;
            }

            return seat.Connected
                ? SeatPresence.Connected(seat.ClientId)
                : SeatPresence.Reconnecting(seat.ClientId, GraceEndServerTime(serverNow, realtimeNow, seat.DisconnectedAt, grace));
        }

        /// <summary>
        /// The same reservation rebuilt later keeps its first grace end: both clocks are read again on every rebuild and
        /// would otherwise move it by a few milliseconds (republishing the reservation). A different client or status
        /// takes <paramref name="rebuilt"/>.
        /// </summary>
        public static SeatPresence KeepReservation(in SeatPresence previous, in SeatPresence rebuilt)
        {
            bool sameReservation = previous.Status == PlayerConnectionStatus.Reconnecting
                && rebuilt.Status == PlayerConnectionStatus.Reconnecting
                && previous.ClientId == rebuilt.ClientId;
            return sameReservation ? previous : rebuilt;
        }

        /// <summary>
        /// Whole board from the seat table (index = (int)PlayerId - 1).
        /// <paramref name="lastRelease"/> holds, per seat, how it was last released; <paramref name="previous"/> is the
        /// board written last (its reservations keep their grace end, see <see cref="KeepReservation"/>).
        /// </summary>
        public static SeatPresenceBoard BuildBoard(
            ReadOnlySpan<PlayerSlot> seats,
            ReadOnlySpan<PlayerConnectionStatus> lastRelease,
            in SeatPresenceBoard previous,
            double serverNow,
            double realtimeNow,
            float grace)
        {
            SeatPresence player1 = seats.Length > 0
                ? BuildSeat(seats[0], lastRelease.Length > 0 ? lastRelease[0] : PlayerConnectionStatus.Connected, serverNow, realtimeNow, grace)
                : SeatPresence.Empty;
            SeatPresence player2 = seats.Length > 1
                ? BuildSeat(seats[1], lastRelease.Length > 1 ? lastRelease[1] : PlayerConnectionStatus.Connected, serverNow, realtimeNow, grace)
                : SeatPresence.Empty;
            player1 = KeepReservation(previous.Player1, player1);
            player2 = KeepReservation(previous.Player2, player2);

            return new SeatPresenceBoard(player1, player2);
        }
    }
}
