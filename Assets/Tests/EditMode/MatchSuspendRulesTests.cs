using AlienFarms.Core;
using AlienFarms.Match;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    /// <summary>Which phases a dropped player suspends and what resuming does (2.7).</summary>
    public sealed class MatchSuspendRulesTests
    {
        [TestCase(MatchPhase.None, false)]
        [TestCase(MatchPhase.WaitingForPlayers, false)]
        [TestCase(MatchPhase.Countdown, true)]
        [TestCase(MatchPhase.Playing, true)]
        [TestCase(MatchPhase.Results, true)]
        public void SuspendsIn(MatchPhase phase, bool expected)
        {
            Assert.That(MatchSuspendRules.SuspendsIn(phase), Is.EqualTo(expected));
        }

        [TestCase(MatchPhase.None, true, MatchResumeAction.None)]
        [TestCase(MatchPhase.WaitingForPlayers, true, MatchResumeAction.None)]
        [TestCase(MatchPhase.Countdown, true, MatchResumeAction.RestartCountdown)]
        [TestCase(MatchPhase.Playing, true, MatchResumeAction.EnableControls)]
        [TestCase(MatchPhase.Results, true, MatchResumeAction.None)]
        [TestCase(MatchPhase.None, false, MatchResumeAction.None)]
        [TestCase(MatchPhase.WaitingForPlayers, false, MatchResumeAction.None)]
        [TestCase(MatchPhase.Countdown, false, MatchResumeAction.None)]
        [TestCase(MatchPhase.Playing, false, MatchResumeAction.None)]
        [TestCase(MatchPhase.Results, false, MatchResumeAction.None)]
        public void OnResume(MatchPhase phase, bool playersComplete, MatchResumeAction expected)
        {
            Assert.That(MatchSuspendRules.OnResume(phase, playersComplete), Is.EqualTo(expected));
        }
    }
}
