using AlienFarms.Networking.Voice;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests.Voice
{
    public sealed class VoicePositionThrottleTests
    {
        [Test]
        public void ShouldSend_SendsThreeTimesDuringFirstSecondAtThreeHertz()
        {
            var throttle = new VoicePositionThrottle(3);
            int sends = 0;
            for (int tick = 0; tick < 100; tick++)
            {
                if (throttle.ShouldSend(tick / 100d))
                {
                    sends++;
                }
            }

            Assert.That(sends, Is.EqualTo(3));
        }

        [Test]
        public void FirstCallAndFirstCallAfterReset_SendImmediately()
        {
            var throttle = new VoicePositionThrottle(3);
            Assert.That(throttle.ShouldSend(15d), Is.True);
            Assert.That(throttle.ShouldSend(15d), Is.False);

            throttle.Reset();

            Assert.That(throttle.ShouldSend(15d), Is.True);
        }

        [TestCase(0, 1d)]
        [TestCase(1, 1d)]
        [TestCase(4, 0.25d)]
        [TestCase(8, 0.25d)]
        public void ShouldSend_ClampsUpdateRate(int hz, double interval)
        {
            var throttle = new VoicePositionThrottle(hz);
            Assert.That(throttle.ShouldSend(0d), Is.True);
            Assert.That(throttle.ShouldSend(interval - 0.001d), Is.False);
            Assert.That(throttle.ShouldSend(interval), Is.True);
        }
    }
}
