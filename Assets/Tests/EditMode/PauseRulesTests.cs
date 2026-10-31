using AlienFarms.Core;
using AlienFarms.Match;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class PauseRulesTests
    {
        [TestCase(MatchPhase.None, false)]
        [TestCase(MatchPhase.WaitingForPlayers, true)]
        [TestCase(MatchPhase.Countdown, true)]
        [TestCase(MatchPhase.Playing, true)]
        [TestCase(MatchPhase.Results, false)]
        public void CanPause_DependsOnPhase(MatchPhase phase, bool expected)
        {
            Assert.That(PauseRules.CanPause(phase), Is.EqualTo(expected));
        }
    }
}
