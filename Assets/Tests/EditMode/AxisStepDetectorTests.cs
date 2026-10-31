using AlienFarms.Input;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class AxisStepDetectorTests
    {
        private const float Threshold = 0.5f;

        [Test]
        public void BelowThreshold_GivesNoStep()
        {
            var detector = new AxisStepDetector();

            Assert.That(detector.Update(0.4f, Threshold), Is.EqualTo(0));
            Assert.That(detector.Update(-0.4f, Threshold), Is.EqualTo(0));
        }

        [TestCase(0.8f, 1)]
        [TestCase(-0.8f, -1)]
        public void CrossingThreshold_GivesOneStepInItsDirection(float axis, int expected)
        {
            var detector = new AxisStepDetector();

            Assert.That(detector.Update(axis, Threshold), Is.EqualTo(expected));
        }

        [Test]
        public void Holding_DoesNotRepeat()
        {
            var detector = new AxisStepDetector();
            detector.Update(1f, Threshold);

            Assert.That(detector.Update(1f, Threshold), Is.EqualTo(0));
            Assert.That(detector.Update(0.7f, Threshold), Is.EqualTo(0));
        }

        [Test]
        public void ReturningToCenter_Rearms()
        {
            var detector = new AxisStepDetector();
            detector.Update(1f, Threshold);

            Assert.That(detector.Update(0f, Threshold), Is.EqualTo(0));
            Assert.That(detector.Update(1f, Threshold), Is.EqualTo(1));
        }

        [Test]
        public void FlippingDirection_GivesStepInNewDirection()
        {
            var detector = new AxisStepDetector();
            detector.Update(1f, Threshold);

            Assert.That(detector.Update(-1f, Threshold), Is.EqualTo(-1));
        }

        [Test]
        public void Reset_Rearms()
        {
            var detector = new AxisStepDetector();
            detector.Update(1f, Threshold);

            detector.Reset();

            Assert.That(detector.Update(1f, Threshold), Is.EqualTo(1));
        }
    }
}
