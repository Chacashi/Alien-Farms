using AlienFarms.Networking.Session;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests
{
    /// <summary>
    /// Reclaiming a held lobby membership: the session of the code is reconnected to, another one is left, and one that
    /// cannot be identified is neither.
    /// </summary>
    public sealed class MembershipRulesTests
    {
        [TestCase("BCDFGH", "BCDFGH")]
        [TestCase("bcdfgh", "BCDFGH")]
        [TestCase(" BCDFGH ", "bcdfgh")]
        public void SameCode_IsSame(string sessionCode, string joinCode)
        {
            Assert.That(MembershipRules.Classify(sessionCode, joinCode), Is.EqualTo(MembershipMatch.Same));
        }

        [TestCase("BCDFGH", "BCDFGJ")]
        [TestCase("bcdfgh", "CDFGHJ")]
        public void OtherValidCode_IsOther(string sessionCode, string joinCode)
        {
            Assert.That(MembershipRules.Classify(sessionCode, joinCode), Is.EqualTo(MembershipMatch.Other));
        }

        [TestCase(null, "BCDFGH")]
        [TestCase("", "BCDFGH")]
        [TestCase("   ", "BCDFGH")]
        [TestCase("ABC", "BCDFGH")]
        [TestCase("BCDFGH", null)]
        [TestCase("BCDFGH", "")]
        [TestCase(null, null)]
        [TestCase("ABC", "ABC")]
        public void EmptyOrMalformedCode_IsUnknown_NeverSameNorOther(string sessionCode, string joinCode)
        {
            Assert.That(MembershipRules.Classify(sessionCode, joinCode), Is.EqualTo(MembershipMatch.Unknown));
        }

        [Test]
        public void Unknown_IsTheDefault()
        {
            Assert.That(default(MembershipMatch), Is.EqualTo(MembershipMatch.Unknown));
        }
    }
}
