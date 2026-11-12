using System;
using AlienFarms.Core;
using AlienFarms.Networking.Session;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests
{
    /// <summary>Server reconnection grace: what a disconnection does to a seat, expiration and time left.</summary>
    public sealed class SeatGraceRulesTests
    {
        private const float Grace = 20f;

        [TestCase(true, true, SeatDropOutcome.Left)]
        [TestCase(true, false, SeatDropOutcome.Left)]
        [TestCase(false, true, SeatDropOutcome.Reserved)]
        [TestCase(false, false, SeatDropOutcome.Released)]
        public void OnDisconnect_DecidesBySignalAndGrace(bool noticed, bool graceEnabled, SeatDropOutcome expected)
        {
            Assert.That(SeatGraceRules.OnDisconnect(noticed, graceEnabled), Is.EqualTo(expected));
        }

        [Test]
        public void GraceLeft_AwaitingSeat_CountsDown()
        {
            PlayerSlot seat = Awaiting(PlayerId.Player2, 100d);

            Assert.That(SeatGraceRules.GraceLeft(seat, 100d, Grace), Is.EqualTo(20f).Within(1e-4f));
            Assert.That(SeatGraceRules.GraceLeft(seat, 112.5d, Grace), Is.EqualTo(7.5f).Within(1e-4f));
            Assert.That(SeatGraceRules.GraceLeft(seat, 120d, Grace), Is.EqualTo(0f));
            Assert.That(SeatGraceRules.GraceLeft(seat, 500d, Grace), Is.EqualTo(0f));
        }

        [Test]
        public void GraceLeft_ConnectedOrEmptySeat_IsZero()
        {
            Assert.That(SeatGraceRules.GraceLeft(Connected(PlayerId.Player1), 10d, Grace), Is.EqualTo(0f));
            Assert.That(SeatGraceRules.GraceLeft(default, 10d, Grace), Is.EqualTo(0f));
        }

        [Test]
        public void CollectExpired_ReturnsOnlySeatsPastTheGrace()
        {
            PlayerSlot[] seats = { Awaiting(PlayerId.Player1, 0d), Awaiting(PlayerId.Player2, 10d) };
            Span<PlayerId> output = stackalloc PlayerId[2];

            Assert.That(SeatGraceRules.CollectExpired(seats, 19.9d, Grace, output), Is.EqualTo(0));

            int count = SeatGraceRules.CollectExpired(seats, 20d, Grace, output);
            Assert.That(count, Is.EqualTo(1));
            Assert.That(output[0], Is.EqualTo(PlayerId.Player1));

            count = SeatGraceRules.CollectExpired(seats, 30d, Grace, output);
            Assert.That(count, Is.EqualTo(2));
            Assert.That(output[1], Is.EqualTo(PlayerId.Player2));
        }

        [Test]
        public void CollectExpired_IgnoresConnectedAndEmptySeats_AndRespectsOutputLength()
        {
            PlayerSlot[] seats = { Connected(PlayerId.Player1), Awaiting(PlayerId.Player2, 0d) };
            Span<PlayerId> none = Span<PlayerId>.Empty;
            Span<PlayerId> output = stackalloc PlayerId[2];

            Assert.That(SeatGraceRules.CollectExpired(seats, 1000d, Grace, none), Is.EqualTo(0));
            Assert.That(SeatGraceRules.CollectExpired(seats, 1000d, Grace, output), Is.EqualTo(1));
            Assert.That(output[0], Is.EqualTo(PlayerId.Player2));
            Assert.That(SeatGraceRules.CollectExpired(new PlayerSlot[2], 1000d, Grace, output), Is.EqualTo(0));
        }

        [Test]
        public void Directory_ExpiredReservation_IsReleasedBySeat_AndNoLongerAwaits()
        {
            var directory = new ServerSlotDirectory();
            ConnectionPayload payload = ConnectionPayload.Create(1, "P2", "ugs-p2");
            Assert.That(directory.TryAssign(PlayerId.Player2, 7, payload), Is.True);
            Assert.That(directory.MarkDisconnected(7, 50d, out PlayerId dropped), Is.True);
            Assert.That(dropped, Is.EqualTo(PlayerId.Player2));
            Assert.That(directory.CountAwaiting, Is.EqualTo(1));

            Span<PlayerId> output = stackalloc PlayerId[ServerSlotDirectory.Capacity];
            int count = SeatGraceRules.CollectExpired(directory.Seats, 70d, Grace, output);
            Assert.That(count, Is.EqualTo(1));
            Assert.That(directory.ReleaseSeat(output[0]), Is.True);
            Assert.That(directory.CountAwaiting, Is.EqualTo(0));
            Assert.That(directory.TryGet(PlayerId.Player2, out _), Is.False);
        }

        private static PlayerSlot Awaiting(PlayerId id, double disconnectedAt)
        {
            PlayerSlot seat = Connected(id);
            seat.Connected = false;
            seat.DisconnectedAt = disconnectedAt;
            return seat;
        }

        private static PlayerSlot Connected(PlayerId id)
        {
            return new PlayerSlot
            {
                Id = id,
                ClientId = (ulong)id,
                CharacterIndex = PlayerSlot.NoCharacter,
                Connected = true
            };
        }
    }
}
