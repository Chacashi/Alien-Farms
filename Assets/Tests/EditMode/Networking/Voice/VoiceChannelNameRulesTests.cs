using AlienFarms.Networking.Voice;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests.Voice
{
    public sealed class VoiceChannelNameRulesTests
    {
        [TestCase("af-", "session-01", "af-session-01")]
        [TestCase("a f-/", "A_/é🙂.%!()+=~", "af-A_.!()+=~")]
        [TestCase("af-", null, null)]
        [TestCase("af-", "", null)]
        [TestCase("af-", "/%", null)]
        public void Build_FiltersOnlyUnsupportedCharacters(string prefix, string id, string expected)
        {
            Assert.That(VoiceChannelNameRules.Build(prefix, id), Is.EqualTo(expected));
        }

        [Test]
        public void Build_TruncatesDeterministicallyAtSdkLimit()
        {
            string id = new string('a', 250);
            string hostName = VoiceChannelNameRules.Build("af-", id);
            string clientName = VoiceChannelNameRules.Build("af-", id);

            Assert.That(hostName, Is.EqualTo("af-" + new string('a', 197)));
            Assert.That(clientName, Is.EqualTo(hostName));
        }
    }
}
