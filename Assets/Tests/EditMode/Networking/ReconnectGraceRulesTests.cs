using AlienFarms.Core;
using AlienFarms.Networking.Match;
using AlienFarms.Networking.Session;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests
{
    /// <summary>Seat table to presence board (<see cref="ReconnectGraceRules"/>).</summary>
    public sealed class ReconnectGraceRulesTests
    {
        private const float Grace = 20f;

        [Test]
        public void GraceEnd_TranslatesTheRealtimeDeadlineToServerTime()
        {
            // Dropped 5 s ago (realtime) with 20 s of grace: 15 s left, from server time 1000.
            Assert.That(ReconnectGraceRules.GraceEndServerTime(1000d, 55d, 50d, Grace), Is.EqualTo(1015d).Within(1e-9));
        }

        [Test]
        public void GraceEnd_ExpiredReservation_IsNow()
        {
            Assert.That(ReconnectGraceRules.GraceEndServerTime(1000d, 90d, 50d, Grace), Is.EqualTo(1000d));
        }

        [Test]
        public void AnyAwaiting_OnlyForDroppedSeats()
        {
            Assert.That(ReconnectGraceRules.AnyAwaiting(new PlayerSlot[2]), Is.False);
            Assert.That(ReconnectGraceRules.AnyAwaiting(new[] { Connected(PlayerId.Player1, 0), Connected(PlayerId.Player2, 1) }), Is.False);
            Assert.That(ReconnectGraceRules.AnyAwaiting(new[] { Connected(PlayerId.Player1, 0), Dropped(PlayerId.Player2, 1, 10d) }), Is.True);
        }

        [Test]
        public void BuildSeat_ConnectedReconnectingAndFree()
        {
            Assert.That(ReconnectGraceRules.BuildSeat(Connected(PlayerId.Player1, 3), PlayerConnectionStatus.Connected, 100d, 10d, Grace),
                Is.EqualTo(SeatPresence.Connected(3)));

            SeatPresence reconnecting = ReconnectGraceRules.BuildSeat(Dropped(PlayerId.Player2, 4, 8d), PlayerConnectionStatus.Connected, 100d, 10d, Grace);
            Assert.That(reconnecting.Status, Is.EqualTo(PlayerConnectionStatus.Reconnecting));
            Assert.That(reconnecting.ClientId, Is.EqualTo(4UL));
            Assert.That(reconnecting.GraceEndTime, Is.EqualTo(118d).Within(1e-9));

            Assert.That(ReconnectGraceRules.BuildSeat(default, PlayerConnectionStatus.Connected, 100d, 10d, Grace), Is.EqualTo(SeatPresence.Empty));
            Assert.That(ReconnectGraceRules.BuildSeat(default, PlayerConnectionStatus.TimedOut, 100d, 10d, Grace),
                Is.EqualTo(SeatPresence.Released(PlayerConnectionStatus.TimedOut)));
            Assert.That(ReconnectGraceRules.BuildSeat(default, PlayerConnectionStatus.Left, 100d, 10d, Grace),
                Is.EqualTo(SeatPresence.Released(PlayerConnectionStatus.Left)));
            Assert.That(ReconnectGraceRules.BuildSeat(default, PlayerConnectionStatus.Reconnecting, 100d, 10d, Grace),
                Is.EqualTo(SeatPresence.Empty), "A free seat is never reconnecting.");
        }

        [Test]
        public void BuildBoard_ReadsSeatsAndTheFirstSpectator()
        {
            var seats = new[] { Connected(PlayerId.Player1, 0), Dropped(PlayerId.Player2, 1, 10d) };
            var spectators = new[] { new SpectatorSeat { InUse = true, ClientId = 2 } };
            var lastRelease = new PlayerConnectionStatus[2];

            SeatPresenceBoard board = ReconnectGraceRules.BuildBoard(seats, spectators, lastRelease, default, 500d, 10d, Grace);

            Assert.That(board.Player1, Is.EqualTo(SeatPresence.Connected(0)));
            Assert.That(board.Player2.Status, Is.EqualTo(PlayerConnectionStatus.Reconnecting));
            Assert.That(board.Player2.GraceEndTime, Is.EqualTo(520d).Within(1e-9));
            Assert.That(board.IsSpectator(2), Is.True);
        }

        [Test]
        public void BuildBoard_WithoutSpectator()
        {
            var seats = new[] { Connected(PlayerId.Player1, 0), default };
            var lastRelease = new[] { PlayerConnectionStatus.Connected, PlayerConnectionStatus.Left };

            SeatPresenceBoard board = ReconnectGraceRules.BuildBoard(seats, new SpectatorSeat[1], lastRelease, default, 0d, 0d, Grace);

            Assert.That(board.HasSpectator, Is.False);
            Assert.That(board.Player2, Is.EqualTo(SeatPresence.Released(PlayerConnectionStatus.Left)));
        }

        [Test]
        public void BuildBoard_SameReservation_KeepsItsGraceEnd()
        {
            var seats = new[] { Connected(PlayerId.Player1, 0), Dropped(PlayerId.Player2, 1, 10d) };
            var lastRelease = new PlayerConnectionStatus[2];
            SeatPresenceBoard first = ReconnectGraceRules.BuildBoard(seats, new SpectatorSeat[1], lastRelease, default, 500d, 10d, Grace);

            // Rebuilt 3 s later with clocks that drifted by 2 ms.
            SeatPresenceBoard later = ReconnectGraceRules.BuildBoard(seats, new SpectatorSeat[1], lastRelease, first, 503.002d, 13d, Grace);

            Assert.That(later, Is.EqualTo(first));
        }

        [Test]
        public void KeepReservation_OnlyForTheSameClientStillReconnecting()
        {
            SeatPresence previous = SeatPresence.Reconnecting(1, 520d);

            Assert.That(ReconnectGraceRules.KeepReservation(previous, SeatPresence.Reconnecting(1, 521d)), Is.EqualTo(previous));
            Assert.That(ReconnectGraceRules.KeepReservation(previous, SeatPresence.Reconnecting(5, 530d)), Is.EqualTo(SeatPresence.Reconnecting(5, 530d)));
            Assert.That(ReconnectGraceRules.KeepReservation(previous, SeatPresence.Connected(5)), Is.EqualTo(SeatPresence.Connected(5)));
            Assert.That(ReconnectGraceRules.KeepReservation(SeatPresence.Connected(1), SeatPresence.Reconnecting(1, 530d)),
                Is.EqualTo(SeatPresence.Reconnecting(1, 530d)));
        }

        private static PlayerSlot Connected(PlayerId id, ulong clientId) =>
            new PlayerSlot { Id = id, ClientId = clientId, Connected = true, CharacterIndex = PlayerSlot.NoCharacter };

        private static PlayerSlot Dropped(PlayerId id, ulong clientId, double disconnectedAt) =>
            new PlayerSlot { Id = id, ClientId = clientId, Connected = false, DisconnectedAt = disconnectedAt, CharacterIndex = PlayerSlot.NoCharacter };
    }
}
