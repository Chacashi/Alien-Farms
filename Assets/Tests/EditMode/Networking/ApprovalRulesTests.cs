using AlienFarms.Core;
using AlienFarms.Networking.Session;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests
{
    /// <summary>Connection approval rules (task 2.3): protocol, one connection per UGS player, reconnection and seats.</summary>
    public sealed class ApprovalRulesTests
    {
        private const int Protocol = 2;
        private const float Grace = 20f;
        private const double Now = 100d;

        private static readonly ApprovalPolicy Players = new ApprovalPolicy(Protocol, 2, Grace);

        private ServerSlotDirectory _slots;

        [SetUp]
        public void SetUp()
        {
            _slots = new ServerSlotDirectory();
        }

        // ---------------------------------------------------------------- Protocol and payload

        [Test]
        public void Evaluate_ProtocolMismatch_Rejects()
        {
            ApprovalDecision decision = Evaluate(Payload("Old", "u1", Protocol + 1));

            Assert.That(decision.Approved, Is.False);
            Assert.That(decision.Outcome, Is.EqualTo(ApprovalOutcome.Rejected));
            Assert.That(decision.Reason, Is.EqualTo(ApprovalRules.ReasonProtocolMismatch));
        }

        [Test]
        public void Evaluate_UnparsedPayloadWithAnotherVersion_IsProtocolMismatch()
        {
            var partial = new ConnectionPayload { ProtocolVersion = 1 };

            ApprovalDecision decision = ApprovalRules.Evaluate(false, partial, Players, _slots.Seats, Now);

            Assert.That(decision.Reason, Is.EqualTo(ApprovalRules.ReasonProtocolMismatch));
        }

        [Test]
        public void Evaluate_Protocol1Bytes_AreToldProtocolMismatch()
        {
            // A protocol 1 client: int + name + character byte + UGS id. Parsing fails, the version survives.
            byte[] old = { 1, 0, 0, 0, 1, 0, 0, 0, (byte)'A', 0, 0, 0, 0, 0 };
            bool parsed = ConnectionPayload.TryParse(old, out ConnectionPayload payload);

            ApprovalDecision decision = ApprovalRules.Evaluate(parsed, payload, Players, _slots.Seats, Now);

            Assert.That(parsed, Is.False);
            Assert.That(decision.Reason, Is.EqualTo(ApprovalRules.ReasonProtocolMismatch));
        }

        [Test]
        public void Evaluate_InvalidPayload_Rejects()
        {
            ApprovalDecision decision = ApprovalRules.Evaluate(false, default, Players, _slots.Seats, Now);

            Assert.That(decision.Approved, Is.False);
            Assert.That(decision.Reason, Is.EqualTo(ApprovalRules.ReasonInvalidPayload));
        }

        [Test]
        public void Evaluate_UnparsedPayloadWithServerVersion_IsInvalidPayload()
        {
            var partial = new ConnectionPayload { ProtocolVersion = Protocol };

            ApprovalDecision decision = ApprovalRules.Evaluate(false, partial, Players, _slots.Seats, Now);

            Assert.That(decision.Reason, Is.EqualTo(ApprovalRules.ReasonInvalidPayload));
        }

        // ---------------------------------------------------------------- Seats

        [Test]
        public void Evaluate_EmptyRoom_AssignsPlayer1()
        {
            ApprovalDecision decision = Evaluate(Payload("Ana", "u1"));

            Assert.That(decision.Approved, Is.True);
            Assert.That(decision.Outcome, Is.EqualTo(ApprovalOutcome.AssignSeat));
            Assert.That(decision.Seat, Is.EqualTo(PlayerId.Player1));
            Assert.That(decision.Reason, Is.Null);
        }

        [Test]
        public void Evaluate_Player1Taken_AssignsPlayer2()
        {
            Admit(1, "Host", "u1");

            ApprovalDecision decision = Evaluate(Payload("Guest", "u2"));

            Assert.That(decision.Outcome, Is.EqualTo(ApprovalOutcome.AssignSeat));
            Assert.That(decision.Seat, Is.EqualTo(PlayerId.Player2));
        }

        [Test]
        public void Evaluate_FreedPlayer1_IsAssignedBeforePlayer2()
        {
            Admit(1, "A", "u1");
            Admit(2, "B", "u2");
            _slots.Release(1, out _);

            ApprovalDecision decision = Evaluate(Payload("C", "u3"));

            Assert.That(decision.Seat, Is.EqualTo(PlayerId.Player1));
        }

        [Test]
        public void Evaluate_ThirdClient_IsRoomFull()
        {
            Admit(1, "A", "u1");
            Admit(2, "B", "u2");

            ApprovalDecision decision = Evaluate(Payload("Third", "u3"));

            Assert.That(decision.Approved, Is.False);
            Assert.That(decision.Seat, Is.EqualTo(PlayerId.None));
            Assert.That(decision.Reason, Is.EqualTo(ApprovalRules.ReasonRoomFull));
        }

        [Test]
        public void Evaluate_PolicyWithOneSeat_SecondClientIsRoomFull()
        {
            Admit(1, "A", "u1");
            var oneSeat = new ApprovalPolicy(Protocol, 1, Grace);

            ApprovalDecision decision = ApprovalRules.Evaluate(true, Payload("B", "u2"), oneSeat, _slots.Seats, Now);

            Assert.That(decision.Reason, Is.EqualTo(ApprovalRules.ReasonRoomFull));
        }

        [Test]
        public void Evaluate_WithoutUgsId_IsSeatedAndNeverDuplicate()
        {
            Admit(1, "Dev", null);

            ApprovalDecision decision = Evaluate(Payload("Dev", null));

            Assert.That(decision.Outcome, Is.EqualTo(ApprovalOutcome.AssignSeat));
            Assert.That(decision.Seat, Is.EqualTo(PlayerId.Player2));
        }

        // ---------------------------------------------------------------- Duplicates

        [Test]
        public void Evaluate_SameUgsIdAlreadyConnected_IsAlreadyConnected()
        {
            Admit(1, "Ana", "u1");

            ApprovalDecision decision = Evaluate(Payload("Ana again", "u1"));

            Assert.That(decision.Approved, Is.False);
            Assert.That(decision.Reason, Is.EqualTo(ApprovalRules.ReasonAlreadyConnected));
        }

        [Test]
        public void Evaluate_DuplicateCheckWinsOverRoomFull()
        {
            Admit(1, "A", "u1");
            Admit(2, "B", "u2");

            Assert.That(Evaluate(Payload("B", "u2")).Reason, Is.EqualTo(ApprovalRules.ReasonAlreadyConnected));
        }

        // ---------------------------------------------------------------- Reconnection

        [Test]
        public void Evaluate_DroppedPlayerWithinGrace_ReconnectsToItsSeat()
        {
            Admit(1, "A", "u1");
            Admit(2, "B", "u2");
            _slots.MarkDisconnected(2, Now, out _);

            ApprovalDecision decision = ApprovalRules.Evaluate(true, Payload("B", "u2"), Players, _slots.Seats, Now + Grace - 0.5d);

            Assert.That(decision.Approved, Is.True);
            Assert.That(decision.Outcome, Is.EqualTo(ApprovalOutcome.Reconnect));
            Assert.That(decision.Seat, Is.EqualTo(PlayerId.Player2));
        }

        [Test]
        public void Evaluate_OtherPlayerWhileSeatReserved_IsRoomFull()
        {
            Admit(1, "A", "u1");
            Admit(2, "B", "u2");
            _slots.MarkDisconnected(2, Now, out _);

            ApprovalDecision decision = ApprovalRules.Evaluate(true, Payload("C", "u3"), Players, _slots.Seats, Now + 1d);

            Assert.That(decision.Reason, Is.EqualTo(ApprovalRules.ReasonRoomFull));
        }

        [Test]
        public void Evaluate_OtherPlayerAfterGrace_TakesTheExpiredSeat()
        {
            Admit(1, "A", "u1");
            Admit(2, "B", "u2");
            _slots.MarkDisconnected(2, Now, out _);

            ApprovalDecision decision = ApprovalRules.Evaluate(true, Payload("C", "u3"), Players, _slots.Seats, Now + Grace);

            Assert.That(decision.Outcome, Is.EqualTo(ApprovalOutcome.AssignSeat));
            Assert.That(decision.Seat, Is.EqualTo(PlayerId.Player2));
        }

        [Test]
        public void Evaluate_SamePlayerAfterGrace_IsSeatedAsNew()
        {
            Admit(1, "A", "u1");
            _slots.MarkDisconnected(1, Now, out _);

            ApprovalDecision decision = ApprovalRules.Evaluate(true, Payload("A", "u1"), Players, _slots.Seats, Now + Grace + 1d);

            Assert.That(decision.Outcome, Is.EqualTo(ApprovalOutcome.AssignSeat));
            Assert.That(decision.Seat, Is.EqualTo(PlayerId.Player1));
        }

        [Test]
        public void Evaluate_ZeroGrace_NeverReconnects()
        {
            Admit(1, "A", "u1");
            _slots.MarkDisconnected(1, Now, out _);
            var noGrace = new ApprovalPolicy(Protocol, 2, 0f);

            ApprovalDecision decision = ApprovalRules.Evaluate(true, Payload("A", "u1"), noGrace, _slots.Seats, Now);

            Assert.That(decision.Outcome, Is.EqualTo(ApprovalOutcome.AssignSeat));
        }

        [Test]
        public void Evaluate_DroppedSeatWithoutUgsId_CannotBeReclaimedWithinGrace()
        {
            Admit(1, "Dev", null);
            Admit(2, "Dev2", null);
            _slots.MarkDisconnected(2, Now, out _);

            ApprovalDecision decision = ApprovalRules.Evaluate(true, Payload("Dev2", null), Players, _slots.Seats, Now + 1d);

            Assert.That(decision.Reason, Is.EqualTo(ApprovalRules.ReasonRoomFull));
        }

        [Test]
        public void Reconnection_ByUgsIdWithinGrace_RestoresTheSeatWithANewClientId()
        {
            // 2.7 without a resume token: the reserved seat is reclaimed by UgsPlayerId (session membership checked before).
            Admit(1, "A", "u1");
            Admit(2, "B", "u2");
            _slots.TrySetCharacter(PlayerId.Player2, 1);
            _slots.MarkDisconnected(2, Now, out _);
            ConnectionPayload back = Payload("B", "u2");

            ApprovalDecision decision = ApprovalRules.Evaluate(true, back, Players, _slots.Seats, Now + 5d);

            Assert.That(decision.Outcome, Is.EqualTo(ApprovalOutcome.Reconnect));
            Assert.That(_slots.TryApply(decision, 7, back), Is.True);
            Assert.That(_slots.TryGetByClient(7, out PlayerSlot slot), Is.True);
            Assert.That(slot.Id, Is.EqualTo(PlayerId.Player2));
            Assert.That(slot.CharacterIndex, Is.EqualTo(1));
            Assert.That(_slots.CountAwaiting, Is.EqualTo(0));
        }

        [Test]
        public void Reconnection_TwiceInARow_StillWorks()
        {
            Admit(1, "A", "u1");
            Admit(2, "B", "u2");
            ConnectionPayload back = Payload("B", "u2");

            _slots.MarkDisconnected(2, Now, out _);
            ApprovalDecision first = ApprovalRules.Evaluate(true, back, Players, _slots.Seats, Now + 1d);
            Assert.That(_slots.TryApply(first, 7, back), Is.True);

            _slots.MarkDisconnected(7, Now + 2d, out _);
            ApprovalDecision second = ApprovalRules.Evaluate(true, back, Players, _slots.Seats, Now + 3d);

            Assert.That(second.Outcome, Is.EqualTo(ApprovalOutcome.Reconnect));
            Assert.That(second.Seat, Is.EqualTo(PlayerId.Player2));
            Assert.That(_slots.TryApply(second, 8, back), Is.True);
        }

        [Test]
        public void Reconnection_WhileTheOldConnectionIsStillSeated_IsAlreadyConnected()
        {
            // The client retries before the server detected the drop (risk R3): already_connected, not a second seat.
            Admit(1, "A", "u1");
            Admit(2, "B", "u2");

            ApprovalDecision decision = ApprovalRules.Evaluate(true, Payload("B", "u2"), Players, _slots.Seats, Now + 1d);

            Assert.That(decision.Approved, Is.False);
            Assert.That(decision.Reason, Is.EqualTo(ApprovalRules.ReasonAlreadyConnected));
            Assert.That(_slots.ConnectedCount, Is.EqualTo(2));
        }

        [Test]
        public void Reconnection_AfterTheSeatWasReleased_IsSeatedAsNew()
        {
            // A voluntary exit (or a member that left the session) releases the reserved seat at once.
            Admit(1, "A", "u1");
            Admit(2, "B", "u2");
            _slots.TrySetCharacter(PlayerId.Player2, 1);
            _slots.MarkDisconnected(2, Now, out _);
            Assume.That(_slots.ReleaseSeat(PlayerId.Player2), Is.True);

            ApprovalDecision decision = ApprovalRules.Evaluate(true, Payload("B", "u2"), Players, _slots.Seats, Now + 1d);

            Assert.That(decision.Outcome, Is.EqualTo(ApprovalOutcome.AssignSeat));
            Assert.That(decision.Seat, Is.EqualTo(PlayerId.Player2));
            Assert.That(_slots.TryApply(decision, 9, Payload("B", "u2")), Is.True);
            _slots.TryGet(PlayerId.Player2, out PlayerSlot slot);
            Assert.That(slot.HasCharacter, Is.False);
        }

        [Test]
        public void Evaluate_ThirdClientWhileP2Reserved_IsRoomFull_UntilTheGraceEnds()
        {
            _slots.TryAssign(PlayerId.Player1, 0, Payload("A", "u1"));
            _slots.TryAssign(PlayerId.Player2, 1, Payload("B", "u2"));
            _slots.MarkDisconnected(1, Now, out _);
            ConnectionPayload third = Payload("C", "u3");

            ApprovalDecision before = ApprovalRules.Evaluate(true, third, Players, _slots.Seats, Now + Grace - 0.001d);
            ApprovalDecision atExpiry = ApprovalRules.Evaluate(true, third, Players, _slots.Seats, Now + Grace);

            Assert.That(before.Reason, Is.EqualTo(ApprovalRules.ReasonRoomFull));
            Assert.That(atExpiry.Outcome, Is.EqualTo(ApprovalOutcome.AssignSeat));
            Assert.That(atExpiry.Seat, Is.EqualTo(PlayerId.Player2));
        }

        [Test]
        public void Evaluate_ThirdRejectedThenOwnerReturns_ReconnectsWithNewClientId()
        {
            _slots.TryAssign(PlayerId.Player1, 0, Payload("A", "u1"));
            ConnectionPayload owner = Payload("B", "u2");
            _slots.TryAssign(PlayerId.Player2, 1, owner);
            _slots.MarkDisconnected(1, Now, out _);

            ApprovalDecision rejected = ApprovalRules.Evaluate(true, Payload("C", "u3"), Players, _slots.Seats, Now + 1d);
            Assert.That(rejected.Reason, Is.EqualTo(ApprovalRules.ReasonRoomFull));
            Assert.That(_slots.TryApply(rejected, 2, Payload("C", "u3")), Is.False);

            ApprovalDecision reconnect = ApprovalRules.Evaluate(true, owner, Players, _slots.Seats, Now + 2d);
            Assert.That(reconnect.Outcome, Is.EqualTo(ApprovalOutcome.Reconnect));
            Assert.That(reconnect.Seat, Is.EqualTo(PlayerId.Player2));
            Assert.That(_slots.TryApply(reconnect, 3, owner), Is.True);
            Assert.That(_slots.TryGetByClient(3, out PlayerSlot seat), Is.True);
            Assert.That(seat.Id, Is.EqualTo(PlayerId.Player2));
            Assert.That(_slots.TryGetByClient(1, out _), Is.False);
        }

        [Test]
        public void Evaluate_BothSeatsReserved_ThirdIsRoomFull_EachOwnerGetsItsOwnSeat()
        {
            ConnectionPayload first = Payload("A", "u1");
            ConnectionPayload second = Payload("B", "u2");
            _slots.TryAssign(PlayerId.Player1, 0, first);
            _slots.TryAssign(PlayerId.Player2, 1, second);
            _slots.MarkDisconnected(0, Now, out _);
            _slots.MarkDisconnected(1, Now, out _);

            double now = Now + 1d;
            Assert.That(ApprovalRules.Evaluate(true, Payload("C", "u3"), Players, _slots.Seats, now).Reason,
                Is.EqualTo(ApprovalRules.ReasonRoomFull));

            ApprovalDecision p2 = ApprovalRules.Evaluate(true, second, Players, _slots.Seats, now);
            ApprovalDecision p1 = ApprovalRules.Evaluate(true, first, Players, _slots.Seats, now);
            Assert.That(p2.Outcome, Is.EqualTo(ApprovalOutcome.Reconnect));
            Assert.That(p2.Seat, Is.EqualTo(PlayerId.Player2));
            Assert.That(p1.Outcome, Is.EqualTo(ApprovalOutcome.Reconnect));
            Assert.That(p1.Seat, Is.EqualTo(PlayerId.Player1));
        }

        [Test]
        public void Evaluate_ApprovedIsAlwaysASeat()
        {
            ConnectionPayload first = Payload("A", "u1");
            ConnectionPayload second = Payload("B", "u2");
            ApprovalDecision p1 = ApprovalRules.Evaluate(true, first, Players, _slots.Seats, Now);
            Assert.That(p1.Approved, Is.True);
            Assert.That(p1.Seat, Is.EqualTo(PlayerId.Player1));
            Assert.That(_slots.TryApply(p1, 0, first), Is.True);

            ApprovalDecision p2 = ApprovalRules.Evaluate(true, second, Players, _slots.Seats, Now);
            Assert.That(p2.Approved, Is.True);
            Assert.That(p2.Seat, Is.EqualTo(PlayerId.Player2));
            Assert.That(_slots.TryApply(p2, 1, second), Is.True);
            _slots.MarkDisconnected(1, Now, out _);

            ApprovalDecision reconnect = ApprovalRules.Evaluate(true, second, Players, _slots.Seats, Now + 1d);
            Assert.That(reconnect.Approved, Is.True);
            Assert.That(reconnect.Seat, Is.EqualTo(PlayerId.Player2));
        }

        // ---------------------------------------------------------------- Policy and reasons

        [Test]
        public void Policy_FromSettings_MatchesSessionCapacity()
        {
            Assert.That(AlienFarms.Data.NetworkSettings.SessionCapacity, Is.EqualTo(2));
            Assert.That(AlienFarms.Data.NetworkSettings.SessionCapacity, Is.EqualTo(ServerSlotDirectory.Capacity));
            Assert.That(AlienFarms.Data.NetworkSettings.SessionCapacity, Is.EqualTo(AlienFarms.Data.OnlineLobbyChannel.MaxSeats));
        }

        [Test]
        public void Policy_ClampsSeatsAndGrace()
        {
            var policy = new ApprovalPolicy(Protocol, 5, -3f);

            Assert.That(policy.PlayerSeats, Is.EqualTo(ServerSlotDirectory.Capacity));
            Assert.That(policy.ReconnectGraceSeconds, Is.EqualTo(0f));
        }

        [Test]
        public void Policy_FromSettings_IsTwoSeats()
        {
            var settings = UnityEngine.ScriptableObject.CreateInstance<AlienFarms.Data.NetworkSettings>();
            try
            {
                ApprovalPolicy policy = ApprovalPolicy.From(settings);

                Assert.That(policy.ProtocolVersion, Is.EqualTo(settings.ProtocolVersion));
                Assert.That(policy.PlayerSeats, Is.EqualTo(2));
                Assert.That(policy.ReconnectGraceSeconds, Is.EqualTo(settings.ReconnectGraceSeconds));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void IsReservationExpired_OnlyForDroppedSeatsPastTheGrace()
        {
            var connected = new PlayerSlot { Id = PlayerId.Player1, Connected = true };
            var dropped = new PlayerSlot { Id = PlayerId.Player1, Connected = false, DisconnectedAt = Now };

            Assert.That(ApprovalRules.IsReservationExpired(default, Now + 99d, Grace), Is.False);
            Assert.That(ApprovalRules.IsReservationExpired(connected, Now + 99d, Grace), Is.False);
            Assert.That(ApprovalRules.IsReservationExpired(dropped, Now + Grace - 0.01d, Grace), Is.False);
            Assert.That(ApprovalRules.IsReservationExpired(dropped, Now + Grace, Grace), Is.True);
            Assert.That(ApprovalRules.IsSeatFree(default, Now, Grace), Is.True);
            Assert.That(ApprovalRules.IsSeatFree(dropped, Now, Grace), Is.False);
        }

        [Test]
        public void DisconnectReasons_OnlyKnownKeysSurvive()
        {
            Assert.That(DisconnectReasons.Normalize(ApprovalRules.ReasonRoomFull), Is.EqualTo(DisconnectReasons.RoomFull));
            Assert.That(DisconnectReasons.Normalize(DisconnectReasons.ProtocolMismatch), Is.EqualTo(DisconnectReasons.ProtocolMismatch));
            Assert.That(DisconnectReasons.Normalize(DisconnectReasons.AlreadyConnected), Is.EqualTo(DisconnectReasons.AlreadyConnected));
            Assert.That(DisconnectReasons.Normalize(DisconnectReasons.HostDisconnected), Is.EqualTo(DisconnectReasons.HostDisconnected));
            Assert.That(DisconnectReasons.Normalize(DisconnectReasons.ReconnectDenied), Is.EqualTo(DisconnectReasons.ReconnectDenied));
        }

        [TestCase("[Disconnect Event][ClosedByRemote]")]
        [TestCase("")]
        [TestCase(null)]
        public void DisconnectReasons_DropWithoutGameReason_IsConnectionLost(string raw)
        {
            Assert.That(DisconnectReasons.Normalize(raw), Is.EqualTo(DisconnectReasons.ConnectionLost));
        }

        // ---------------------------------------------------------------- Session membership

        [Test]
        public void CheckMembership_NoOnlineSession_IsNotVerified()
        {
            // Direct dev connection: whatever the payload says, nothing is checked.
            Assert.That(ApprovalRules.CheckMembership(false, false, false, false, false), Is.EqualTo(SessionMembership.NotRequired));
            Assert.That(ApprovalRules.CheckMembership(false, false, true, false, true), Is.EqualTo(SessionMembership.NotRequired));
            Assert.That(ApprovalRules.CheckMembership(false, false, true, true, false), Is.EqualTo(SessionMembership.NotRequired));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CheckMembership_HostClient_IsNotVerified(bool expired)
        {
            Assert.That(ApprovalRules.CheckMembership(true, true, false, false, expired), Is.EqualTo(SessionMembership.NotRequired));
            Assert.That(ApprovalRules.CheckMembership(true, true, true, false, expired), Is.EqualTo(SessionMembership.NotRequired));
        }

        [Test]
        public void CheckMembership_OnlineWithoutUgsId_IsRejected()
        {
            Assert.That(ApprovalRules.CheckMembership(false, true, false, false, false), Is.EqualTo(SessionMembership.Rejected));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CheckMembership_ListedId_IsMember(bool expired)
        {
            Assert.That(ApprovalRules.CheckMembership(false, true, true, true, expired), Is.EqualTo(SessionMembership.Member));
        }

        [Test]
        public void CheckMembership_UnlistedId_WaitsThenIsRejected()
        {
            // The host's player list may lag behind the client's join: wait first, reject only once the wait is over.
            Assert.That(ApprovalRules.CheckMembership(false, true, true, false, false), Is.EqualTo(SessionMembership.Unconfirmed));
            Assert.That(ApprovalRules.CheckMembership(false, true, true, false, true), Is.EqualTo(SessionMembership.Rejected));
        }

        [Test]
        public void NeedsIdentity_OnlyForAParsedPayloadOfTheSameProtocol()
        {
            Assert.That(ApprovalRules.NeedsIdentity(true, Payload("A", "u1"), Players), Is.True);
            Assert.That(ApprovalRules.NeedsIdentity(false, Payload("A", "u1"), Players), Is.False);
            Assert.That(ApprovalRules.NeedsIdentity(true, Payload("A", "u1", Protocol + 1), Players), Is.False);
        }

        [TestCase(ApprovalRules.ReasonInvalidPayload)]
        [TestCase(ApprovalRules.ReasonProtocolMismatch)]
        [TestCase(ApprovalRules.ReasonRoomFull)]
        [TestCase(ApprovalRules.ReasonAlreadyConnected)]
        [TestCase(ApprovalRules.ReasonNotSessionMember)]
        [TestCase(DisconnectReasons.ReconnectDenied)]
        [TestCase(DisconnectReasons.ConnectionLost)]
        [TestCase(DisconnectReasons.HostDisconnected)]
        public void EveryRejectionReason_MapsToAnOnlineErrorKey(string reason)
        {
            Assert.That(DisconnectReasons.IsKnown(reason), Is.True);
            Assert.That(OnlineSessionErrorMapper.FromDisconnectReason(reason), Does.StartWith("online.error."));
        }

        // ---------------------------------------------------------------- Helpers

        private ApprovalDecision Evaluate(in ConnectionPayload payload)
        {
            return ApprovalRules.Evaluate(true, payload, Players, _slots.Seats, Now);
        }

        // Runs the rules and applies the result, as the approval handler does.
        private void Admit(ulong clientId, string name, string ugsId)
        {
            ConnectionPayload payload = Payload(name, ugsId);
            ApprovalDecision decision = Evaluate(payload);
            Assume.That(decision.Approved, Is.True, decision.Reason);
            Assume.That(_slots.TryApply(decision, clientId, payload), Is.True);
        }

        private static ConnectionPayload Payload(string name, string ugsId, int protocol = Protocol)
        {
            return ConnectionPayload.Create(protocol, name, ugsId);
        }
    }
}
