using AlienFarms.Core;
using AlienFarms.Items;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class BombBlastRulesTests
    {
        private const float Radius = 3.5f;

        [Test]
        public void ShouldStun_RivalInside_True()
        {
            Assert.That(BombBlastRules.ShouldStun(PlayerId.Player1, PlayerId.Player2, 1f, Radius), Is.True);
        }

        [Test]
        public void ShouldStun_ThrowerInside_False()
        {
            Assert.That(BombBlastRules.ShouldStun(PlayerId.Player1, PlayerId.Player1, 0f, Radius), Is.False);
        }

        [Test]
        public void ShouldStun_RivalOutside_False()
        {
            Assert.That(BombBlastRules.ShouldStun(PlayerId.Player1, PlayerId.Player2, 4f * 4f, Radius), Is.False);
        }

        [Test]
        public void ShouldStun_RivalExactlyOnEdge_True()
        {
            Assert.That(BombBlastRules.ShouldStun(PlayerId.Player1, PlayerId.Player2, Radius * Radius, Radius), Is.True);
        }

        [Test]
        public void ShouldStun_TargetNone_False()
        {
            Assert.That(BombBlastRules.ShouldStun(PlayerId.Player1, PlayerId.None, 0f, Radius), Is.False);
        }

        [Test]
        public void ShouldStun_NoThrower_PlayerInside_True()
        {
            Assert.That(BombBlastRules.ShouldStun(PlayerId.None, PlayerId.Player1, 1f, Radius), Is.True);
        }

        [Test]
        public void ShouldDetonateOnContact_ArmedNotThrower_True()
        {
            Assert.That(BombBlastRules.ShouldDetonateOnContact(BombState.Armed, false), Is.True);
        }

        [Test]
        public void ShouldDetonateOnContact_ArmedThrower_False()
        {
            Assert.That(BombBlastRules.ShouldDetonateOnContact(BombState.Armed, true), Is.False);
        }

        [TestCase(BombState.Idle)]
        [TestCase(BombState.Carried)]
        [TestCase(BombState.Inactive)]
        public void ShouldDetonateOnContact_NotArmed_False(BombState state)
        {
            Assert.That(BombBlastRules.ShouldDetonateOnContact(state, false), Is.False);
        }

        [TestCase(3.99f, false)]
        [TestCase(4f, true)]
        [TestCase(5f, true)]
        [TestCase(0f, false)]
        public void HasTimedOut_Limits(float armedTime, bool expected)
        {
            Assert.That(BombBlastRules.HasTimedOut(armedTime, 4f), Is.EqualTo(expected));
        }
    }
}
