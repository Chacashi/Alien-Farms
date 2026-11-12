using AlienFarms.Core;
using AlienFarms.Networking.Match;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests
{
    /// <summary>Seconds a client derives from the replicated server times.</summary>
    public sealed class MatchClockTests
    {
        [Test]
        public void Playing_CountsDownToTheEndTimeRoundingUp()
        {
            Assert.That(MatchClock.DisplaySeconds(MatchPhase.Playing, 400d, 100d, 300f), Is.EqualTo(300));
            Assert.That(MatchClock.DisplaySeconds(MatchPhase.Playing, 400d, 100.01d, 300f), Is.EqualTo(300));
            Assert.That(MatchClock.DisplaySeconds(MatchPhase.Playing, 400d, 101d, 300f), Is.EqualTo(299));
            Assert.That(MatchClock.DisplaySeconds(MatchPhase.Playing, 400d, 399.5d, 300f), Is.EqualTo(1));
        }

        [Test]
        public void Playing_NeverGoesBelowZero()
        {
            Assert.That(MatchClock.DisplaySeconds(MatchPhase.Playing, 400d, 400d, 300f), Is.EqualTo(0));
            Assert.That(MatchClock.DisplaySeconds(MatchPhase.Playing, 400d, 450d, 300f), Is.EqualTo(0));
        }

        [Test]
        public void OtherPhases_ShowFullDurationOrZero()
        {
            Assert.That(MatchClock.DisplaySeconds(MatchPhase.WaitingForPlayers, 0d, 50d, 300f), Is.EqualTo(300));
            Assert.That(MatchClock.DisplaySeconds(MatchPhase.Countdown, 0d, 50d, 299.5f), Is.EqualTo(300));
            Assert.That(MatchClock.DisplaySeconds(MatchPhase.Results, 400d, 10d, 300f), Is.EqualTo(0));
        }

        [Test]
        public void Countdown_RoundsUpAndNeverShowsZero()
        {
            Assert.That(MatchClock.CountdownSecondsLeft(13d, 10d), Is.EqualTo(3));
            Assert.That(MatchClock.CountdownSecondsLeft(13d, 10.5d), Is.EqualTo(3));
            Assert.That(MatchClock.CountdownSecondsLeft(13d, 11.2d), Is.EqualTo(2));
            Assert.That(MatchClock.CountdownSecondsLeft(13d, 12.9d), Is.EqualTo(1));
            Assert.That(MatchClock.CountdownSecondsLeft(13d, 14d), Is.EqualTo(1));
        }

        [Test]
        public void Frozen_ShowsTheFrozenSecondsWhateverTheTime()
        {
            // Round end long passed, but the clock froze with 42.3 s left: clients keep showing 43.
            Assert.That(MatchClock.DisplaySeconds(MatchPhase.Playing, 100d, 500d, 300f, 42.3f), Is.EqualTo(43));
            Assert.That(MatchClock.DisplaySeconds(MatchPhase.Playing, 100d, 900d, 300f, 42.3f), Is.EqualTo(43));
            Assert.That(MatchClock.DisplaySeconds(MatchPhase.Playing, 100d, 0d, 300f, 0f), Is.EqualTo(0));
        }

        [Test]
        public void Frozen_IgnoredOutsidePlaying()
        {
            Assert.That(MatchClock.DisplaySeconds(MatchPhase.Results, 400d, 10d, 300f, 42f), Is.EqualTo(0));
            Assert.That(MatchClock.DisplaySeconds(MatchPhase.Countdown, 0d, 10d, 300f, 42f), Is.EqualTo(300));
            Assert.That(MatchClock.DisplaySeconds(MatchPhase.WaitingForPlayers, 0d, 10d, 300f, 42f), Is.EqualTo(300));
        }

        [Test]
        public void Running_NegativeFrozen_CountsDownLikeBefore()
        {
            Assert.That(MatchClock.DisplaySeconds(MatchPhase.Playing, 400d, 101d, 300f, -1f), Is.EqualTo(299));
            Assert.That(MatchClock.DisplaySeconds(MatchPhase.Playing, 400d, 450d, 300f, -1f), Is.EqualTo(0));
        }

        [Test]
        public void Resumed_ContinuesFromTheFrozenValueWithoutJumps()
        {
            // Frozen with 42.3 s left; the server resumes at server time 1000 rewriting RoundEndTime = now + remaining.
            const float frozen = 42.3f;
            const double resumeAt = 1000d;
            double roundEnd = resumeAt + frozen;

            int beforeResume = MatchClock.DisplaySeconds(MatchPhase.Playing, 100d, resumeAt, 300f, frozen);
            int atResume = MatchClock.DisplaySeconds(MatchPhase.Playing, roundEnd, resumeAt, 300f, -1f);
            int later = MatchClock.DisplaySeconds(MatchPhase.Playing, roundEnd, resumeAt + 10d, 300f, -1f);

            Assert.That(atResume, Is.EqualTo(beforeResume));
            Assert.That(later, Is.EqualTo(33));
        }

        [Test]
        public void Countdown_TicksOnlyWhileTheClockRuns()
        {
            Assert.That(MatchClock.TicksCountdown(MatchPhase.Countdown, -1f), Is.True);
            Assert.That(MatchClock.TicksCountdown(MatchPhase.Countdown, 0f), Is.False);
            Assert.That(MatchClock.TicksCountdown(MatchPhase.Countdown, 120f), Is.False);
            Assert.That(MatchClock.TicksCountdown(MatchPhase.Playing, -1f), Is.False);
            Assert.That(MatchClock.TicksCountdown(MatchPhase.WaitingForPlayers, -1f), Is.False);
        }

        [Test]
        public void GarbageTimes_StayInRange()
        {
            Assert.That(MatchClock.DisplaySeconds(MatchPhase.Playing, double.NaN, 0d, 300f), Is.EqualTo(0));
            Assert.That(MatchClock.DisplaySeconds(MatchPhase.Playing, double.MaxValue, 0d, 300f), Is.GreaterThan(0));
        }
    }
}
