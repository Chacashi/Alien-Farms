using AlienFarms.Items;
using NUnit.Framework;
using UnityEngine;

namespace AlienFarms.Tests
{
    public sealed class BombSpawnScheduleTests
    {
        private const int MaxActive = 1;
        private const float Tolerance = 0.0001f;

        [Test]
        public void WithoutStart_NeverDue()
        {
            var schedule = new BombSpawnSchedule();

            Assert.That(schedule.IsRunning, Is.False);
            Assert.That(schedule.Tick(100f, 0, MaxActive), Is.False);
        }

        [Test]
        public void Start_DueOnlyAfterInitialDelay()
        {
            var schedule = new BombSpawnSchedule();
            schedule.Start(2f);

            Assert.That(schedule.Tick(1.9f, 0, MaxActive), Is.False);
            Assert.That(schedule.Tick(0.1f, 0, MaxActive), Is.True);
        }

        [Test]
        public void SlotsFull_TimerPaused()
        {
            var schedule = new BombSpawnSchedule();
            schedule.Start(2f);

            Assert.That(schedule.Tick(10f, MaxActive, MaxActive), Is.False);
            Assert.That(schedule.TimeUntilNext, Is.EqualTo(2f).Within(Tolerance));
        }

        [Test]
        public void StaysDueUntilOutcomeReported()
        {
            var schedule = new BombSpawnSchedule();
            schedule.Start(0f);

            Assert.That(schedule.Tick(0.02f, 0, MaxActive), Is.True);
            Assert.That(schedule.Tick(0.02f, 0, MaxActive), Is.True);
        }

        [Test]
        public void OnSpawned_NextDueAfterIntervalWithFreeSlot()
        {
            var schedule = new BombSpawnSchedule();
            schedule.Start(0f);
            Assert.That(schedule.Tick(0.02f, 0, MaxActive), Is.True);
            schedule.OnSpawned(15f);

            // While the bomb is in play, nothing accumulates.
            Assert.That(schedule.Tick(30f, 1, MaxActive), Is.False);

            Assert.That(schedule.Tick(10f, 0, MaxActive), Is.False);
            Assert.That(schedule.Tick(4.9f, 0, MaxActive), Is.False);
            Assert.That(schedule.Tick(0.1f, 0, MaxActive), Is.True);
        }

        [Test]
        public void OnSpawnFailed_RetriesAfterDelay()
        {
            var schedule = new BombSpawnSchedule();
            schedule.Start(0f);
            Assert.That(schedule.Tick(0.02f, 0, MaxActive), Is.True);
            schedule.OnSpawnFailed(1f);

            Assert.That(schedule.Tick(0.5f, 0, MaxActive), Is.False);
            Assert.That(schedule.Tick(0.5f, 0, MaxActive), Is.True);
        }

        [Test]
        public void Stop_CutsAndStartRestarts()
        {
            var schedule = new BombSpawnSchedule();
            schedule.Start(2f);
            schedule.Tick(1.5f, 0, MaxActive);
            schedule.Stop();

            Assert.That(schedule.IsRunning, Is.False);
            Assert.That(schedule.Tick(10f, 0, MaxActive), Is.False);

            schedule.Start(2f);
            Assert.That(schedule.Tick(1.5f, 0, MaxActive), Is.False);
            Assert.That(schedule.Tick(0.5f, 0, MaxActive), Is.True);
        }

        [TestCase(0f, 15f)]
        [TestCase(0.5f, 20f)]
        [TestCase(1f, 25f)]
        public void NextInterval_LerpsRange(float value01, float expected)
        {
            var rng = ScriptedRandomSource.WithFloats(value01);

            float interval = BombSpawnSchedule.NextInterval(new Vector2(15f, 25f), rng);

            Assert.That(interval, Is.EqualTo(expected).Within(Tolerance));
        }
    }
}
