using AlienFarms.Core;
using AlienFarms.Match;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class MatchScoreTests
    {
        [Test]
        public void Add_TracksEachPlayerSeparately()
        {
            var score = new MatchScore();

            Assert.That(score.Add(PlayerId.Player1), Is.EqualTo(1));
            Assert.That(score.Add(PlayerId.Player1), Is.EqualTo(2));
            Assert.That(score.Add(PlayerId.Player2, 3), Is.EqualTo(3));

            Assert.That(score.Get(PlayerId.Player1), Is.EqualTo(2));
            Assert.That(score.Get(PlayerId.Player2), Is.EqualTo(3));
        }

        [Test]
        public void Add_None_IsIgnored()
        {
            var score = new MatchScore();

            Assert.That(score.Add(PlayerId.None), Is.EqualTo(0));
            Assert.That(score.Get(PlayerId.None), Is.EqualTo(0));
            Assert.That(score.Get(PlayerId.Player1), Is.EqualTo(0));
            Assert.That(score.Get(PlayerId.Player2), Is.EqualTo(0));
        }

        [Test]
        public void Evaluate_Player1Ahead_Player1Wins()
        {
            var score = new MatchScore();
            score.Add(PlayerId.Player1, 4);
            score.Add(PlayerId.Player2, 2);

            MatchResult result = score.Evaluate();

            Assert.That(result.Winner, Is.EqualTo(PlayerId.Player1));
            Assert.That(result.IsDraw, Is.False);
            Assert.That(result.Player1Score, Is.EqualTo(4));
            Assert.That(result.Player2Score, Is.EqualTo(2));
            Assert.That(result.GetScore(PlayerId.Player2), Is.EqualTo(2));
        }

        [Test]
        public void Evaluate_Player2Ahead_Player2Wins()
        {
            var score = new MatchScore();
            score.Add(PlayerId.Player1, 1);
            score.Add(PlayerId.Player2, 5);

            Assert.That(score.Evaluate().Winner, Is.EqualTo(PlayerId.Player2));
        }

        [Test]
        public void Evaluate_Tie_IsDraw()
        {
            var score = new MatchScore();
            score.Add(PlayerId.Player1, 3);
            score.Add(PlayerId.Player2, 3);

            MatchResult result = score.Evaluate();

            Assert.That(result.Winner, Is.EqualTo(PlayerId.None));
            Assert.That(result.IsDraw, Is.True);
        }

        [Test]
        public void Evaluate_ZeroZero_IsDraw()
        {
            Assert.That(new MatchScore().Evaluate().IsDraw, Is.True);
        }

        [Test]
        public void Reset_ClearsScores()
        {
            var score = new MatchScore();
            score.Add(PlayerId.Player1, 2);
            score.Add(PlayerId.Player2, 7);

            score.Reset();

            Assert.That(score.Get(PlayerId.Player1), Is.EqualTo(0));
            Assert.That(score.Get(PlayerId.Player2), Is.EqualTo(0));
        }
    }
}
