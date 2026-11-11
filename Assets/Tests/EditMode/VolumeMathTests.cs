using AlienFarms.Audio;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class VolumeMathTests
    {
        private const float Tolerance = 0.01f;

        [Test]
        public void LinearToDecibels_One_IsZero()
        {
            Assert.AreEqual(0f, VolumeMath.LinearToDecibels(1f), Tolerance);
        }

        [Test]
        public void LinearToDecibels_Half_IsAboutMinusSix()
        {
            Assert.AreEqual(-6.02f, VolumeMath.LinearToDecibels(0.5f), Tolerance);
        }

        [TestCase(0f)]
        [TestCase(0.0001f)]
        [TestCase(-1f)]
        public void LinearToDecibels_SilenceOrBelow_IsFloor(float linear)
        {
            Assert.AreEqual(VolumeMath.MinDecibels, VolumeMath.LinearToDecibels(linear));
        }

        [Test]
        public void LinearToDecibels_AboveOne_IsClamped()
        {
            Assert.AreEqual(0f, VolumeMath.LinearToDecibels(2f), Tolerance);
        }

        [Test]
        public void LinearToDecibels_NeverBelowFloor()
        {
            Assert.GreaterOrEqual(VolumeMath.LinearToDecibels(0.00011f), VolumeMath.MinDecibels);
        }

        [TestCase(-80f, 0f)]
        [TestCase(-120f, 0f)]
        [TestCase(0f, 1f)]
        [TestCase(10f, 1f)]
        public void DecibelsToLinear_Limits(float decibels, float expected)
        {
            Assert.AreEqual(expected, VolumeMath.DecibelsToLinear(decibels), Tolerance);
        }

        [TestCase(0.25f)]
        [TestCase(0.5f)]
        [TestCase(0.7f)]
        [TestCase(1f)]
        public void RoundTrip_ReturnsSameLevel(float linear)
        {
            Assert.AreEqual(linear, VolumeMath.DecibelsToLinear(VolumeMath.LinearToDecibels(linear)), 0.001f);
        }
    }
}
