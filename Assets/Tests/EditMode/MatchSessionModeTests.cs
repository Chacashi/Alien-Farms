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
        private CharacterDefinition _character;

        [SetUp]
        public void SetUp()
        {
            _session = ScriptableObject.CreateInstance<MatchSession>();
            _character = ScriptableObject.CreateInstance<CharacterDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_session);
            Object.DestroyImmediate(_character);
        }

        [Test]
        public void New_IsLocalWithoutCharacter()
        {
            Assert.That(_session.Mode, Is.EqualTo(GameMode.Local));
            Assert.That(_session.LocalCharacter, Is.Null);
        }

        [Test]
        public void SetMode_And_SetLocalCharacter_AreKept()
        {
            _session.SetMode(GameMode.Online);
            _session.SetLocalCharacter(_character);

            Assert.That(_session.Mode, Is.EqualTo(GameMode.Online));
            Assert.That(_session.LocalCharacter, Is.SameAs(_character));
        }

        [Test]
        public void Clear_RestoresLocal()
        {
            _session.SetMode(GameMode.Online);
            _session.SetLocalCharacter(_character);
            _session.Clear();

            Assert.That(_session.Mode, Is.EqualTo(GameMode.Local));
            Assert.That(_session.LocalCharacter, Is.Null);
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
