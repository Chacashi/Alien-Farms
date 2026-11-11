using System.Reflection;
using AlienFarms.Core;
using AlienFarms.Data;
using NUnit.Framework;
using UnityEngine;

namespace AlienFarms.Tests
{
    public sealed class MatchSessionModeTests
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

        [Test]
        public void New_IsLocal()
        {
            Assert.That(_session.Mode, Is.EqualTo(GameMode.Local));
        }

        [Test]
        public void SetMode_IsKept()
        {
            _session.SetMode(GameMode.Online);

            Assert.That(_session.Mode, Is.EqualTo(GameMode.Online));
        }

        [Test]
        public void Clear_RestoresLocal()
        {
            _session.SetMode(GameMode.Online);
            _session.Clear();

            Assert.That(_session.Mode, Is.EqualTo(GameMode.Local));
        }

        [Test]
        public void ReturnToOnlineMenu_IsConsumedOnce()
        {
            Assert.That(_session.ReturnToOnlineMenu, Is.False);
            _session.Clear();
            _session.RequestReturnToOnlineMenu();

            Assert.That(_session.ConsumeReturnToOnlineMenu(), Is.True);
            Assert.That(_session.ConsumeReturnToOnlineMenu(), Is.False);
        }

        [Test]
        public void Clear_DropsReturnToOnlineMenu()
        {
            _session.RequestReturnToOnlineMenu();
            _session.Clear();

            Assert.That(_session.ReturnToOnlineMenu, Is.False);
        }

        [Test]
        public void AfterResetStatics_ReturnToOnlineMenu_IsDiscarded()
        {
            _session.RequestReturnToOnlineMenu();

            MethodInfo reset = typeof(MatchSession).GetMethod("ResetStatics", BindingFlags.NonPublic | BindingFlags.Static);
            reset.Invoke(null, null);

            Assert.That(_session.ReturnToOnlineMenu, Is.False);
        }

        [Test]
        public void AfterResetStatics_ModeFromPreviousPlaySession_IsDiscarded()
        {
            _session.SetMode(GameMode.Online);

            MethodInfo reset = typeof(MatchSession).GetMethod("ResetStatics", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(reset, Is.Not.Null, "MatchSession.ResetStatics not found.");
            reset.Invoke(null, null);

            Assert.That(_session.Mode, Is.EqualTo(GameMode.Local));
        }
    }
}
