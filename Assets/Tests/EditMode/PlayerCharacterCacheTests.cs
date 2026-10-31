using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.UI;
using NUnit.Framework;
using UnityEngine;

namespace AlienFarms.Tests
{
    public sealed class PlayerCharacterCacheTests
    {
        private CharacterDefinition _samuel;
        private CharacterDefinition _zamb;

        [SetUp]
        public void SetUp()
        {
            _samuel = ScriptableObject.CreateInstance<CharacterDefinition>();
            _zamb = ScriptableObject.CreateInstance<CharacterDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_samuel);
            Object.DestroyImmediate(_zamb);
        }

        [Test]
        public void Empty_ReturnsNull()
        {
            var cache = new PlayerCharacterCache();

            Assert.That(cache.Get(PlayerId.Player1), Is.Null);
            Assert.That(cache.Get(PlayerId.Player2), Is.Null);
        }

        [Test]
        public void Set_StoresPerPlayer()
        {
            var cache = new PlayerCharacterCache();

            cache.Set(PlayerId.Player1, _samuel);
            cache.Set(PlayerId.Player2, _zamb);

            Assert.That(cache.Get(PlayerId.Player1), Is.SameAs(_samuel));
            Assert.That(cache.Get(PlayerId.Player2), Is.SameAs(_zamb));
        }

        [Test]
        public void Set_OverwritesPreviousCharacter()
        {
            var cache = new PlayerCharacterCache();
            cache.Set(PlayerId.Player1, _samuel);

            cache.Set(PlayerId.Player1, _zamb);

            Assert.That(cache.Get(PlayerId.Player1), Is.SameAs(_zamb));
        }

        [Test]
        public void None_IsIgnored()
        {
            var cache = new PlayerCharacterCache();

            cache.Set(PlayerId.None, _samuel);

            Assert.That(cache.Get(PlayerId.None), Is.Null);
            Assert.That(cache.Get(PlayerId.Player1), Is.Null);
        }

        [Test]
        public void Clear_RemovesAll()
        {
            var cache = new PlayerCharacterCache();
            cache.Set(PlayerId.Player1, _samuel);
            cache.Set(PlayerId.Player2, _zamb);

            cache.Clear();

            Assert.That(cache.Get(PlayerId.Player1), Is.Null);
            Assert.That(cache.Get(PlayerId.Player2), Is.Null);
        }
    }
}
