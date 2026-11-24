using AlienFarms.Networking.Interest;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests
{
    public sealed class SpawnFreshnessRulesTests
    {
        [TestCase(100, 100, true)]
        [TestCase(100, 149, true)]
        [TestCase(100, 150, true)]
        [TestCase(100, 151, false)]
        [TestCase(0, 0, true)]
        [TestCase(0, 50, true)]
        [TestCase(0, 51, false)]
        [TestCase(100, 99, true)]
        [TestCase(100, 49, false)]
        public void ChecksAgeAtFiftyTicksPerSecond(int spawnTick, int nowTick, bool expected)
        {
            Assert.That(SpawnFreshnessRules.IsFresh(spawnTick, nowTick, 50), Is.EqualTo(expected));
        }

        [Test]
        public void HandlesTickWrap()
        {
            Assert.That(SpawnFreshnessRules.IsFresh(int.MaxValue, int.MinValue, 50), Is.True);
        }
    }
}
