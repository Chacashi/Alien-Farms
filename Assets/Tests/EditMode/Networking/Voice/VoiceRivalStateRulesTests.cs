using AlienFarms.Core;
using AlienFarms.Networking.Voice;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests.Voice
{
    public sealed class VoiceRivalStateRulesTests
    {
        [TestCase(false, -1f, VoiceRivalState.NoVoice)]
        [TestCase(false, 10f, VoiceRivalState.NoVoice)]
        [TestCase(false, 35f, VoiceRivalState.NoVoice)]
        [TestCase(true, -1f, VoiceRivalState.InRange)]
        [TestCase(true, 10f, VoiceRivalState.InRange)]
        [TestCase(true, 30f, VoiceRivalState.InRange)]
        [TestCase(true, 35f, VoiceRivalState.OutOfRange)]
        public void Evaluate_UsesPresenceAndAudibleDistance(bool present, float distance, VoiceRivalState expected)
        {
            Assert.That(VoiceRivalStateRules.Evaluate(present, false, true, distance, 30f, 0d, null).State,
                Is.EqualTo(expected));
        }

        [TestCase(35f)]
        [TestCase(30f)]
        [TestCase(25.1f)]
        public void MissingHeardRival_StaysOutOfRangeUntilInnerBoundary(float distance)
        {
            var (state, deadline) = VoiceRivalStateRules.Evaluate(false, true, true, distance, 30f, 100d, 3d);
            Assert.That(state, Is.EqualTo(VoiceRivalState.OutOfRange));
            Assert.That(deadline, Is.Null);
        }

        [Test]
        public void MissingHeardRival_ExpiresThreeSecondsAfterCrossingInnerBoundary()
        {
            var (state, deadline) = VoiceRivalStateRules.Evaluate(false, true, true, 25f, 30f, 10d, null);
            Assert.That(state, Is.EqualTo(VoiceRivalState.OutOfRange));
            Assert.That(deadline, Is.EqualTo(13d));

            Assert.That(VoiceRivalStateRules.Evaluate(false, true, true, 20f, 30f, 12.99d, deadline).State,
                Is.EqualTo(VoiceRivalState.OutOfRange));
            Assert.That(VoiceRivalStateRules.Evaluate(false, true, true, 20f, 30f, 13d, deadline).State,
                Is.EqualTo(VoiceRivalState.NoVoice));
        }

        [TestCase(true, true, 24f, VoiceRivalState.InRange)]
        [TestCase(true, false, 28f, VoiceRivalState.NoVoice)]
        [TestCase(false, false, 35f, VoiceRivalState.NoVoice)]
        [TestCase(false, true, -1f, VoiceRivalState.NoVoice)]
        public void Evaluate_ClearsDeadlineOnReturnDisconnectOrLostDistance(bool present, bool connected,
            float distance, VoiceRivalState expected)
        {
            var (state, deadline) = VoiceRivalStateRules.Evaluate(present, true, connected, distance, 30f, 1d, 3d);
            Assert.That(state, Is.EqualTo(expected));
            Assert.That(deadline, Is.Null);
        }
    }
}
