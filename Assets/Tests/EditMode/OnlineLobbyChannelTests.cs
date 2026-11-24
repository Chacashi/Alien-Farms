using AlienFarms.Core;
using AlienFarms.Data;
using NUnit.Framework;
using UnityEngine;

namespace AlienFarms.Tests
{
    /// <summary>Online lobby channel: requests, seats, local role, phase and rejects.</summary>
    public sealed class OnlineLobbyChannelTests
    {
        private OnlineLobbyChannel _channel;
        private OnlineLobbySeat[] _seats;
        private int _changed;
        private int _lastRequestedCharacter;
        private int _characterRequests;
        private int _startRequests;

        [SetUp]
        public void SetUp()
        {
            _channel = ScriptableObject.CreateInstance<OnlineLobbyChannel>();
            _channel.Changed += CountChange;
            _seats = new[]
            {
                new OnlineLobbySeat(PlayerId.Player1, 0, "Host", true, true),
                new OnlineLobbySeat(PlayerId.Player2, OnlineLobbySeat.NoCharacter, "Guest", false, false)
            };
            _changed = 0;
            _lastRequestedCharacter = int.MinValue;
            _characterRequests = 0;
            _startRequests = 0;
        }

        [TearDown]
        public void TearDown()
        {
            _channel.Changed -= CountChange;
            _channel.CharacterRequested -= OnCharacterRequested;
            _channel.StartRequested -= OnStartRequested;
            Object.DestroyImmediate(_channel);
        }

        // ---- Initial state ----

        [Test]
        public void NewChannel_IsUnavailableWithoutSeats()
        {
            Assert.That(_channel.Phase, Is.EqualTo(OnlineLobbyPhase.Unavailable));
            Assert.That(_channel.SeatCount, Is.Zero);
            Assert.That(_channel.LocalPlayerId, Is.EqualTo(PlayerId.None));
            Assert.That(_channel.CanStart, Is.False);
            Assert.That(_channel.HasReject, Is.False);
            Assert.That(_channel.TryGetSeat(PlayerId.Player1, out _), Is.False);
        }

        // ---- Requests ----

        [Test]
        public void RequestCharacter_WithoutListener_ReturnsFalseAndNotConfigured()
        {
            Assert.That(_channel.RequestCharacter(1), Is.False);
            Assert.That(_channel.LastRejectKey, Is.EqualTo(OnlineSessionErrorKeys.NotConfigured));
        }

        [Test]
        public void RequestStart_WithoutListener_ReturnsFalseAndNotConfigured()
        {
            Assert.That(_channel.RequestStart(), Is.False);
            Assert.That(_channel.LastRejectKey, Is.EqualTo(OnlineSessionErrorKeys.NotConfigured));
        }

        [Test]
        public void RequestCharacter_WithListener_DispatchesIndexAndClearsReject()
        {
            _channel.SetReject(OnlineSessionErrorKeys.CharacterTaken);
            _channel.CharacterRequested += OnCharacterRequested;

            Assert.That(_channel.RequestCharacter(2), Is.True);
            Assert.That(_lastRequestedCharacter, Is.EqualTo(2));
            Assert.That(_characterRequests, Is.EqualTo(1));
            Assert.That(_channel.HasReject, Is.False);
        }

        [TestCase(-1)]
        [TestCase(-7)]
        public void RequestCharacter_Negative_DispatchesRelease(int index)
        {
            _channel.CharacterRequested += OnCharacterRequested;

            Assert.That(_channel.RequestCharacter(index), Is.True);
            Assert.That(_lastRequestedCharacter, Is.EqualTo(OnlineLobbyChannel.ReleaseCharacter));
        }

        [Test]
        public void RequestStart_WithListener_Dispatches()
        {
            _channel.StartRequested += OnStartRequested;

            Assert.That(_channel.RequestStart(), Is.True);
            Assert.That(_startRequests, Is.EqualTo(1));
        }

        // ---- Seats ----

        [Test]
        public void SetSeats_StoresSeatsAndLocalRole()
        {
            _channel.SetSeats(_seats, 2, PlayerId.Player1, false);

            Assert.That(_channel.SeatCount, Is.EqualTo(2));
            Assert.That(_channel.LocalPlayerId, Is.EqualTo(PlayerId.Player1));
            Assert.That(_channel.TryGetSeat(PlayerId.Player2, out OnlineLobbySeat guest), Is.True);
            Assert.That(guest.DisplayName, Is.EqualTo("Guest"));
            Assert.That(guest.HasCharacter, Is.False);
            Assert.That(_channel.GetSeat(0).HasCharacter, Is.True);
            Assert.That(_changed, Is.EqualTo(1));
        }

        [Test]
        public void SetSeats_SameDataTwice_RaisesChangedOnce()
        {
            _channel.SetSeats(_seats, 2, PlayerId.Player1, false);
            _channel.SetSeats(_seats, 2, PlayerId.Player1, false);

            Assert.That(_changed, Is.EqualTo(1));
        }

        [Test]
        public void SetSeats_SeatChange_RaisesChanged()
        {
            _channel.SetSeats(_seats, 2, PlayerId.Player1, false);
            _seats[1] = new OnlineLobbySeat(PlayerId.Player2, 1, "Guest", false, false);

            _channel.SetSeats(_seats, 2, PlayerId.Player1, true);

            Assert.That(_changed, Is.EqualTo(2));
            Assert.That(_channel.CanStart, Is.True);
            Assert.That(_channel.TryGetSeat(PlayerId.Player2, out OnlineLobbySeat guest) && guest.CharacterIndex == 1, Is.True);
        }

        [Test]
        public void SetSeats_FewerSeats_ClearsTheRest()
        {
            _channel.SetSeats(_seats, 2, PlayerId.Player1, false);

            _channel.SetSeats(_seats, 1, PlayerId.Player1, false);

            Assert.That(_channel.SeatCount, Is.EqualTo(1));
            Assert.That(_channel.TryGetSeat(PlayerId.Player2, out _), Is.False);
        }

        [Test]
        public void SetSeats_CountIsClampedToMaxSeats()
        {
            var many = new OnlineLobbySeat[4];
            many[0] = _seats[0];
            many[1] = _seats[1];

            _channel.SetSeats(many, 4, PlayerId.Player1, false);

            Assert.That(_channel.SeatCount, Is.EqualTo(OnlineLobbyChannel.MaxSeats));
        }

        [Test]
        public void TryGetSeat_None_ReturnsFalse()
        {
            _channel.SetSeats(_seats, 2, PlayerId.Player1, false);

            Assert.That(_channel.TryGetSeat(PlayerId.None, out _), Is.False);
        }

        // ---- Phase ----

        [Test]
        public void SetPhase_SamePhaseTwice_RaisesChangedOnce()
        {
            _channel.SetPhase(OnlineLobbyPhase.Open);
            _channel.SetPhase(OnlineLobbyPhase.Open);

            Assert.That(_channel.Phase, Is.EqualTo(OnlineLobbyPhase.Open));
            Assert.That(_changed, Is.EqualTo(1));
        }

        [Test]
        public void SetPhase_Unavailable_ClearsSeatsAndRole()
        {
            _channel.SetPhase(OnlineLobbyPhase.Open);
            _channel.SetSeats(_seats, 2, PlayerId.Player1, true);

            _channel.SetPhase(OnlineLobbyPhase.Unavailable);

            Assert.That(_channel.SeatCount, Is.Zero);
            Assert.That(_channel.LocalPlayerId, Is.EqualTo(PlayerId.None));
            Assert.That(_channel.CanStart, Is.False);
            Assert.That(_changed, Is.EqualTo(3));
        }

        [Test]
        public void SetPhase_Unavailable_ClearsReject()
        {
            _channel.SetPhase(OnlineLobbyPhase.Open);
            _channel.SetReject(OnlineSessionErrorKeys.CharacterTaken);
            _changed = 0;

            _channel.SetPhase(OnlineLobbyPhase.Unavailable);

            Assert.That(_channel.HasReject, Is.False);
            Assert.That(_changed, Is.EqualTo(1));
        }

        [Test]
        public void SetPhase_Starting_KeepsReject()
        {
            _channel.SetPhase(OnlineLobbyPhase.Open);
            _channel.SetReject(OnlineSessionErrorKeys.CharacterTaken);

            _channel.SetPhase(OnlineLobbyPhase.Starting);

            Assert.That(_channel.LastRejectKey, Is.EqualTo(OnlineSessionErrorKeys.CharacterTaken));
        }

        [Test]
        public void SetPhase_Starting_KeepsSeats()
        {
            _channel.SetSeats(_seats, 2, PlayerId.Player1, true);

            _channel.SetPhase(OnlineLobbyPhase.Starting);

            Assert.That(_channel.SeatCount, Is.EqualTo(2));
            Assert.That(_channel.Phase, Is.EqualTo(OnlineLobbyPhase.Starting));
        }

        // ---- Rejects ----

        [Test]
        public void SetReject_SameKeyTwice_RaisesChangedOnce()
        {
            _channel.SetReject(OnlineSessionErrorKeys.CharacterTaken);
            _channel.SetReject(OnlineSessionErrorKeys.CharacterTaken);

            Assert.That(_channel.LastRejectKey, Is.EqualTo(OnlineSessionErrorKeys.CharacterTaken));
            Assert.That(_changed, Is.EqualTo(1));
        }

        [Test]
        public void ClearReject_RemovesKey()
        {
            _channel.SetReject(OnlineSessionErrorKeys.NotSeated);

            _channel.ClearReject();

            Assert.That(_channel.HasReject, Is.False);
            Assert.That(_changed, Is.EqualTo(2));
        }

        // ---- Seat struct ----

        [Test]
        public void Seat_NegativeCharacter_NormalizesToNoCharacter()
        {
            var seat = new OnlineLobbySeat(PlayerId.Player1, -5, "A", false, false);

            Assert.That(seat.CharacterIndex, Is.EqualTo(OnlineLobbySeat.NoCharacter));
            Assert.That(seat.HasCharacter, Is.False);
        }

        private void CountChange()
        {
            _changed++;
        }

        private void OnCharacterRequested(int index)
        {
            _lastRequestedCharacter = index;
            _characterRequests++;
        }

        private void OnStartRequested()
        {
            _startRequests++;
        }
    }
}
