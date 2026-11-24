using AlienFarms.Core;
using AlienFarms.Networking.Match;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests
{
    /// <summary>Which <see cref="PresenceChange"/>s a board transition publishes (<see cref="SeatPresenceRules"/>).</summary>
    public sealed class SeatPresenceRulesTests
    {
        private readonly PresenceChange[] _buffer = new PresenceChange[SeatPresenceRules.MaxChanges];

        [Test]
        public void SameBoard_PublishesNothing()
        {
            SeatPresenceBoard board = Board(SeatPresence.Connected(0), SeatPresence.Reconnecting(1, 50d));

            Assert.That(SeatPresenceRules.Diff(board, board, 40d, _buffer), Is.EqualTo(0));
            Assert.That(SeatPresenceRules.Diff(default, default, 40d, _buffer), Is.EqualTo(0));
        }

        [Test]
        public void ConnectedToReconnecting_PublishesGraceLeft()
        {
            SeatPresenceBoard previous = Board(SeatPresence.Connected(0), SeatPresence.Connected(1));
            SeatPresenceBoard current = Board(SeatPresence.Connected(0), SeatPresence.Reconnecting(1, 120d));

            int count = SeatPresenceRules.Diff(previous, current, 100d, _buffer);

            Assert.That(count, Is.EqualTo(1));
            AssertChange(_buffer[0], PlayerId.Player2, PlayerConnectionStatus.Reconnecting, 20f);
        }

        [Test]
        public void ReconnectingToConnected_PublishesReturnWithoutGrace()
        {
            SeatPresenceBoard previous = Board(SeatPresence.Connected(0), SeatPresence.Reconnecting(1, 120d));
            SeatPresenceBoard current = Board(SeatPresence.Connected(0), SeatPresence.Connected(4));

            Assert.That(SeatPresenceRules.Diff(previous, current, 110d, _buffer), Is.EqualTo(1));
            AssertChange(_buffer[0], PlayerId.Player2, PlayerConnectionStatus.Connected, 0f);
        }

        [Test]
        public void ReconnectingToTimedOutOrLeft_PublishesTheRelease()
        {
            SeatPresenceBoard previous = Board(SeatPresence.Connected(0), SeatPresence.Reconnecting(1, 120d));

            Assert.That(SeatPresenceRules.Diff(previous, Board(SeatPresence.Connected(0), SeatPresence.Released(PlayerConnectionStatus.TimedOut)), 125d, _buffer), Is.EqualTo(1));
            AssertChange(_buffer[0], PlayerId.Player2, PlayerConnectionStatus.TimedOut, 0f);

            Assert.That(SeatPresenceRules.Diff(previous, Board(SeatPresence.Connected(0), SeatPresence.Released(PlayerConnectionStatus.Left)), 110d, _buffer), Is.EqualTo(1));
            AssertChange(_buffer[0], PlayerId.Player2, PlayerConnectionStatus.Left, 0f);
        }

        [Test]
        public void ConnectedToLeft_Publishes()
        {
            SeatPresenceBoard previous = Board(SeatPresence.Connected(0), SeatPresence.Connected(1));
            SeatPresenceBoard current = Board(SeatPresence.Connected(0), SeatPresence.Released(PlayerConnectionStatus.Left));

            Assert.That(SeatPresenceRules.Diff(previous, current, 0d, _buffer), Is.EqualTo(1));
            AssertChange(_buffer[0], PlayerId.Player2, PlayerConnectionStatus.Left, 0f);
        }

        [Test]
        public void FirstJoin_And_NewOccupantAfterRelease_PublishConnected()
        {
            SeatPresenceBoard empty = Board(SeatPresence.Connected(0), SeatPresence.Empty);
            SeatPresenceBoard joined = Board(SeatPresence.Connected(0), SeatPresence.Connected(2));
            SeatPresenceBoard released = Board(SeatPresence.Connected(0), SeatPresence.Released(PlayerConnectionStatus.TimedOut));

            Assert.That(SeatPresenceRules.Diff(empty, joined, 0d, _buffer), Is.EqualTo(1));
            AssertChange(_buffer[0], PlayerId.Player2, PlayerConnectionStatus.Connected, 0f);

            Assert.That(SeatPresenceRules.Diff(released, joined, 0d, _buffer), Is.EqualTo(1));
            AssertChange(_buffer[0], PlayerId.Player2, PlayerConnectionStatus.Connected, 0f);
        }

        [Test]
        public void NewReservation_RepublishesReconnecting()
        {
            SeatPresenceBoard first = Board(SeatPresence.Connected(0), SeatPresence.Reconnecting(1, 120d));
            SeatPresenceBoard otherClient = Board(SeatPresence.Connected(0), SeatPresence.Reconnecting(4, 120d));
            SeatPresenceBoard otherEnd = Board(SeatPresence.Connected(0), SeatPresence.Reconnecting(1, 150d));

            Assert.That(SeatPresenceRules.Diff(first, otherClient, 100d, _buffer), Is.EqualTo(1));
            Assert.That(SeatPresenceRules.Diff(first, otherEnd, 100d, _buffer), Is.EqualTo(1));
            AssertChange(_buffer[0], PlayerId.Player2, PlayerConnectionStatus.Reconnecting, 50f);
        }

        [Test]
        public void SameRelease_PublishesNothing()
        {
            // Still the same free seat: nothing new to show.
            SeatPresenceBoard released = Board(SeatPresence.Connected(0), SeatPresence.Released(PlayerConnectionStatus.Left));
            SeatPresenceBoard releasedAgain = Board(SeatPresence.Connected(0), SeatPresence.Released(PlayerConnectionStatus.Left));

            Assert.That(SeatPresenceRules.Diff(released, releasedAgain, 0d, _buffer), Is.EqualTo(0));
        }

        [Test]
        public void BothSeatsChanging_PublishesPlayer1First()
        {
            SeatPresenceBoard current = Board(SeatPresence.Connected(0), SeatPresence.Connected(1));

            Assert.That(SeatPresenceRules.Diff(default, current, 0d, _buffer), Is.EqualTo(2));
            Assert.That(_buffer[0].Id, Is.EqualTo(PlayerId.Player1));
            Assert.That(_buffer[1].Id, Is.EqualTo(PlayerId.Player2));
        }

        [Test]
        public void Diff_RespectsTheOutputLength()
        {
            SeatPresenceBoard current = Board(SeatPresence.Connected(0), SeatPresence.Connected(1));
            var single = new PresenceChange[1];

            Assert.That(SeatPresenceRules.Diff(default, current, 0d, single), Is.EqualTo(1));
            Assert.That(SeatPresenceRules.Diff(default, current, 0d, System.Span<PresenceChange>.Empty), Is.EqualTo(0));
        }

        [Test]
        public void GraceLeft_NeverNegative()
        {
            SeatPresenceBoard previous = Board(SeatPresence.Connected(0), SeatPresence.Connected(1));
            SeatPresenceBoard current = Board(SeatPresence.Connected(0), SeatPresence.Reconnecting(1, 120d));

            SeatPresenceRules.Diff(previous, current, 130d, _buffer);

            Assert.That(_buffer[0].GraceLeft, Is.EqualTo(0f));
        }

        [Test]
        public void Current_PublishesHeldSeatsOnly()
        {
            SeatPresenceBoard board = Board(SeatPresence.Connected(0), SeatPresence.Reconnecting(1, 120d));

            Assert.That(SeatPresenceRules.Current(board, 105d, _buffer), Is.EqualTo(2));
            AssertChange(_buffer[0], PlayerId.Player1, PlayerConnectionStatus.Connected, 0f);
            AssertChange(_buffer[1], PlayerId.Player2, PlayerConnectionStatus.Reconnecting, 15f);

            SeatPresenceBoard released = Board(SeatPresence.Connected(0), SeatPresence.Released(PlayerConnectionStatus.TimedOut));
            Assert.That(SeatPresenceRules.Current(released, 0d, _buffer), Is.EqualTo(1), "Past releases are not replayed.");
            Assert.That(SeatPresenceRules.Current(default, 0d, _buffer), Is.EqualTo(0));
        }

        private static SeatPresenceBoard Board(SeatPresence player1, SeatPresence player2) =>
            new SeatPresenceBoard(player1, player2);

        private static void AssertChange(PresenceChange change, PlayerId id, PlayerConnectionStatus status, float graceLeft)
        {
            Assert.That(change.Id, Is.EqualTo(id));
            Assert.That(change.Status, Is.EqualTo(status));
            Assert.That(change.GraceLeft, Is.EqualTo(graceLeft).Within(0.0001f));
        }
    }
}
