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
        public void GarbageTimes_StayInRange()
        {
            Assert.That(MatchClock.DisplaySeconds(MatchPhase.Playing, double.NaN, 0d, 300f), Is.EqualTo(0));
            Assert.That(MatchClock.DisplaySeconds(MatchPhase.Playing, double.MaxValue, 0d, 300f), Is.GreaterThan(0));
        }
    }
}
