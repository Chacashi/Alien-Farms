using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Match;
using NUnit.Framework;
using UnityEngine;

namespace AlienFarms.Tests
{
    public sealed class MatchScoreByTypeTests
    {
        private AnimalData _chicken;
        private AnimalData _cow;

        [SetUp]
        public void SetUp()
        {
            _chicken = ScriptableObject.CreateInstance<AnimalData>();
            _cow = ScriptableObject.CreateInstance<AnimalData>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_chicken);
            Object.DestroyImmediate(_cow);
        }

        [Test]
        public void GetCountByType_CountsOnlyThatTypeInThatZone()
        {
            var score = new MatchScore();
            score.TryAdd(PlayerId.Player1, new object(), 1, _chicken, out _);
            score.TryAdd(PlayerId.Player1, new object(), 1, _chicken, out _);
            score.TryAdd(PlayerId.Player1, new object(), 3, _cow, out _);
            score.TryAdd(PlayerId.Player2, new object(), 1, _chicken, out _);

            Assert.That(score.GetCountByType(PlayerId.Player1, _chicken), Is.EqualTo(2));
            Assert.That(score.GetCountByType(PlayerId.Player1, _cow), Is.EqualTo(1));
            Assert.That(score.GetCountByType(PlayerId.Player2, _chicken), Is.EqualTo(1));
            Assert.That(score.GetCountByType(PlayerId.Player2, _cow), Is.EqualTo(0));
        }

        [Test]
        public void GetCountByType_FollowsMovesAndRemovals()
        {
            var score = new MatchScore();
            var animal = new object();
            score.TryAdd(PlayerId.Player1, animal, 1, _cow, out _);
            score.TryAdd(PlayerId.Player2, animal, 1, _cow, out PlayerId movedFrom);

            Assert.That(movedFrom, Is.EqualTo(PlayerId.Player1));
            Assert.That(score.GetCountByType(PlayerId.Player1, _cow), Is.EqualTo(0));
            Assert.That(score.GetCountByType(PlayerId.Player2, _cow), Is.EqualTo(1));

            score.TryRemove(PlayerId.Player2, animal);
            Assert.That(score.GetCountByType(PlayerId.Player2, _cow), Is.EqualTo(0));
        }

        [Test]
        public void GetCountByType_NullTypeOrNoneOrReset_IsZero()
        {
            var score = new MatchScore();
            score.TryAdd(PlayerId.Player1, new object(), 1, out _);
            score.TryAdd(PlayerId.Player1, new object(), 1, _chicken, out _);

            Assert.That(score.GetCountByType(PlayerId.Player1, null), Is.EqualTo(0));
            Assert.That(score.GetCountByType(PlayerId.None, _chicken), Is.EqualTo(0));

            score.Reset();
            Assert.That(score.GetCountByType(PlayerId.Player1, _chicken), Is.EqualTo(0));
        }

        [Test]
        public void TryAdd_WithType_KeepsPointsAndTotals()
        {
            var score = new MatchScore();
            score.TryAdd(PlayerId.Player1, new object(), 3, _cow, out _);
            score.TryAdd(PlayerId.Player1, new object(), 1, _chicken, out _);

            Assert.That(score.Get(PlayerId.Player1), Is.EqualTo(4));
            Assert.That(score.GetAnimalCount(PlayerId.Player1), Is.EqualTo(2));
        }
    }
}
