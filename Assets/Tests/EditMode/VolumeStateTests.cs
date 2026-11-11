using System;
using AlienFarms.Audio;
using AlienFarms.Data;
using NUnit.Framework;
using UnityEngine;

namespace AlienFarms.Tests
{
    public sealed class VolumeStateTests
    {
        private AudioVolumeSettings _defaults;
        private FakeVolumeStore _store;

        [SetUp]
        public void SetUp()
        {
            // Field initializers: Master 1, Music 0.7, SFX 1, not muted.
            _defaults = ScriptableObject.CreateInstance<AudioVolumeSettings>();
            _store = new FakeVolumeStore();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_defaults);
        }

        [Test]
        public void Constructor_NullStore_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new VolumeState(null, _defaults));
        }

        [Test]
        public void EmptyStore_UsesDefaults()
        {
            var state = new VolumeState(_store, _defaults);

            Assert.AreEqual(1f, state.GetLevel(VolumeChannel.Master));
            Assert.AreEqual(0.7f, state.GetLevel(VolumeChannel.Music), 0.0001f);
            Assert.AreEqual(1f, state.GetLevel(VolumeChannel.Sfx));
            Assert.IsFalse(state.IsMuted);
        }

        [Test]
        public void NullDefaults_UsesFullVolumeUnmuted()
        {
            var state = new VolumeState(_store, null);

            Assert.AreEqual(1f, state.GetLevel(VolumeChannel.Music));
            Assert.IsFalse(state.IsMuted);
        }

        [Test]
        public void SavedValues_OverrideDefaults()
        {
            _store.Floats[VolumeState.MusicKey] = 0.2f;
            _store.Bools[VolumeState.MutedKey] = true;

            var state = new VolumeState(_store, _defaults);

            Assert.AreEqual(0.2f, state.GetLevel(VolumeChannel.Music), 0.0001f);
            Assert.IsTrue(state.IsMuted);
        }

        [Test]
        public void SavedValueOutOfRange_IsClamped()
        {
            _store.Floats[VolumeState.SfxKey] = 3f;

            var state = new VolumeState(_store, _defaults);

            Assert.AreEqual(1f, state.GetLevel(VolumeChannel.Sfx));
        }

        [Test]
        public void SetLevel_ClampsAndWritesThrough()
        {
            var state = new VolumeState(_store, _defaults);

            Assert.IsTrue(state.SetLevel(VolumeChannel.Master, -0.5f));
            Assert.AreEqual(0f, state.GetLevel(VolumeChannel.Master));
            Assert.AreEqual(0f, _store.Floats[VolumeState.MasterKey]);
        }

        [Test]
        public void SetLevel_SameValue_ReturnsFalse()
        {
            var state = new VolumeState(_store, _defaults);

            Assert.IsFalse(state.SetLevel(VolumeChannel.Sfx, 1f));
            Assert.IsFalse(_store.Floats.ContainsKey(VolumeState.SfxKey));
        }

        [Test]
        public void SetMuted_WritesThroughAndReportsChange()
        {
            var state = new VolumeState(_store, _defaults);

            Assert.IsTrue(state.SetMuted(true));
            Assert.IsFalse(state.SetMuted(true));
            Assert.IsTrue(_store.Bools[VolumeState.MutedKey]);
        }

        [Test]
        public void Muted_SilencesMasterOnly()
        {
            var state = new VolumeState(_store, _defaults);
            state.SetMuted(true);

            Assert.AreEqual(VolumeMath.MinDecibels, state.GetAppliedDecibels(VolumeChannel.Master));
            Assert.AreEqual(VolumeMath.LinearToDecibels(0.7f), state.GetAppliedDecibels(VolumeChannel.Music));
            Assert.AreEqual(1f, state.GetLevel(VolumeChannel.Master), "Muting keeps the master level.");
        }

        [Test]
        public void Unmuted_AppliesLevelInDecibels()
        {
            var state = new VolumeState(_store, _defaults);
            state.SetLevel(VolumeChannel.Master, 0.5f);

            Assert.AreEqual(VolumeMath.LinearToDecibels(0.5f), state.GetAppliedDecibels(VolumeChannel.Master));
        }

        [Test]
        public void Save_FlushesStore()
        {
            var state = new VolumeState(_store, _defaults);

            state.Save();

            Assert.AreEqual(1, _store.SaveCount);
        }

        [Test]
        public void InvalidChannel_Throws()
        {
            var state = new VolumeState(_store, _defaults);

            Assert.Throws<ArgumentOutOfRangeException>(() => state.GetLevel((VolumeChannel)7));
            Assert.Throws<ArgumentOutOfRangeException>(() => VolumeState.GetKey((VolumeChannel)7));
        }

        [Test]
        public void Keys_AreDistinct()
        {
            CollectionAssert.AllItemsAreUnique(new[]
            {
                VolumeState.GetKey(VolumeChannel.Master),
                VolumeState.GetKey(VolumeChannel.Music),
                VolumeState.GetKey(VolumeChannel.Sfx),
                VolumeState.MutedKey
            });
        }
    }
}
