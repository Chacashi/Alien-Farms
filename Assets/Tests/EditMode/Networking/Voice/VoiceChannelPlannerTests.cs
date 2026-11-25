using AlienFarms.Networking.Voice;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests.Voice
{
    public sealed class VoiceChannelPlannerTests
    {
        [TestCase(null, null, false, VoiceChannelStep.None)]
        [TestCase("a", "a", false, VoiceChannelStep.None)]
        [TestCase("a", null, false, VoiceChannelStep.Join)]
        [TestCase(null, "a", false, VoiceChannelStep.Leave)]
        [TestCase("a", "b", false, VoiceChannelStep.LeaveThenJoin)]
        [TestCase("a", "b", true, VoiceChannelStep.None)]
        [TestCase(null, "a", true, VoiceChannelStep.None)]
        [TestCase("a", null, true, VoiceChannelStep.None)]
        public void Next_ReconcilesWhenIdle(string desired, string joined, bool busy, VoiceChannelStep expected)
        {
            Assert.That(VoiceChannelPlanner.Next(desired, joined, busy), Is.EqualTo(expected));
        }
    }
}
