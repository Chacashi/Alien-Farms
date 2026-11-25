using AlienFarms.Networking.Voice;
using NUnit.Framework;
using UnityEngine;

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

        [TestCase(0.049f, 0f, 1d, false)]
        [TestCase(0.051f, 0f, 1d, true)]
        [TestCase(0f, 1.9f, 1d, false)]
        [TestCase(0f, 2.1f, 1d, true)]
        [TestCase(0f, 0f, 1.999d, false)]
        [TestCase(0f, 0f, 2d, true)]
        public void ShouldSendPose_UsesMotionEpsilonsAndKeepAlive(float displacement, float angle, double elapsed,
            bool expected)
        {
            Vector3 forward = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            Assert.That(VoicePositionThrottle.ShouldSendPose(Vector3.right * displacement, forward,
                Vector3.zero, Vector3.forward, elapsed), Is.EqualTo(expected));
        }

        [Test]
        public void KeepAliveDeadline_CanSampleBeforeNextScheduledTick()
        {
            var throttle = new VoicePositionThrottle(3);
            Assert.That(throttle.ShouldSend(1.9d), Is.True);
            Assert.That(throttle.ShouldSend(1.99d, 2d), Is.False);
            Assert.That(throttle.ShouldSend(2d, 2d), Is.True);
            Assert.That(throttle.ShouldSend(2.1d), Is.False);
        }
    }
}
