using AlienFarms.Core;
using AlienFarms.Networking.Session;
using NUnit.Framework;
using UnityEngine;

namespace AlienFarms.Networking.Tests
{
    /// <summary>
    /// Seat table: seats start without a character and the online room sets it (2.2); decisions applied, dropped seats
    /// kept for reconnection, spectators and saved poses (2.3).
    /// </summary>
    public sealed class ServerSlotDirectoryTests
    {
        private const int Protocol = 2;

        private static readonly ApprovalPolicy Policy = new ApprovalPolicy(Protocol, 2, 0, 20f);

        private ServerSlotDirectory _slots;

        [SetUp]
        public void SetUp()
        {
            _slots = new ServerSlotDirectory();
        }

        // ---------------------------------------------------------------- Seat assignment (rules + TryApply)

        [Test]
        public void Admit_AssignsPlayer1ThenPlayer2_AndRefusesThird()
        {
            ConnectionPayload payload = Payload("P");

            Assert.That(Admit(0, payload, out PlayerId first), Is.True);
            Assert.That(Admit(5, payload, out PlayerId second), Is.True);
            Assert.That(Admit(9, payload, out PlayerId third), Is.False);

            Assert.That(first, Is.EqualTo(PlayerId.Player1));
            Assert.That(second, Is.EqualTo(PlayerId.Player2));
            Assert.That(third, Is.EqualTo(PlayerId.None));
            Assert.That(_slots.ConnectedCount, Is.EqualTo(2));
        }

        [Test]
        public void Release_FreesTheSeatForTheNextClient()
        {
            Admit(0, Payload("A"), out _);
            Admit(5, Payload("B"), out _);

            Assert.That(_slots.Release(0, out PlayerId released), Is.True);
            Assert.That(released, Is.EqualTo(PlayerId.Player1));
            Assert.That(Admit(7, Payload("C"), out PlayerId reused), Is.True);
            Assert.That(reused, Is.EqualTo(PlayerId.Player1));
            Assert.That(_slots.TryGetByClient(7, out PlayerSlot slot) && slot.Id == PlayerId.Player1, Is.True);
        }

        [Test]
        public void TryApply_SameClientCannotTakeTwoSeats()
        {
            Admit(3, Payload("P"), out _);

            Assert.That(Admit(3, Payload("P"), out PlayerId id), Is.False);
            Assert.That(id, Is.EqualTo(PlayerId.None));
            Assert.That(_slots.ConnectedCount, Is.EqualTo(1));
        }

        [Test]
        public void TryApply_KeepsPayloadIdentityAndStartsWithoutCharacter()
        {
            Assert.That(Admit(1, Payload("Ana", "ugs-ana"), out PlayerId id), Is.True);

            Assert.That(_slots.TryGet(id, out PlayerSlot slot), Is.True);
            Assert.That(slot.CharacterIndex, Is.EqualTo(PlayerSlot.NoCharacter));
            Assert.That(slot.HasCharacter, Is.False);
            Assert.That(slot.DisplayName.ToString(), Is.EqualTo("Ana"));
            Assert.That(slot.UgsPlayerId.ToString(), Is.EqualTo("ugs-ana"));
            Assert.That(slot.Connected, Is.True);
            Assert.That(slot.HasSavedPose, Is.False);
        }

        [Test]
        public void TryAssignSeat_InvalidSeatOrKnownClient_Fails()
        {
            Assert.That(_slots.TryAssign(PlayerId.None, 1, Payload("A")), Is.False);
            Assert.That(_slots.TryAssign((PlayerId)3, 1, Payload("A")), Is.False);
            Assert.That(_slots.TryAssign(PlayerId.Player2, 1, Payload("A")), Is.True);
            Assert.That(_slots.TryAssign(PlayerId.Player1, 1, Payload("A")), Is.False, "Client already seated.");
            Assert.That(_slots.TryAssign(PlayerId.Player2, 2, Payload("B")), Is.False, "Seat held by a connected client.");
            Assert.That(_slots.TryGet(PlayerId.Player1, out _), Is.False);
        }

        // ---------------------------------------------------------------- Characters (2.2)

        [Test]
        public void TrySetCharacter_OccupiedSeat_StoresIndex()
        {
            Admit(1, Payload("A"), out _);
            Admit(2, Payload("B"), out _);

            Assert.That(_slots.TrySetCharacter(PlayerId.Player2, 1), Is.True);

            _slots.TryGet(PlayerId.Player1, out PlayerSlot first);
            _slots.TryGet(PlayerId.Player2, out PlayerSlot second);
            Assert.That(first.CharacterIndex, Is.EqualTo(PlayerSlot.NoCharacter));
            Assert.That(second.CharacterIndex, Is.EqualTo(1));
            Assert.That(second.ClientId, Is.EqualTo(2UL));
        }

        [Test]
        public void TrySetCharacter_CanResetToNoCharacter()
        {
            Admit(1, Payload("A"), out PlayerId id);
            _slots.TrySetCharacter(id, 0);

            Assert.That(_slots.TrySetCharacter(id, PlayerSlot.NoCharacter), Is.True);
            _slots.TryGet(id, out PlayerSlot slot);
            Assert.That(slot.CharacterIndex, Is.EqualTo(PlayerSlot.NoCharacter));
        }

        [Test]
        public void TrySetCharacter_EmptySeat_ReturnsFalse()
        {
            Admit(1, Payload("A"), out _);

            Assert.That(_slots.TrySetCharacter(PlayerId.Player2, 0), Is.False);
            Assert.That(_slots.TryGet(PlayerId.Player2, out _), Is.False);
        }

        [TestCase(PlayerId.None)]
        [TestCase((PlayerId)3)]
        public void TrySetCharacter_InvalidId_ReturnsFalse(PlayerId id)
        {
            Admit(1, Payload("A"), out _);

            Assert.That(_slots.TrySetCharacter(id, 0), Is.False);
        }

        [Test]
        public void TrySetCharacter_AfterRelease_ReturnsFalse()
        {
            Admit(1, Payload("A"), out PlayerId id);
            _slots.Release(1, out _);

            Assert.That(_slots.TrySetCharacter(id, 0), Is.False);
        }

        [Test]
        public void TryApply_AfterRelease_StartsWithoutPreviousCharacter()
        {
            Admit(1, Payload("A"), out PlayerId id);
            _slots.TrySetCharacter(id, 2);
            _slots.Release(1, out _);

            Admit(5, Payload("C"), out PlayerId reused);

            _slots.TryGet(reused, out PlayerSlot slot);
            Assert.That(reused, Is.EqualTo(id));
            Assert.That(slot.CharacterIndex, Is.EqualTo(PlayerSlot.NoCharacter));
        }

        // ---------------------------------------------------------------- Decisions (2.3)

        [Test]
        public void TryApply_AssignSeat_TakesThatSeat()
        {
            Assert.That(_slots.TryApply(ApprovalDecision.AssignSeat(PlayerId.Player2), 4, Payload("B", "u2")), Is.True);

            Assert.That(_slots.TryGet(PlayerId.Player1, out _), Is.False);
            Assert.That(_slots.TryGetByClient(4, out PlayerSlot slot) && slot.Id == PlayerId.Player2, Is.True);
        }

        [Test]
        public void TryApply_AssignSeatHeldByConnectedClient_Fails()
        {
            Admit(1, Payload("A", "u1"), out _);

            Assert.That(_slots.TryApply(ApprovalDecision.AssignSeat(PlayerId.Player1), 2, Payload("B", "u2")), Is.False);
            Assert.That(_slots.TryGetByClient(1, out _), Is.True);
        }

        [Test]
        public void TryApply_Rejection_ChangesNothing()
        {
            Assert.That(_slots.TryApply(ApprovalDecision.Reject(DisconnectReasons.RoomFull), 1, Payload("A")), Is.False);
            Assert.That(_slots.ConnectedCount, Is.EqualTo(0));
            Assert.That(_slots.SpectatorCount, Is.EqualTo(0));
        }

        [Test]
        public void TryApply_AssignSeatOverExpiredReservation_DiscardsThePreviousPlayer()
        {
            Admit(1, Payload("A", "u1"), out PlayerId id);
            _slots.TrySetCharacter(id, 3);
            _slots.TrySavePose(id, Vector3.one, 90f);
            _slots.MarkDisconnected(1, 10d, out _);

            Assert.That(_slots.TryApply(ApprovalDecision.AssignSeat(id), 8, Payload("C", "u3")), Is.True);

            _slots.TryGet(id, out PlayerSlot slot);
            Assert.That(slot.ClientId, Is.EqualTo(8UL));
            Assert.That(slot.UgsPlayerId.ToString(), Is.EqualTo("u3"));
            Assert.That(slot.HasCharacter, Is.False);
            Assert.That(slot.HasSavedPose, Is.False);
            Assert.That(slot.Connected, Is.True);
        }

        // ---------------------------------------------------------------- Disconnection and reconnection

        [Test]
        public void MarkDisconnected_KeepsTheSeatAwaitingItsPlayer()
        {
            Admit(1, Payload("A", "u1"), out PlayerId id);

            Assert.That(_slots.MarkDisconnected(1, 42d, out PlayerId dropped), Is.True);

            Assert.That(dropped, Is.EqualTo(id));
            Assert.That(_slots.TryGet(id, out PlayerSlot slot), Is.True);
            Assert.That(slot.IsAwaitingReconnect, Is.True);
            Assert.That(slot.DisconnectedAt, Is.EqualTo(42d));
            Assert.That(_slots.ConnectedCount, Is.EqualTo(0));
            Assert.That(_slots.TryGetByClient(1, out _), Is.False);
            Assert.That(_slots.MarkDisconnected(1, 50d, out _), Is.False, "Already dropped.");
            Assert.That(_slots.Release(1, out _), Is.False, "A dropped seat is not freed by its old client id.");
        }

        [Test]
        public void TryReconnect_SameUgsId_RestoresSeatCharacterAndPoseWithNewClientId()
        {
            Admit(1, Payload("A", "u1"), out PlayerId id);
            _slots.TrySetCharacter(id, 2);
            _slots.TrySavePose(id, new Vector3(1f, 0f, 3f), 45f);
            _slots.MarkDisconnected(1, 10d, out _);

            Assert.That(_slots.TryApply(ApprovalDecision.Reconnect(id), 9, Payload("A renamed", "u1")), Is.True);

            _slots.TryGet(id, out PlayerSlot slot);
            Assert.That(slot.Connected, Is.True);
            Assert.That(slot.ClientId, Is.EqualTo(9UL));
            Assert.That(slot.DisconnectedAt, Is.EqualTo(0d));
            Assert.That(slot.CharacterIndex, Is.EqualTo(2));
            Assert.That(slot.HasSavedPose, Is.True);
            Assert.That(slot.SavedPosition, Is.EqualTo(new Vector3(1f, 0f, 3f)));
            Assert.That(slot.SavedFacingYaw, Is.EqualTo(45f));
            Assert.That(slot.DisplayName.ToString(), Is.EqualTo("A renamed"));
            Assert.That(_slots.ConnectedCount, Is.EqualTo(1));
        }

        [Test]
        public void TryReconnect_OtherUgsIdOrConnectedSeat_Fails()
        {
            Admit(1, Payload("A", "u1"), out PlayerId first);
            Admit(2, Payload("B", "u2"), out PlayerId second);
            _slots.MarkDisconnected(1, 10d, out _);

            Assert.That(_slots.TryReconnect(first, 9, Payload("X", "u9")), Is.False);
            Assert.That(_slots.TryReconnect(first, 9, Payload("X")), Is.False, "No UGS id.");
            Assert.That(_slots.TryReconnect(second, 9, Payload("B", "u2")), Is.False, "Seat still connected.");
            Assert.That(_slots.TryReconnect(first, 2, Payload("A", "u1")), Is.False, "Client already seated.");
        }

        [Test]
        public void ReleaseExpired_FreesOnlyDroppedSeatsPastTheGrace()
        {
            Admit(1, Payload("A", "u1"), out PlayerId first);
            Admit(2, Payload("B", "u2"), out PlayerId second);
            _slots.MarkDisconnected(1, 0d, out _);

            Assert.That(_slots.ReleaseExpired(19d, 20f), Is.EqualTo(0));
            Assert.That(_slots.ReleaseExpired(20d, 20f), Is.EqualTo(1));
            Assert.That(_slots.TryGet(first, out _), Is.False);
            Assert.That(_slots.TryGet(second, out _), Is.True);
        }

        // ---------------------------------------------------------------- Spectators and poses

        [Test]
        public void TryApply_Spectator_AddsUpToCapacityAndRelease()
        {
            Assert.That(_slots.TryApply(ApprovalDecision.Spectator(), 3, Payload("S", "u3")), Is.True);
            Assert.That(_slots.TryApply(ApprovalDecision.Spectator(), 4, Payload("T", "u4")), Is.False);

            Assert.That(_slots.SpectatorCount, Is.EqualTo(1));
            Assert.That(_slots.ConnectedCount, Is.EqualTo(0));
            Assert.That(_slots.TryGetSpectator(3, out SpectatorSeat spectator) && spectator.DisplayName.ToString() == "S", Is.True);
            Assert.That(_slots.Release(3, out _), Is.False, "A spectator holds no seat.");
            Assert.That(_slots.ReleaseSpectator(3), Is.True);
            Assert.That(_slots.SpectatorCount, Is.EqualTo(0));
        }

        [Test]
        public void SeatedClient_CannotAlsoBeSpectator_AndViceVersa()
        {
            Admit(1, Payload("A"), out _);
            _slots.TryAddSpectator(2, Payload("S"));

            Assert.That(_slots.TryAddSpectator(1, Payload("A")), Is.False);
            Assert.That(Admit(2, Payload("S"), out _), Is.False);
        }

        [Test]
        public void TrySavePose_EmptySeat_ReturnsFalse()
        {
            Assert.That(_slots.TrySavePose(PlayerId.Player1, Vector3.zero, 0f), Is.False);
            Assert.That(_slots.TrySavePose(PlayerId.None, Vector3.zero, 0f), Is.False);
        }

        [Test]
        public void Clear_EmptiesSeatsAndSpectators()
        {
            Admit(1, Payload("A"), out _);
            _slots.TryAddSpectator(2, Payload("S"));

            _slots.Clear();

            Assert.That(_slots.ConnectedCount, Is.EqualTo(0));
            Assert.That(_slots.SpectatorCount, Is.EqualTo(0));
            Assert.That(_slots.TryGet(PlayerId.Player1, out _), Is.False);
        }

        // Production path: the approval rules pick the seat (Player1 first) and the table applies it.
        private bool Admit(ulong clientId, in ConnectionPayload payload, out PlayerId id)
        {
            ApprovalDecision decision = ApprovalRules.Evaluate(true, payload, Policy, _slots.Seats, _slots.Spectators, 0d);
            if (decision.Outcome == ApprovalOutcome.AssignSeat && _slots.TryApply(decision, clientId, payload))
            {
                id = decision.Seat;
                return true;
            }

            id = PlayerId.None;
            return false;
        }

        private static ConnectionPayload Payload(string name, string ugsId = null)
        {
            return ConnectionPayload.Create(Protocol, name, ugsId);
        }
    }
}
