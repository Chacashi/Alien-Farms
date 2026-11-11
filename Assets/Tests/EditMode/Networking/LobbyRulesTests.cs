using System;
using AlienFarms.Core;
using AlienFarms.Networking.Session;
using NUnit.Framework;
using Unity.Collections;

namespace AlienFarms.Networking.Tests
{
    /// <summary>Online room rules (task 2.2): character requests, start condition and seat reconciliation.</summary>
    public sealed class LobbyRulesTests
    {
        private const ulong HostClient = 0;
        private const ulong GuestClient = 7;
        private const ulong SpectatorClient = 9;
        private const int Roster = 4;
        private const byte None = PlayerSlot.NoCharacter;

        // ---------------------------------------------------------------- EvaluateRequest / ApplyRequest

        [Test]
        public void Request_FreeCharacter_IsAccepted()
        {
            LobbySlotEntry[] entries = Room(None, None);

            LobbyRequestResult result = LobbyRules.ApplyRequest(entries, 2, GuestClient, 2, Roster, true);

            Assert.That(result, Is.EqualTo(LobbyRequestResult.Accepted));
            Assert.That(entries[1].CharacterIndex, Is.EqualTo(2));
            Assert.That(entries[0].CharacterIndex, Is.EqualTo(None));
        }

        [Test]
        public void Request_CharacterOfOtherSeat_IsTaken()
        {
            LobbySlotEntry[] entries = Room(1, None);

            LobbyRequestResult result = LobbyRules.ApplyRequest(entries, 2, GuestClient, 1, Roster, true);

            Assert.That(result, Is.EqualTo(LobbyRequestResult.RejectedTaken));
            Assert.That(entries[1].CharacterIndex, Is.EqualTo(None));
        }

        [Test]
        public void Request_OwnCurrentCharacter_IsUnchanged()
        {
            LobbySlotEntry[] entries = Room(3, None);

            Assert.That(LobbyRules.EvaluateRequest(entries, 2, HostClient, 3, Roster, true), Is.EqualTo(LobbyRequestResult.Unchanged));
        }

        [Test]
        public void Request_OtherCharacterWhileHoldingOne_IsLocked()
        {
            LobbySlotEntry[] entries = Room(3, None);

            Assert.That(LobbyRules.ApplyRequest(entries, 2, HostClient, 0, Roster, true), Is.EqualTo(LobbyRequestResult.RejectedLocked));
            Assert.That(entries[0].CharacterIndex, Is.EqualTo(3));
        }

        [Test]
        public void Request_ReleaseThenOtherCharacter_IsAccepted()
        {
            LobbySlotEntry[] entries = Room(3, None);

            LobbyRules.ApplyRequest(entries, 2, HostClient, LobbyRules.Release, Roster, true);

            Assert.That(LobbyRules.ApplyRequest(entries, 2, HostClient, 0, Roster, true), Is.EqualTo(LobbyRequestResult.Accepted));
            Assert.That(entries[0].CharacterIndex, Is.EqualTo(0));
        }

        [Test]
        public void Request_Release_FreesThePick()
        {
            LobbySlotEntry[] entries = Room(2, None);

            LobbyRequestResult result = LobbyRules.ApplyRequest(entries, 2, HostClient, LobbyRules.Release, Roster, true);

            Assert.That(result, Is.EqualTo(LobbyRequestResult.Released));
            Assert.That(entries[0].HasCharacter, Is.False);
        }

        [Test]
        public void Request_ReleaseWithoutPick_IsUnchanged()
        {
            LobbySlotEntry[] entries = Room(None, None);

            Assert.That(LobbyRules.ApplyRequest(entries, 2, HostClient, LobbyRules.Release, Roster, true), Is.EqualTo(LobbyRequestResult.Unchanged));
        }

        [TestCase(Roster)]
        [TestCase(254)]
        [TestCase(255)]
        [TestCase(int.MaxValue)]
        public void Request_OutOfRange_IsRejected(int index)
        {
            LobbySlotEntry[] entries = Room(None, None);

            Assert.That(LobbyRules.ApplyRequest(entries, 2, GuestClient, index, Roster, true), Is.EqualTo(LobbyRequestResult.RejectedOutOfRange));
            Assert.That(entries[1].HasCharacter, Is.False);
        }

        [Test]
        public void Request_EmptyRoster_IsOutOfRange()
        {
            Assert.That(LobbyRules.EvaluateRequest(Room(None, None), 2, HostClient, 0, 0, true), Is.EqualTo(LobbyRequestResult.RejectedOutOfRange));
        }

        [Test]
        public void Request_FromClientWithoutSeat_IsNotSeated()
        {
            LobbySlotEntry[] entries = Room(None, None);

            Assert.That(LobbyRules.ApplyRequest(entries, 2, SpectatorClient, 0, Roster, true), Is.EqualTo(LobbyRequestResult.RejectedNotSeated));
            Assert.That(LobbyRules.ApplyRequest(entries, 2, SpectatorClient, LobbyRules.Release, Roster, true), Is.EqualTo(LobbyRequestResult.RejectedNotSeated));
        }

        [Test]
        public void Request_SeatBeyondCount_IsNotSeated()
        {
            // The second entry is stale buffer data: only the first count entries are seats.
            Assert.That(LobbyRules.EvaluateRequest(Room(None, None), 1, GuestClient, 0, Roster, true), Is.EqualTo(LobbyRequestResult.RejectedNotSeated));
        }

        [Test]
        public void Request_WhenClosed_IsRejectedClosed()
        {
            LobbySlotEntry[] entries = Room(None, None);

            Assert.That(LobbyRules.ApplyRequest(entries, 2, HostClient, 0, Roster, false), Is.EqualTo(LobbyRequestResult.RejectedClosed));
            Assert.That(entries[0].HasCharacter, Is.False);
        }

        [Test]
        public void Request_SameCharacterBothPlayers_FirstOneWins()
        {
            LobbySlotEntry[] entries = Room(None, None);

            LobbyRequestResult first = LobbyRules.ApplyRequest(entries, 2, GuestClient, 1, Roster, true);
            LobbyRequestResult second = LobbyRules.ApplyRequest(entries, 2, HostClient, 1, Roster, true);

            Assert.That(first, Is.EqualTo(LobbyRequestResult.Accepted));
            Assert.That(second, Is.EqualTo(LobbyRequestResult.RejectedTaken));
            Assert.That(entries[1].CharacterIndex, Is.EqualTo(1));
            Assert.That(entries[0].HasCharacter, Is.False);
        }

        [Test]
        public void Request_AfterOtherReleases_IsAccepted()
        {
            LobbySlotEntry[] entries = Room(1, None);
            LobbyRules.ApplyRequest(entries, 2, HostClient, LobbyRules.Release, Roster, true);

            Assert.That(LobbyRules.ApplyRequest(entries, 2, GuestClient, 1, Roster, true), Is.EqualTo(LobbyRequestResult.Accepted));
        }

        // ---------------------------------------------------------------- IsTaken

        [Test]
        public void IsTaken_IgnoresExceptedSeatAndNoCharacter()
        {
            LobbySlotEntry[] entries = Room(1, None);

            Assert.That(LobbyRules.IsTaken(entries, 2, 1, PlayerId.Player2), Is.True);
            Assert.That(LobbyRules.IsTaken(entries, 2, 1, PlayerId.Player1), Is.False);
            Assert.That(LobbyRules.IsTaken(entries, 2, None, PlayerId.None), Is.False);
            Assert.That(LobbyRules.IsTaken(entries, 0, 1, PlayerId.Player2), Is.False);
        }

        // ---------------------------------------------------------------- CanStart

        [Test]
        public void CanStart_NoSeats_False()
        {
            Assert.That(LobbyRules.CanStart(new LobbySlotEntry[2], 0, 2), Is.False);
        }

        [Test]
        public void CanStart_OneSeatWithPick_False()
        {
            Assert.That(LobbyRules.CanStart(Room(0, None), 1, 2), Is.False);
        }

        [Test]
        public void CanStart_TwoSeatsDistinctPicks_True()
        {
            Assert.That(LobbyRules.CanStart(Room(0, 2), 2, 2), Is.True);
        }

        [Test]
        public void CanStart_TwoSeatsOneWithoutPick_False()
        {
            Assert.That(LobbyRules.CanStart(Room(0, None), 2, 2), Is.False);
        }

        [Test]
        public void CanStart_TwoSeatsSamePick_False()
        {
            Assert.That(LobbyRules.CanStart(Room(1, 1), 2, 2), Is.False);
        }

        [Test]
        public void CanStart_InvalidArguments_False()
        {
            Assert.That(LobbyRules.CanStart(null, 2, 2), Is.False);
            Assert.That(LobbyRules.CanStart(Room(0, 1), 2, 0), Is.False);
            Assert.That(LobbyRules.CanStart(Room(0, 1), 5, 2), Is.True, "Count is clamped to the buffer length.");
        }

        // ---------------------------------------------------------------- Reconcile

        [Test]
        public void Reconcile_SameClientsKeepTheirPicks()
        {
            LobbySlotEntry[] current = Room(1, 3);
            var output = new LobbySlotEntry[2];

            int count = LobbyRules.Reconcile(current, 2, Seats(Seat(PlayerId.Player1, HostClient), Seat(PlayerId.Player2, GuestClient)), output);

            Assert.That(count, Is.EqualTo(2));
            Assert.That(output[0].CharacterIndex, Is.EqualTo(1));
            Assert.That(output[1].CharacterIndex, Is.EqualTo(3));
            Assert.That(output[1].ClientId, Is.EqualTo(GuestClient));
            Assert.That(output[1].DisplayName.ToString(), Is.EqualTo("Guest"));
        }

        [Test]
        public void Reconcile_FreedSeat_DropsEntryAndPick()
        {
            LobbySlotEntry[] current = Room(1, 3);
            var output = new LobbySlotEntry[2];

            int count = LobbyRules.Reconcile(current, 2, Seats(Seat(PlayerId.Player1, HostClient), default), output);

            Assert.That(count, Is.EqualTo(1));
            Assert.That(output[0].Id, Is.EqualTo(PlayerId.Player1));
            Assert.That(output[1], Is.EqualTo(default(LobbySlotEntry)));
        }

        [Test]
        public void Reconcile_NewClientInSeat_StartsWithoutCharacter()
        {
            LobbySlotEntry[] current = Room(1, 3);
            var output = new LobbySlotEntry[2];

            int count = LobbyRules.Reconcile(current, 2, Seats(Seat(PlayerId.Player1, HostClient), Seat(PlayerId.Player2, 42)), output);

            Assert.That(count, Is.EqualTo(2));
            Assert.That(output[1].ClientId, Is.EqualTo(42));
            Assert.That(output[1].HasCharacter, Is.False);
            Assert.That(output[0].CharacterIndex, Is.EqualTo(1));
        }

        [Test]
        public void Reconcile_NewSeatJoining_AddsEntryWithoutCharacter()
        {
            LobbySlotEntry[] current = Room(1, None);
            var output = new LobbySlotEntry[2];

            int count = LobbyRules.Reconcile(current, 1, Seats(Seat(PlayerId.Player1, HostClient), Seat(PlayerId.Player2, GuestClient)), output);

            Assert.That(count, Is.EqualTo(2));
            Assert.That(output[0].CharacterIndex, Is.EqualTo(1));
            Assert.That(output[1].Id, Is.EqualTo(PlayerId.Player2));
            Assert.That(output[1].HasCharacter, Is.False);
        }

        [Test]
        public void Reconcile_DisconnectedSeat_IsSkipped()
        {
            PlayerSlot dropped = Seat(PlayerId.Player2, GuestClient);
            dropped.Connected = false;
            var output = new LobbySlotEntry[2];

            int count = LobbyRules.Reconcile(Room(None, None), 2, Seats(Seat(PlayerId.Player1, HostClient), dropped), output);

            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void Reconcile_OnlySecondSeatHeld_IsWrittenFirst()
        {
            var output = new LobbySlotEntry[2];

            int count = LobbyRules.Reconcile(null, 0, Seats(default, Seat(PlayerId.Player2, GuestClient)), output);

            Assert.That(count, Is.EqualTo(1));
            Assert.That(output[0].Id, Is.EqualTo(PlayerId.Player2));
        }

        [Test]
        public void Reconcile_NullOutput_ReturnsZero()
        {
            Assert.That(LobbyRules.Reconcile(Room(0, 1), 2, Seats(Seat(PlayerId.Player1, HostClient), default), null), Is.EqualTo(0));
        }

        // ---------------------------------------------------------------- ToErrorKey

        [TestCase(LobbyRequestResult.Accepted, null)]
        [TestCase(LobbyRequestResult.Released, null)]
        [TestCase(LobbyRequestResult.Unchanged, null)]
        [TestCase(LobbyRequestResult.RejectedNotSeated, OnlineSessionErrorKeys.NotSeated)]
        [TestCase(LobbyRequestResult.RejectedTaken, OnlineSessionErrorKeys.CharacterTaken)]
        [TestCase(LobbyRequestResult.RejectedClosed, OnlineSessionErrorKeys.Busy)]
        [TestCase(LobbyRequestResult.RejectedLocked, null)]
        [TestCase(LobbyRequestResult.RejectedOutOfRange, OnlineSessionErrorKeys.Unknown)]
        [TestCase((LobbyRequestResult)200, OnlineSessionErrorKeys.Unknown)]
        public void ToErrorKey_MapsEveryResult(LobbyRequestResult result, string expected)
        {
            Assert.That(LobbyRules.ToErrorKey(result), Is.EqualTo(expected));
        }

        [Test]
        public void IsRejected_OnlyForRejections()
        {
            Assert.That(LobbyRules.IsRejected(LobbyRequestResult.Accepted), Is.False);
            Assert.That(LobbyRules.IsRejected(LobbyRequestResult.Released), Is.False);
            Assert.That(LobbyRules.IsRejected(LobbyRequestResult.Unchanged), Is.False);
            Assert.That(LobbyRules.IsRejected(LobbyRequestResult.RejectedNotSeated), Is.True);
            Assert.That(LobbyRules.IsRejected(LobbyRequestResult.RejectedClosed), Is.True);
            Assert.That(LobbyRules.IsRejected(LobbyRequestResult.RejectedLocked), Is.True);
        }

        // ---------------------------------------------------------------- Helpers

        private static LobbySlotEntry[] Room(byte hostCharacter, byte guestCharacter)
        {
            return new[]
            {
                new LobbySlotEntry(PlayerId.Player1, HostClient, hostCharacter, new FixedString32Bytes("Host")),
                new LobbySlotEntry(PlayerId.Player2, GuestClient, guestCharacter, new FixedString32Bytes("Guest"))
            };
        }

        private static PlayerSlot Seat(PlayerId id, ulong clientId)
        {
            return new PlayerSlot
            {
                Id = id,
                ClientId = clientId,
                DisplayName = new FixedString32Bytes(id == PlayerId.Player1 ? "Host" : "Guest"),
                CharacterIndex = None,
                Connected = true
            };
        }

        private static ReadOnlySpan<PlayerSlot> Seats(PlayerSlot first, PlayerSlot second)
        {
            return new[] { first, second };
        }
    }
}
