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
            Assert.That(VoiceRivalStateRules.Evaluate(present, distance, 30f), Is.EqualTo(expected));
        }
    }
}
