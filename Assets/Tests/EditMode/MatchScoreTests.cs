using AlienFarms.Core;
using AlienFarms.Match;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class MatchScoreTests
    {
        [Test]
        public void TryAdd_TracksEachZoneSeparately()
        {
            var score = new MatchScore();

            Assert.That(score.TryAdd(PlayerId.Player1, new object(), out PlayerId movedFrom), Is.True);
            Assert.That(movedFrom, Is.EqualTo(PlayerId.None));
            score.TryAdd(PlayerId.Player1, new object(), out _);
            score.TryAdd(PlayerId.Player2, new object(), out _);

            Assert.That(score.Get(PlayerId.Player1), Is.EqualTo(2));
            Assert.That(score.Get(PlayerId.Player2), Is.EqualTo(1));
        }

        [Test]
        public void TryAdd_Duplicate_IsIgnored()
        {
            var score = new MatchScore();
            var animal = new object();
            score.TryAdd(PlayerId.Player1, animal, out _);

            Assert.That(score.TryAdd(PlayerId.Player1, animal, out PlayerId movedFrom), Is.False);
            Assert.That(movedFrom, Is.EqualTo(PlayerId.None));
            Assert.That(score.Get(PlayerId.Player1), Is.EqualTo(1));
        }

        [Test]
        public void TryAdd_FromOtherZone_MovesAndReportsSource()
        {
            var score = new MatchScore();
            var animal = new object();
            score.TryAdd(PlayerId.Player1, animal, out _);

            Assert.That(score.TryAdd(PlayerId.Player2, animal, out PlayerId movedFrom), Is.True);
            Assert.That(movedFrom, Is.EqualTo(PlayerId.Player1));
            Assert.That(score.Get(PlayerId.Player1), Is.EqualTo(0));
            Assert.That(score.Get(PlayerId.Player2), Is.EqualTo(1));
        }

        [Test]
        public void TryAdd_NoneOrNull_IsIgnored()
        {
            var score = new MatchScore();

            Assert.That(score.TryAdd(PlayerId.None, new object(), out _), Is.False);
            Assert.That(score.TryAdd(PlayerId.Player1, null, out _), Is.False);
            Assert.That(score.Get(PlayerId.None), Is.EqualTo(0));
            Assert.That(score.Get(PlayerId.Player1), Is.EqualTo(0));
            Assert.That(score.Get(PlayerId.Player2), Is.EqualTo(0));
        }

        [Test]
        public void TryRemove_Present_RemovesOnce()
        {
            var score = new MatchScore();
            var animal = new object();
            score.TryAdd(PlayerId.Player2, animal, out _);

            Assert.That(score.TryRemove(PlayerId.Player2, animal), Is.True);
            Assert.That(score.TryRemove(PlayerId.Player2, animal), Is.False);
            Assert.That(score.Get(PlayerId.Player2), Is.EqualTo(0));
        }

        [Test]
        public void TryRemove_AbsentOrWrongZone_ReturnsFalse()
        {
            var score = new MatchScore();
            var animal = new object();
            score.TryAdd(PlayerId.Player1, animal, out _);

            Assert.That(score.TryRemove(PlayerId.Player2, animal), Is.False);
            Assert.That(score.TryRemove(PlayerId.Player1, new object()), Is.False);
            Assert.That(score.TryRemove(PlayerId.None, animal), Is.False);
            Assert.That(score.Get(PlayerId.Player1), Is.EqualTo(1));
        }

        [Test]
        public void Reset_ClearsZones()
        {
            var score = new MatchScore();
            TestScores.AddAnimals(score, PlayerId.Player1, 2);
            TestScores.AddAnimals(score, PlayerId.Player2, 7);

            score.Reset();

            Assert.That(score.Get(PlayerId.Player1), Is.EqualTo(0));
            Assert.That(score.Get(PlayerId.Player2), Is.EqualTo(0));
        }

        [Test]
        public void Evaluate_Player1Ahead_Player1Wins()
        {
            var score = new MatchScore();
            TestScores.AddAnimals(score, PlayerId.Player1, 4);
            TestScores.AddAnimals(score, PlayerId.Player2, 2);

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
            TestScores.AddAnimals(score, PlayerId.Player1, 1);
            TestScores.AddAnimals(score, PlayerId.Player2, 5);

            Assert.That(score.Evaluate().Winner, Is.EqualTo(PlayerId.Player2));
        }

        [Test]
        public void Evaluate_Tie_IsDraw()
        {
            var score = new MatchScore();
            TestScores.AddAnimals(score, PlayerId.Player1, 3);
            TestScores.AddAnimals(score, PlayerId.Player2, 3);

            MatchResult result = score.Evaluate();

            Assert.That(result.Winner, Is.EqualTo(PlayerId.None));
            Assert.That(result.IsDraw, Is.True);
        }

        [Test]
        public void Evaluate_ZeroZero_IsDraw()
        {
            Assert.That(new MatchScore().Evaluate().IsDraw, Is.True);
        }
    }
}
