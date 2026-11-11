using AlienFarms.Audio;
using NUnit.Framework;
using UnityEngine;

namespace AlienFarms.Tests
{
    public sealed class AudioVariationTests
    {
        [Test]
        public void PickRandom_NullOrEmpty_ReturnsNull()
        {
            Assert.IsNull(AudioVariation.PickRandom<string>(null, new ScriptedRandomSource()));
            Assert.IsNull(AudioVariation.PickRandom(new string[0], new ScriptedRandomSource()));
        }

        [Test]
        public void PickRandom_ReturnsRolledEntry()
        {
            var items = new[] { "a", "b", "c" };
            Assert.AreEqual("b", AudioVariation.PickRandom(items, new ScriptedRandomSource(1)));
            Assert.AreEqual("c", AudioVariation.PickRandom(items, new ScriptedRandomSource(2)));
        }

        [Test]
        public void PickRandom_SkipsNullSlotsForward()
        {
            var items = new[] { "a", null, null };
            Assert.AreEqual("a", AudioVariation.PickRandom(items, new ScriptedRandomSource(1)));
        }

        [Test]
        public void PickRandom_AllNull_ReturnsNull()
        {
            var items = new string[] { null, null };
            Assert.IsNull(AudioVariation.PickRandom(items, new ScriptedRandomSource(1)));
        }

        [TestCase(0f, 0.92f)]
        [TestCase(0.5f, 1f)]
        [TestCase(1f, 1.08f)]
        public void PickPitch_LerpsInsideRange(float roll, float expected)
        {
            float pitch = AudioVariation.PickPitch(new Vector2(0.92f, 1.08f), ScriptedRandomSource.WithFloats(roll));
            Assert.AreEqual(expected, pitch, 1e-4f);
        }

        [Test]
        public void PickPitch_InvertedOrOutOfBoundsRange_IsClamped()
        {
            float inverted = AudioVariation.PickPitch(new Vector2(1.2f, 0.8f), ScriptedRandomSource.WithFloats(1f));
            Assert.AreEqual(1.2f, inverted, 1e-4f);

            float tooLow = AudioVariation.PickPitch(new Vector2(-1f, -1f), ScriptedRandomSource.WithFloats(0.5f));
            Assert.AreEqual(AudioVariation.MinPitch, tooLow, 1e-4f);
        }

        [Test]
        public void SelectVoice_NoVoices_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, AudioVariation.SelectVoice(null, 0));
            Assert.AreEqual(-1, AudioVariation.SelectVoice(new bool[0], 0));
        }

        [Test]
        public void SelectVoice_PicksFirstIdleFromNext()
        {
            var busy = new[] { false, true, true, false };
            Assert.AreEqual(3, AudioVariation.SelectVoice(busy, 1));
            Assert.AreEqual(0, AudioVariation.SelectVoice(busy, 0));
        }

        [Test]
        public void SelectVoice_AllBusy_StealsNext()
        {
            var busy = new[] { true, true, true };
            Assert.AreEqual(2, AudioVariation.SelectVoice(busy, 2));
        }

        [Test]
        public void SelectVoice_WrapsOutOfRangeNext()
        {
            var busy = new[] { false, false };
            Assert.AreEqual(1, AudioVariation.SelectVoice(busy, 3));
            Assert.AreEqual(1, AudioVariation.SelectVoice(busy, -1));
        }

        [TestCase(0f, 1f)]
        [TestCase(0.5f, 1.25f)]
        [TestCase(1f, 1.5f)]
        [TestCase(3f, 1.5f)]
        public void RampPitch_RisesLinearlyThenHolds(float elapsed, float expected)
        {
            Assert.AreEqual(expected, AudioVariation.RampPitch(1f, elapsed, 1f, 1.5f), 1e-5f);
        }

        [Test]
        public void RampPitch_ScalesBasePitch()
        {
            Assert.AreEqual(1.8f, AudioVariation.RampPitch(1.2f, 2f, 1f, 1.5f), 1e-5f);
        }

        [Test]
        public void RampPitch_ZeroRampTime_JumpsToTarget()
        {
            Assert.AreEqual(2f, AudioVariation.RampPitch(1f, 0f, 0f, 2f), 1e-5f);
        }

        [Test]
        public void RampPitch_ClampsToMaxPitch()
        {
            Assert.AreEqual(AudioVariation.MaxPitch, AudioVariation.RampPitch(2.5f, 1f, 1f, 3f), 1e-5f);
        }
    }
}
