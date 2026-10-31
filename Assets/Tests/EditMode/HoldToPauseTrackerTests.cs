using AlienFarms.Input;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class HoldToPauseTrackerTests
    {
        private const float Duration = 3f;

        [Test]
        public void NotHeld_StaysAtZero()
        {
            var tracker = new HoldToPauseTracker(Duration);

            bool completed = tracker.Tick(false, 1f);

            Assert.That(completed, Is.False);
            Assert.That(tracker.Progress, Is.EqualTo(0f));
        }

        [Test]
        public void HeldShorterThanDuration_DoesNotComplete()
        {
            var tracker = new HoldToPauseTracker(Duration);

            bool completed = Hold(tracker, 2.9f);

            Assert.That(completed, Is.False);
            Assert.That(tracker.Progress, Is.EqualTo(2.9f / Duration).Within(0.001f));
        }

        [Test]
        public void CrossingDuration_CompletesExactlyOnce()
        {
            var tracker = new HoldToPauseTracker(Duration);
            Hold(tracker, 2.9f);

            Assert.That(tracker.Tick(true, 0.2f), Is.True);
            Assert.That(tracker.Progress, Is.EqualTo(1f));
            Assert.That(tracker.Tick(true, 0.1f), Is.False);
        }

        [Test]
        public void KeepHolding_AfterCompletion_DoesNotRepeat()
        {
            var tracker = new HoldToPauseTracker(Duration);
            Hold(tracker, Duration + 0.1f);

            bool repeated = Hold(tracker, 10f);

            Assert.That(repeated, Is.False);
        }

        [Test]
        public void EarlyRelease_ResetsProgress()
        {
            var tracker = new HoldToPauseTracker(Duration);
            Hold(tracker, 2f);

            tracker.Tick(false, 0.1f);

            Assert.That(tracker.Progress, Is.EqualTo(0f));
            Assert.That(Hold(tracker, 2f), Is.False);
        }

        [Test]
        public void AfterCompletion_ReleaseRearms()
        {
            var tracker = new HoldToPauseTracker(Duration);
            Assert.That(Hold(tracker, Duration + 0.1f), Is.True);

            tracker.Tick(false, 0.1f);

            Assert.That(Hold(tracker, Duration + 0.1f), Is.True);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        public void NonPositiveOrInvalidDelta_DoesNotAdvance(float deltaTime)
        {
            var tracker = new HoldToPauseTracker(Duration);

            bool completed = tracker.Tick(true, deltaTime);

            Assert.That(completed, Is.False);
            Assert.That(tracker.Progress, Is.EqualTo(0f));
        }

        [TestCase(0f)]
        [TestCase(-5f)]
        [TestCase(float.NaN)]
        public void InvalidDuration_IsClampedToMinimum(float duration)
        {
            var tracker = new HoldToPauseTracker(duration);

            Assert.That(tracker.Duration, Is.EqualTo(HoldToPauseTracker.MinDuration));
            Assert.That(tracker.Tick(true, HoldToPauseTracker.MinDuration), Is.True);
        }

        /// <summary>Holds in 0.1 s steps for <paramref name="seconds"/>; returns true if any tick completed.</summary>
        private static bool Hold(HoldToPauseTracker tracker, float seconds)
        {
            const float step = 0.1f;
            bool completed = false;
            float elapsed = 0f;
            while (elapsed < seconds - 0.00001f)
            {
                float dt = seconds - elapsed < step ? seconds - elapsed : step;
                completed |= tracker.Tick(true, dt);
                elapsed += dt;
            }

            return completed;
        }
    }
}
