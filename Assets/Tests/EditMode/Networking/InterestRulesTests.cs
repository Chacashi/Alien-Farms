using AlienFarms.Networking.Interest;
using NUnit.Framework;
using UnityEngine;

namespace AlienFarms.Networking.Tests
{
    public sealed class InterestRulesTests
    {
        private static readonly InterestRadii s_radii = InterestRadii.Create(30f, 36f);

        [TestCase(false, 29.9f, true)]
        [TestCase(false, 30f, false)]
        [TestCase(false, 33f, false)]
        [TestCase(true, 33f, true)]
        [TestCase(true, 36f, true)]
        [TestCase(true, 36.1f, false)]
        public void UsesHysteresis(bool wasVisible, float distance, bool expected)
        {
            Assert.That(Evaluate(wasVisible, distance), Is.EqualTo(expected));
        }

        [Test]
        public void OscillationChangesVisibilityOnlyAtTheTwoThresholds()
        {
            float[] distances = { 29f, 33f, 31f, 35f, 33f, 37f, 33f, 31f, 29f };
            bool wasVisible = true;
            for (int i = 0; i < distances.Length; i++)
            {
                bool visible = Evaluate(wasVisible, distances[i]);
                InterestChange expected = i == 5 ? InterestChange.Hide :
                    i == 8 ? InterestChange.Show : InterestChange.None;
                Assert.That(InterestRules.Classify(wasVisible, visible), Is.EqualTo(expected), $"Step {i}");
                wasVisible = visible;
            }
        }

        [TestCase(false, true, false, false, false)]
        [TestCase(true, false, false, false, false)]
        [TestCase(true, true, true, false, false)]
        [TestCase(true, true, false, true, false)]
        [TestCase(true, true, false, false, true)]
        public void BypassesDistance(bool enabled, bool filtered, bool server, bool owner, bool exempt)
        {
            var query = new InterestQuery(enabled, filtered, server, owner, exempt, true, false, 80f * 80f);
            Assert.That(InterestRules.ShouldBeVisible(query, s_radii), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingPositionFailsOpen(bool wasVisible)
        {
            var query = new InterestQuery(true, true, false, false, false, false, wasVisible, 80f * 80f);
            Assert.That(InterestRules.ShouldBeVisible(query, s_radii), Is.True);
        }

        [Test]
        public void DistanceIgnoresHeight()
        {
            Assert.That(InterestRules.SqrHorizontalDistance(Vector3.zero, new Vector3(3f, 100f, 4f)), Is.EqualTo(25f));
        }

        [Test]
        public void ExitRadiusCannotBeSmallerThanEnterRadius()
        {
            var radii = InterestRadii.Create(30f, 20f);
            Assert.That(radii.EnterSqr, Is.EqualTo(900f));
            Assert.That(radii.ExitSqr, Is.EqualTo(900f));
        }

        [TestCase(false, false, InterestChange.None)]
        [TestCase(false, true, InterestChange.Show)]
        [TestCase(true, false, InterestChange.Hide)]
        [TestCase(true, true, InterestChange.None)]
        public void ClassifiesTransitions(bool before, bool after, InterestChange expected)
        {
            Assert.That(InterestRules.Classify(before, after), Is.EqualTo(expected));
        }

        private static bool Evaluate(bool wasVisible, float distance)
        {
            var query = new InterestQuery(true, true, false, false, false, true, wasVisible, distance * distance);
            return InterestRules.ShouldBeVisible(query, s_radii);
        }
    }
}
