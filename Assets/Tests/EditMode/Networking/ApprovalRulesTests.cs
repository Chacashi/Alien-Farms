using AlienFarms.Core;
using AlienFarms.Networking.Session;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests
{
    /// <summary>Connection approval rules, payload serialization and seat assignment (version 1.5).</summary>
    public sealed class ApprovalRulesTests
    {
        private const int Protocol = 1;

        [Test]
        public void Evaluate_ValidPayloadWithFreeSeat_Approves()
        {
            ConnectionPayload payload = ConnectionPayload.Create(Protocol, "Ana", 0);

            ApprovalDecision decision = ApprovalRules.Evaluate(true, payload, Protocol, 1, 2);

            Assert.That(decision.Approved, Is.True);
            Assert.That(decision.Reason, Is.Null);
        }

        [Test]
        public void Evaluate_RoomFull_RejectsWithReason()
        {
            ConnectionPayload payload = ConnectionPayload.Create(Protocol, "Third", -1);

            ApprovalDecision decision = ApprovalRules.Evaluate(true, payload, Protocol, 2, 2);

            Assert.That(decision.Approved, Is.False);
            Assert.That(decision.Reason, Is.EqualTo(ApprovalRules.ReasonRoomFull));
        }

        [Test]
        public void Evaluate_ProtocolMismatch_Rejects()
        {
            ConnectionPayload payload = ConnectionPayload.Create(Protocol + 1, "Old", -1);

            ApprovalDecision decision = ApprovalRules.Evaluate(true, payload, Protocol, 0, 2);

            Assert.That(decision.Approved, Is.False);
            Assert.That(decision.Reason, Is.EqualTo(ApprovalRules.ReasonProtocolMismatch));
        }

        [Test]
        public void Evaluate_InvalidPayload_Rejects()
        {
            ApprovalDecision decision = ApprovalRules.Evaluate(false, default, Protocol, 0, 2);

            Assert.That(decision.Approved, Is.False);
            Assert.That(decision.Reason, Is.EqualTo(ApprovalRules.ReasonInvalidPayload));
        }

        [Test]
        public void Payload_RoundTripsThroughBytes()
        {
            ConnectionPayload payload = ConnectionPayload.Create(Protocol, "Zambblarp", 2);

            bool parsed = ConnectionPayload.TryParse(payload.ToBytes(), out ConnectionPayload result);

            Assert.That(parsed, Is.True);
            Assert.That(result.ProtocolVersion, Is.EqualTo(Protocol));
            Assert.That(result.DisplayName.ToString(), Is.EqualTo("Zambblarp"));
            Assert.That(result.CharacterIndex, Is.EqualTo(2));
            Assert.That(result.HasCharacter, Is.True);
        }

        [Test]
        public void Payload_LongNameIsTruncatedToFixedCapacity()
        {
            ConnectionPayload payload = ConnectionPayload.Create(Protocol, new string('a', 64), -1);

            Assert.That(payload.DisplayName.Length, Is.EqualTo(payload.DisplayName.Capacity));
            Assert.That(payload.HasCharacter, Is.False);
        }

        [Test]
        public void Payload_TryParseRejectsGarbage()
        {
            Assert.That(ConnectionPayload.TryParse(null, out _), Is.False);
            Assert.That(ConnectionPayload.TryParse(new byte[0], out _), Is.False);
            Assert.That(ConnectionPayload.TryParse(new byte[] { 1, 2 }, out _), Is.False);
        }

        [Test]
        public void Payload_HugeOrInvalidStringLengthIsRejectedWithoutThrowing()
        {
            byte[] valid = ConnectionPayload.Create(Protocol, "Ana", 0).ToBytes();

            // Bytes 4..7 are the DisplayName length prefix: claim 2^32-1 bytes, then just over the capacity.
            byte[] huge = (byte[])valid.Clone();
            huge[4] = 0xFF;
            huge[5] = 0xFF;
            huge[6] = 0xFF;
            huge[7] = 0xFF;

            byte[] overCapacity = (byte[])valid.Clone();
            overCapacity[4] = 30;
            overCapacity[5] = 0;
            overCapacity[6] = 0;
            overCapacity[7] = 0;

            byte[] truncated = new byte[valid.Length - 2];
            System.Array.Copy(valid, truncated, truncated.Length);

            Assert.DoesNotThrow(() => ConnectionPayload.TryParse(huge, out _));
            Assert.That(ConnectionPayload.TryParse(huge, out _), Is.False);
            Assert.That(ConnectionPayload.TryParse(overCapacity, out _), Is.False);
            Assert.That(ConnectionPayload.TryParse(truncated, out _), Is.False);
        }

        [Test]
        public void DisconnectReasons_OnlyKnownKeysSurvive()
        {
            Assert.That(DisconnectReasons.Normalize(ApprovalRules.ReasonRoomFull), Is.EqualTo(DisconnectReasons.RoomFull));
            Assert.That(DisconnectReasons.Normalize(DisconnectReasons.ProtocolMismatch), Is.EqualTo(DisconnectReasons.ProtocolMismatch));
            Assert.That(DisconnectReasons.Normalize("[Disconnect Event][ClosedByRemote]"), Is.EqualTo(DisconnectReasons.HostDisconnected));
            Assert.That(DisconnectReasons.Normalize(null), Is.EqualTo(DisconnectReasons.HostDisconnected));
        }

        [Test]
        public void Directory_AssignsPlayer1ThenPlayer2_AndRefusesThird()
        {
            var slots = new ServerSlotDirectory();
            ConnectionPayload payload = ConnectionPayload.Create(Protocol, "P", -1);

            Assert.That(slots.TryAssign(0, payload, out PlayerId first), Is.True);
            Assert.That(slots.TryAssign(5, payload, out PlayerId second), Is.True);
            Assert.That(slots.TryAssign(9, payload, out PlayerId third), Is.False);

            Assert.That(first, Is.EqualTo(PlayerId.Player1));
            Assert.That(second, Is.EqualTo(PlayerId.Player2));
            Assert.That(third, Is.EqualTo(PlayerId.None));
            Assert.That(slots.ConnectedCount, Is.EqualTo(2));
        }

        [Test]
        public void Directory_ReleaseFreesTheSeatForTheNextClient()
        {
            var slots = new ServerSlotDirectory();
            ConnectionPayload payload = ConnectionPayload.Create(Protocol, "P", -1);
            slots.TryAssign(0, payload, out _);
            slots.TryAssign(5, payload, out _);

            Assert.That(slots.Release(0, out PlayerId released), Is.True);
            Assert.That(released, Is.EqualTo(PlayerId.Player1));
            Assert.That(slots.TryAssign(7, payload, out PlayerId reused), Is.True);
            Assert.That(reused, Is.EqualTo(PlayerId.Player1));
            Assert.That(slots.TryGetByClient(7, out PlayerSlot slot) && slot.Id == PlayerId.Player1, Is.True);
        }

        [Test]
        public void Directory_SameClientCannotTakeTwoSeats()
        {
            var slots = new ServerSlotDirectory();
            ConnectionPayload payload = ConnectionPayload.Create(Protocol, "P", -1);
            slots.TryAssign(3, payload, out _);

            Assert.That(slots.TryAssign(3, payload, out _), Is.False);
            Assert.That(slots.ConnectedCount, Is.EqualTo(1));
        }
    }
}
