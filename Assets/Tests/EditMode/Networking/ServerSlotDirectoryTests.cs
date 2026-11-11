using AlienFarms.Core;
using AlienFarms.Networking.Session;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests
{
    /// <summary>Seat table (task 2.2): seats start without a character and the online room sets it.</summary>
    public sealed class ServerSlotDirectoryTests
    {
        private const int Protocol = 1;

        [Test]
        public void TryAssign_IgnoresPayloadCharacter()
        {
            var slots = new ServerSlotDirectory();
            ConnectionPayload payload = ConnectionPayload.Create(Protocol, "Ana", 3);
            Assume.That(payload.HasCharacter, Is.True);

            Assert.That(slots.TryAssign(1, payload, out PlayerId id), Is.True);

            Assert.That(slots.TryGet(id, out PlayerSlot slot), Is.True);
            Assert.That(slot.CharacterIndex, Is.EqualTo(ConnectionPayload.NoCharacter));
            Assert.That(slot.DisplayName.ToString(), Is.EqualTo("Ana"));
        }

        [Test]
        public void TrySetCharacter_OccupiedSeat_StoresIndex()
        {
            var slots = new ServerSlotDirectory();
            slots.TryAssign(1, ConnectionPayload.Create(Protocol, "A", -1), out _);
            slots.TryAssign(2, ConnectionPayload.Create(Protocol, "B", -1), out _);

            Assert.That(slots.TrySetCharacter(PlayerId.Player2, 1), Is.True);

            slots.TryGet(PlayerId.Player1, out PlayerSlot first);
            slots.TryGet(PlayerId.Player2, out PlayerSlot second);
            Assert.That(first.CharacterIndex, Is.EqualTo(ConnectionPayload.NoCharacter));
            Assert.That(second.CharacterIndex, Is.EqualTo(1));
            Assert.That(second.ClientId, Is.EqualTo(2UL));
        }

        [Test]
        public void TrySetCharacter_CanResetToNoCharacter()
        {
            var slots = new ServerSlotDirectory();
            slots.TryAssign(1, ConnectionPayload.Create(Protocol, "A", -1), out PlayerId id);
            slots.TrySetCharacter(id, 0);

            Assert.That(slots.TrySetCharacter(id, ConnectionPayload.NoCharacter), Is.True);
            slots.TryGet(id, out PlayerSlot slot);
            Assert.That(slot.CharacterIndex, Is.EqualTo(ConnectionPayload.NoCharacter));
        }

        [Test]
        public void TrySetCharacter_EmptySeat_ReturnsFalse()
        {
            var slots = new ServerSlotDirectory();
            slots.TryAssign(1, ConnectionPayload.Create(Protocol, "A", -1), out _);

            Assert.That(slots.TrySetCharacter(PlayerId.Player2, 0), Is.False);
            Assert.That(slots.TryGet(PlayerId.Player2, out _), Is.False);
        }

        [TestCase(PlayerId.None)]
        [TestCase((PlayerId)3)]
        public void TrySetCharacter_InvalidId_ReturnsFalse(PlayerId id)
        {
            var slots = new ServerSlotDirectory();
            slots.TryAssign(1, ConnectionPayload.Create(Protocol, "A", -1), out _);

            Assert.That(slots.TrySetCharacter(id, 0), Is.False);
        }

        [Test]
        public void TrySetCharacter_AfterRelease_ReturnsFalse()
        {
            var slots = new ServerSlotDirectory();
            slots.TryAssign(1, ConnectionPayload.Create(Protocol, "A", -1), out PlayerId id);
            slots.Release(1, out _);

            Assert.That(slots.TrySetCharacter(id, 0), Is.False);
        }

        [Test]
        public void TryAssign_AfterRelease_StartsWithoutPreviousCharacter()
        {
            var slots = new ServerSlotDirectory();
            slots.TryAssign(1, ConnectionPayload.Create(Protocol, "A", -1), out PlayerId id);
            slots.TrySetCharacter(id, 2);
            slots.Release(1, out _);

            slots.TryAssign(5, ConnectionPayload.Create(Protocol, "C", 2), out PlayerId reused);

            slots.TryGet(reused, out PlayerSlot slot);
            Assert.That(reused, Is.EqualTo(id));
            Assert.That(slot.CharacterIndex, Is.EqualTo(ConnectionPayload.NoCharacter));
        }
    }
}
