using AlienFarms.Core;
using AlienFarms.Networking.Voice;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests.Voice
{
    public sealed class VoiceDesiredChannelRulesTests
    {
        [TestCase(OnlineSessionStatus.SignedOut, null)]
        [TestCase(OnlineSessionStatus.SigningIn, null)]
        [TestCase(OnlineSessionStatus.Ready, null)]
        [TestCase(OnlineSessionStatus.Creating, null)]
        [TestCase(OnlineSessionStatus.Joining, null)]
        [TestCase(OnlineSessionStatus.InSession, "af-session")]
        [TestCase(OnlineSessionStatus.Leaving, null)]
        public void Desired_UsesOnlyAnActiveSession(OnlineSessionStatus status, string expected)
        {
            Assert.That(VoiceDesiredChannelRules.Desired(status, "session", "af-"), Is.EqualTo(expected));
        }
    }
}
