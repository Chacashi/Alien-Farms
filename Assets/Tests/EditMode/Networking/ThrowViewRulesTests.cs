using AlienFarms.Networking.Player;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests
{
    /// <summary>Server validation of the view a remote player reports for its throw.</summary>
    public sealed class ThrowViewRulesTests
    {
        [Test]
        public void AcceptsAViewWithinTolerance()
        {
            Assert.That(ThrowViewRules.IsWithinTolerance(100f, 20f, 110f, 25f, 15f), Is.True);
            Assert.That(ThrowViewRules.IsWithinTolerance(100f, 20f, 100f, 20f, 0f), Is.True);
        }

        [Test]
        public void RejectsYawOrPitchOutsideTolerance()
        {
            Assert.That(ThrowViewRules.IsWithinTolerance(100f, 20f, 130f, 20f, 15f), Is.False);
            Assert.That(ThrowViewRules.IsWithinTolerance(100f, 20f, 100f, -5f, 15f), Is.False);
        }

        [Test]
        public void YawWrapsAroundAFullTurn()
        {
            Assert.That(ThrowViewRules.IsWithinTolerance(355f, 0f, 5f, 0f, 15f), Is.True);
            Assert.That(ThrowViewRules.IsWithinTolerance(-10f, 0f, 350f, 0f, 1f), Is.True);
            Assert.That(ThrowViewRules.IsWithinTolerance(340f, 0f, 20f, 0f, 15f), Is.False);
        }

        [Test]
        public void RejectsNonFiniteValuesAndNegativeTolerance()
        {
            Assert.That(ThrowViewRules.IsWithinTolerance(float.NaN, 0f, 0f, 0f, 15f), Is.False);
            Assert.That(ThrowViewRules.IsWithinTolerance(0f, float.PositiveInfinity, 0f, 0f, 15f), Is.False);
            Assert.That(ThrowViewRules.IsWithinTolerance(0f, 0f, 0f, 0f, -1f), Is.False);
        }
    }
}
