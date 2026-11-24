using AlienFarms.Core;
using AlienFarms.Networking.Persistence;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests.Persistence
{
    public sealed class MatchStatsTallyTests
    {
        private static readonly MatchResult Player1Wins = new MatchResult(PlayerId.Player1, 5, 2);

        private MatchStatsTally _tally;

        [SetUp]
        public void SetUp()
        {
            _tally = new MatchStatsTally();
        }

        [Test]
        public void RoundSeenLive_CompletesOnce()
        {
            PlayLive();

            Assert.That(_tally.TryComplete(PlayerId.Player1, Player1Wins, 3, 0, out PlayerStatsSummary delta), Is.True);
            Assert.That(delta, Is.EqualTo(new PlayerStatsSummary(1, 1, 0, 0, 3, 0)));
            Assert.That(_tally.TryComplete(PlayerId.Player1, Player1Wins, 3, 0, out _), Is.False, "the replica may re-raise the result");
        }

        [Test]
        public void JoiningStraightIntoResults_DoesNotCount()
        {
            _tally.OnPhaseChanged(MatchPhase.None, MatchPhase.Results);

            Assert.That(_tally.TryComplete(PlayerId.Player2, Player1Wins, 0, 0, out _), Is.False);
        }

        [Test]
        public void RejoiningDuringPlaying_Counts()
        {
            _tally.OnPhaseChanged(MatchPhase.None, MatchPhase.Playing);
            _tally.OnPhaseChanged(MatchPhase.Playing, MatchPhase.Results);

            Assert.That(_tally.TryComplete(PlayerId.Player2, Player1Wins, 0, 0, out PlayerStatsSummary delta), Is.True);
            Assert.That(delta.Losses, Is.EqualTo(1));
        }

        [Test]
        public void NoLocalSeat_RecordsNothing()
        {
            PlayLive();

            Assert.That(_tally.TryComplete(PlayerId.None, Player1Wins, 0, 0, out _), Is.False);
        }

        [Test]
        public void Rematch_CountsTheNextRoundToo()
        {
            PlayLive();
            _tally.TryComplete(PlayerId.Player1, Player1Wins, 1, 0, out _);

            _tally.OnPhaseChanged(MatchPhase.Results, MatchPhase.Countdown);
            _tally.OnPhaseChanged(MatchPhase.Countdown, MatchPhase.Playing);
            _tally.OnPhaseChanged(MatchPhase.Playing, MatchPhase.Results);

            Assert.That(_tally.TryComplete(PlayerId.Player1, Player1Wins, 1, 0, out _), Is.True);
        }

        [Test]
        public void AbortToWaiting_ForgetsTheRound()
        {
            _tally.OnPhaseChanged(MatchPhase.Countdown, MatchPhase.Playing);
            _tally.OnPhaseChanged(MatchPhase.Playing, MatchPhase.WaitingForPlayers);
            _tally.OnPhaseChanged(MatchPhase.WaitingForPlayers, MatchPhase.Results);

            Assert.That(_tally.TryComplete(PlayerId.Player1, Player1Wins, 0, 0, out _), Is.False);
        }

        [Test]
        public void TryCompleteEarly_OnlyDuringLivePlaying()
        {
            Assert.That(_tally.TryCompleteEarly(false, 0, 0, out _), Is.False);

            _tally.OnPhaseChanged(MatchPhase.Countdown, MatchPhase.Playing);
            Assert.That(_tally.IsRoundInProgress, Is.True);
            Assert.That(_tally.TryCompleteEarly(false, 2, 0, out PlayerStatsSummary loss), Is.True);
            Assert.That(loss, Is.EqualTo(new PlayerStatsSummary(1, 0, 0, 1, 2, 0)));
            Assert.That(_tally.TryCompleteEarly(true, 2, 0, out _), Is.False, "once per round");
        }

        [Test]
        public void TryCompleteEarly_Win_CountsAWin()
        {
            _tally.OnPhaseChanged(MatchPhase.Countdown, MatchPhase.Playing);

            Assert.That(_tally.TryCompleteEarly(true, 1, 0, out PlayerStatsSummary win), Is.True);
            Assert.That(win.Wins, Is.EqualTo(1));
        }

        [Test]
        public void TryCompleteEarly_InResults_IsRefused()
        {
            PlayLive();

            Assert.That(_tally.TryCompleteEarly(false, 0, 0, out _), Is.False);
        }

        private void PlayLive()
        {
            _tally.OnPhaseChanged(MatchPhase.WaitingForPlayers, MatchPhase.Countdown);
            _tally.OnPhaseChanged(MatchPhase.Countdown, MatchPhase.Playing);
            _tally.OnPhaseChanged(MatchPhase.Playing, MatchPhase.Results);
        }
    }
}
