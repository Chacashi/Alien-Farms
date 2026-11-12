using AlienFarms.Animals;
using AlienFarms.Core;
using AlienFarms.Match;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class CaptureRulesTests
    {
        [TestCase(PlayerId.Player1)]
        [TestCase(PlayerId.Player2)]
        public void Captured_CountsForPlayerZone(PlayerId owner)
        {
            Assert.That(CaptureRules.CountsForZone(AnimalState.Captured, owner), Is.True);
        }

        [Test]
        public void Captured_ZoneOwnerNone_DoesNotCount()
        {
            Assert.That(CaptureRules.CountsForZone(AnimalState.Captured, PlayerId.None), Is.False);
        }

        [TestCase(AnimalState.Inactive)]
        [TestCase(AnimalState.Wandering)]
        [TestCase(AnimalState.Fleeing)]
        [TestCase(AnimalState.Carried)]
        [TestCase(AnimalState.Airborne)]
        public void NotCaptured_NeverCounts(AnimalState state)
        {
            Assert.That(CaptureRules.CountsForZone(state, PlayerId.Player1), Is.False);
            Assert.That(CaptureRules.CountsForZone(state, PlayerId.Player2), Is.False);
        }

        [TestCase(MatchPhase.None, false)]
        [TestCase(MatchPhase.WaitingForPlayers, false)]
        [TestCase(MatchPhase.Countdown, false)]
        [TestCase(MatchPhase.Playing, true)]
        [TestCase(MatchPhase.Results, false)]
        public void PublishesScore_OnlyWhilePlaying(MatchPhase phase, bool expected)
        {
            Assert.That(CaptureRules.PublishesScore(phase), Is.EqualTo(expected));
        }

        [TestCase(MatchPhase.None, false)]
        [TestCase(MatchPhase.WaitingForPlayers, false)]
        [TestCase(MatchPhase.Countdown, false)]
        [TestCase(MatchPhase.Playing, true)]
        [TestCase(MatchPhase.Results, false)]
        public void TracksScore_OnlyWhilePlaying(MatchPhase phase, bool expected)
        {
            Assert.That(CaptureRules.TracksScore(phase), Is.EqualTo(expected));
        }

        [TestCase(MatchPhase.None, false)]
        [TestCase(MatchPhase.WaitingForPlayers, false)]
        [TestCase(MatchPhase.Countdown, false)]
        [TestCase(MatchPhase.Playing, true)]
        [TestCase(MatchPhase.Results, false)]
        public void Capture_CountsOnlyWhilePlaying(MatchPhase phase, bool expected)
        {
            Assert.That(CaptureRules.CountsForZone(AnimalState.Captured, PlayerId.Player1, phase), Is.EqualTo(expected));
            Assert.That(CaptureRules.CountsForZone(AnimalState.Captured, PlayerId.Player2, phase), Is.EqualTo(expected));
        }

        [Test]
        public void Capture_WhilePlaying_StillNeedsACapturedAnimalInAPlayerZone()
        {
            Assert.That(CaptureRules.CountsForZone(AnimalState.Airborne, PlayerId.Player1, MatchPhase.Playing), Is.False);
            Assert.That(CaptureRules.CountsForZone(AnimalState.Captured, PlayerId.None, MatchPhase.Playing), Is.False);
        }
    }
}
