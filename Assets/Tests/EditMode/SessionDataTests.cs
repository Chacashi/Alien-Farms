using AlienFarms.Core;
using AlienFarms.Data;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class SessionDataTests
    {
        private static SessionSlot Slot(PlayerId id) => new SessionSlot(id, "Gamepad", null, null);

        [Test]
        public void Empty_IsNotComplete()
        {
            var data = new SessionData();

            Assert.That(data.IsComplete(2), Is.False);
        }

        [Test]
        public void TwoSlots_IsComplete()
        {
            var data = new SessionData();
            data.Set(Slot(PlayerId.Player1));
            data.Set(Slot(PlayerId.Player2));

            Assert.That(data.IsComplete(2), Is.True);
        }

        [Test]
        public void OneSlot_IsCompleteOnlyForOnePlayer()
        {
            var data = new SessionData();
            data.Set(Slot(PlayerId.Player1));

            Assert.That(data.IsComplete(1), Is.True);
            Assert.That(data.IsComplete(2), Is.False);
        }

        [Test]
        public void Clear_IsNotComplete()
        {
            var data = new SessionData();
            data.Set(Slot(PlayerId.Player1));
            data.Set(Slot(PlayerId.Player2));

            data.Clear();

            Assert.That(data.IsComplete(2), Is.False);
            Assert.That(data.TryGet(PlayerId.Player1, out _), Is.False);
        }

        [Test]
        public void TryGet_Missing_ReturnsFalse()
        {
            var data = new SessionData();
            data.Set(Slot(PlayerId.Player1));

            Assert.That(data.TryGet(PlayerId.Player2, out _), Is.False);
            Assert.That(data.TryGet(PlayerId.None, out _), Is.False);
        }

        [Test]
        public void Set_InvalidId_IsIgnored()
        {
            var data = new SessionData();

            Assert.That(data.Set(Slot(PlayerId.None)), Is.False);
            Assert.That(data.Set(Slot((PlayerId)3)), Is.False);
        }

        [Test]
        public void TryGet_ReturnsStoredSlot_WithNullDevices()
        {
            var data = new SessionData();
            data.Set(new SessionSlot(PlayerId.Player2, "KeyboardMouse", null, null));

            Assert.That(data.TryGet(PlayerId.Player2, out SessionSlot slot), Is.True);
            Assert.That(slot.Id, Is.EqualTo(PlayerId.Player2));
            Assert.That(slot.ControlScheme, Is.EqualTo("KeyboardMouse"));
            Assert.That(slot.Devices, Is.Null);
        }

        [Test]
        public void Set_SamePlayer_ReplacesSlot()
        {
            var data = new SessionData();
            data.Set(new SessionSlot(PlayerId.Player1, "Gamepad", null, null));
            data.Set(new SessionSlot(PlayerId.Player1, "KeyboardMouse", null, null));

            data.TryGet(PlayerId.Player1, out SessionSlot slot);
            Assert.That(slot.ControlScheme, Is.EqualTo("KeyboardMouse"));
        }
    }
}
