using AlienFarms.Core;
using AlienFarms.Networking.Match;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode;

namespace AlienFarms.Networking.Tests
{
    /// <summary>Wire format and equality of <see cref="SeatPresence"/> and <see cref="SeatPresenceBoard"/>.</summary>
    public sealed class SeatPresenceSerializationTests
    {
        [Test]
        public void Seat_Is18Bytes()
        {
            // bool seated + ulong client + byte status + double grace end.
            Assert.That(Serialize(SeatPresence.Empty).Length, Is.EqualTo(18));
            Assert.That(Serialize(SeatPresence.Reconnecting(ulong.MaxValue, 123.5d)).Length, Is.EqualTo(18));
        }

        [Test]
        public void Board_Is36Bytes()
        {
            Assert.That(Serialize(new SeatPresenceBoard()).Length, Is.EqualTo(36));
        }

        [Test]
        public void Seat_RoundTrip_EveryStatus()
        {
            AssertSeatRoundTrip(SeatPresence.Empty);
            AssertSeatRoundTrip(SeatPresence.Connected(7));
            AssertSeatRoundTrip(SeatPresence.Reconnecting(ulong.MaxValue, 98765.4321d));
            AssertSeatRoundTrip(SeatPresence.Released(PlayerConnectionStatus.Left));
            AssertSeatRoundTrip(SeatPresence.Released(PlayerConnectionStatus.TimedOut));
        }

        [Test]
        public void Board_RoundTrip_LimitValues()
        {
            var board = new SeatPresenceBoard(
                SeatPresence.Connected(0),
                SeatPresence.Reconnecting(ulong.MaxValue, double.MaxValue));

            SeatPresenceBoard result = RoundTrip(board);

            Assert.That(result, Is.EqualTo(board));
            Assert.That(result.Player2.Status, Is.EqualTo(PlayerConnectionStatus.Reconnecting));
            Assert.That(result.Player2.GraceEndTime, Is.EqualTo(double.MaxValue));
        }

        [Test]
        public void Seat_Equality_ComparesEveryField()
        {
            SeatPresence seat = SeatPresence.Reconnecting(3, 10d);

            Assert.That(seat.Equals(SeatPresence.Reconnecting(3, 10d)), Is.True);
            Assert.That(seat.GetHashCode(), Is.EqualTo(SeatPresence.Reconnecting(3, 10d).GetHashCode()));
            Assert.That(seat.Equals(SeatPresence.Reconnecting(4, 10d)), Is.False);
            Assert.That(seat.Equals(SeatPresence.Reconnecting(3, 11d)), Is.False);
            Assert.That(seat.Equals(SeatPresence.Connected(3)), Is.False);
            Assert.That(SeatPresence.Empty.Equals(SeatPresence.Connected(0)), Is.False, "Seated differs.");
            Assert.That(seat.Equals((object)seat), Is.True);
            Assert.That(seat.Equals(null), Is.False);
        }

        [Test]
        public void Board_WithAndGet_TouchOnlyThatSeat()
        {
            SeatPresenceBoard board = new SeatPresenceBoard()
                .With(PlayerId.Player2, SeatPresence.Connected(5));

            Assert.That(board.Get(PlayerId.Player2), Is.EqualTo(SeatPresence.Connected(5)));
            Assert.That(board.Get(PlayerId.Player1), Is.EqualTo(SeatPresence.Empty));
            Assert.That(board.Get(PlayerId.None), Is.EqualTo(SeatPresence.Empty));
            Assert.That(board.With(PlayerId.None, SeatPresence.Connected(1)), Is.EqualTo(board));
        }

        private static void AssertSeatRoundTrip(SeatPresence seat)
        {
            SeatPresence result = RoundTrip(seat);
            Assert.That(result, Is.EqualTo(seat));
            Assert.That(result.Status, Is.EqualTo(seat.Status));
            Assert.That(result.IsSeated, Is.EqualTo(seat.IsSeated));
        }

        private static byte[] Serialize<T>(T value) where T : INetworkSerializable
        {
            using (var writer = new FastBufferWriter(128, Allocator.Temp))
            {
                writer.WriteNetworkSerializable(value);
                return writer.ToArray();
            }
        }

        private static T RoundTrip<T>(T value) where T : INetworkSerializable, new()
        {
            byte[] bytes = Serialize(value);
            using (var reader = new FastBufferReader(bytes, Allocator.Temp))
            {
                reader.ReadNetworkSerializable(out T result);
                Assert.That(reader.Position, Is.EqualTo(bytes.Length), $"{typeof(T).Name} left unread bytes.");
                return result;
            }
        }
    }
}
