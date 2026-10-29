using AlienFarms.Match;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class MatchTimerTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void Tick_SubtractsAndClampsAtZero()
        {
            var timer = new MatchTimer(10f);

            timer.Tick(3f);
            Assert.That(timer.Remaining, Is.EqualTo(7f).Within(Tolerance));

            timer.Tick(100f);
            Assert.That(timer.Remaining, Is.EqualTo(0f));
            Assert.That(timer.IsFinished, Is.True);
        }

        [Test]
        public void Tick_ReturnsTrueOnlyOnFinishingTick()
        {
            var timer = new MatchTimer(1f);

            Assert.That(timer.Tick(0.5f), Is.False);
            Assert.That(timer.Tick(0.6f), Is.True);
            Assert.That(timer.Tick(0.5f), Is.False);
        }

        [TestCase(299.2f, 300)]
        [TestCase(0.01f, 1)]
        [TestCase(0f, 0)]
        [TestCase(300f, 300)]
        public void DisplaySeconds_RoundsUp(float remaining, int expected)
        {
            var timer = new MatchTimer(remaining);

            Assert.That(timer.DisplaySeconds, Is.EqualTo(expected));
        }

        [Test]
        public void Reset_RestoresDuration()
        {
            var timer = new MatchTimer(5f);
            timer.Tick(10f);

            timer.Reset(30f);

            Assert.That(timer.Remaining, Is.EqualTo(30f));
            Assert.That(timer.IsFinished, Is.False);
        }
    }
}
