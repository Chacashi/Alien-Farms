using System.Reflection;
using AlienFarms.Core;
using AlienFarms.Data;
using NUnit.Framework;
using UnityEngine;

namespace AlienFarms.Tests
{
    public sealed class MatchSessionTests
    {
        private MatchSession _session;

        [SetUp]
        public void SetUp()
        {
            _session = ScriptableObject.CreateInstance<MatchSession>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_session);
        }

        private static SessionSlot Slot(PlayerId id) => new SessionSlot(id, "Gamepad", null, null);

        [Test]
        public void New_IsNotReady()
        {
            Assert.That(_session.IsReady, Is.False);
            Assert.That(_session.TryGetSlot(PlayerId.Player1, out _), Is.False);
        }

        [Test]
        public void TwoSlots_IsReady()
        {
            _session.SetSlot(Slot(PlayerId.Player1));
            _session.SetSlot(Slot(PlayerId.Player2));

            Assert.That(_session.IsReady, Is.True);
        }

        [Test]
        public void OneSlot_IsNotReady()
        {
            _session.SetSlot(Slot(PlayerId.Player1));

            Assert.That(_session.IsReady, Is.False);
            Assert.That(_session.TryGetSlot(PlayerId.Player1, out _), Is.True);
        }

        [Test]
        public void Clear_IsNotReady()
        {
            _session.SetSlot(Slot(PlayerId.Player1));
            _session.SetSlot(Slot(PlayerId.Player2));

            _session.Clear();

            Assert.That(_session.IsReady, Is.False);
            Assert.That(_session.TryGetSlot(PlayerId.Player2, out _), Is.False);
        }

        [Test]
        public void TryGetSlot_Missing_ReturnsFalse()
        {
            _session.SetSlot(Slot(PlayerId.Player1));

            Assert.That(_session.TryGetSlot(PlayerId.Player2, out _), Is.False);
        }

        [Test]
        public void AfterResetStatics_DataFromPreviousPlaySession_IsDiscarded()
        {
            _session.SetSlot(Slot(PlayerId.Player1));
            _session.SetSlot(Slot(PlayerId.Player2));
            Assert.That(_session.IsReady, Is.True);

            MethodInfo reset = typeof(MatchSession).GetMethod("ResetStatics", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(reset, Is.Not.Null, "MatchSession.ResetStatics not found.");
            reset.Invoke(null, null);

            Assert.That(_session.IsReady, Is.False);
            Assert.That(_session.TryGetSlot(PlayerId.Player1, out _), Is.False);

            // Writing again starts a fresh session (the stale slot of Player 2 is not resurrected).
            _session.SetSlot(Slot(PlayerId.Player1));
            Assert.That(_session.IsReady, Is.False);
            Assert.That(_session.TryGetSlot(PlayerId.Player2, out _), Is.False);
        }

        [Test]
        public void TryGetSlot_KeepsNullDevicesAndCharacter()
        {
            _session.SetSlot(new SessionSlot(PlayerId.Player1, "KeyboardMouse", null, null));

            Assert.That(_session.TryGetSlot(PlayerId.Player1, out SessionSlot slot), Is.True);
            Assert.That(slot.Devices, Is.Null);
            Assert.That(slot.Character, Is.Null);
            Assert.That(slot.ControlScheme, Is.EqualTo("KeyboardMouse"));
        }
    }
}
