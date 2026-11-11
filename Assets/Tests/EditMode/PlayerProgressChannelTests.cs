using AlienFarms.Core;
using AlienFarms.Data;
using NUnit.Framework;
using UnityEngine;

namespace AlienFarms.Tests
{
    /// <summary>Player progress channel: initial state, load/save transitions and change notifications.</summary>
    public sealed class PlayerProgressChannelTests
    {
        private PlayerProgressChannel _channel;
        private int _changed;

        [SetUp]
        public void SetUp()
        {
            _channel = ScriptableObject.CreateInstance<PlayerProgressChannel>();
            _channel.Changed += CountChange;
            _changed = 0;
        }

        [TearDown]
        public void TearDown()
        {
            _channel.Changed -= CountChange;
            Object.DestroyImmediate(_channel);
        }

        [Test]
        public void NewChannel_IsUnavailableWithoutStats()
        {
            Assert.That(_channel.Status, Is.EqualTo(PlayerProgressStatus.Unavailable));
            Assert.That(_channel.SaveStatus, Is.EqualTo(PlayerProgressSaveStatus.Idle));
            Assert.That(_channel.HasStats, Is.False);
            Assert.That(_channel.HasError, Is.False);
            Assert.That(_channel.Stats, Is.EqualTo(default(PlayerStatsSummary)));
        }

        [Test]
        public void SetLoaded_StoresStatsAndRaisesChanged()
        {
            var stats = new PlayerStatsSummary(10, 4, 2, 4, 37, 12);

            _channel.SetLoading();
            _channel.SetLoaded(stats);

            Assert.That(_channel.Status, Is.EqualTo(PlayerProgressStatus.Loaded));
            Assert.That(_channel.HasStats, Is.True);
            Assert.That(_channel.Stats.MatchesPlayed, Is.EqualTo(10));
            Assert.That(_channel.Stats.Wins, Is.EqualTo(4));
            Assert.That(_channel.Stats.Draws, Is.EqualTo(2));
            Assert.That(_channel.Stats.Losses, Is.EqualTo(4));
            Assert.That(_channel.Stats.AnimalsScored, Is.EqualTo(37));
            Assert.That(_channel.Stats.TacklesLanded, Is.EqualTo(12));
            Assert.That(_changed, Is.EqualTo(2));
        }

        [Test]
        public void SetLoaded_SameStatsTwice_RaisesChangedOnce()
        {
            var stats = new PlayerStatsSummary(1, 1, 0, 0, 3, 0);

            _channel.SetLoaded(stats);
            _channel.SetLoaded(stats);

            Assert.That(_changed, Is.EqualTo(1));
        }

        [Test]
        public void SetFailed_KeepsPreviousStatsAndRecordsError()
        {
            var stats = new PlayerStatsSummary(2, 1, 1, 0, 5, 1);
            _channel.SetLoaded(stats);

            _channel.SetFailed(OnlineSessionErrorKeys.NoConnection);

            Assert.That(_channel.Status, Is.EqualTo(PlayerProgressStatus.Failed));
            Assert.That(_channel.LastErrorKey, Is.EqualTo(OnlineSessionErrorKeys.NoConnection));
            Assert.That(_channel.Stats, Is.EqualTo(stats));
            Assert.That(_channel.HasStats, Is.False);
        }

        [Test]
        public void SetLoading_ClearsError()
        {
            _channel.SetFailed(OnlineSessionErrorKeys.Unknown);

            _channel.SetLoading();

            Assert.That(_channel.Status, Is.EqualTo(PlayerProgressStatus.Loading));
            Assert.That(_channel.HasError, Is.False);
        }

        [Test]
        public void SetSaveStatus_ChangesOnlySaveStatus()
        {
            var stats = new PlayerStatsSummary(3, 0, 0, 3, 0, 0);
            _channel.SetLoaded(stats);
            _changed = 0;

            _channel.SetSaveStatus(PlayerProgressSaveStatus.Saving);
            _channel.SetSaveStatus(PlayerProgressSaveStatus.Saving);
            _channel.SetSaveStatus(PlayerProgressSaveStatus.Saved);

            Assert.That(_channel.SaveStatus, Is.EqualTo(PlayerProgressSaveStatus.Saved));
            Assert.That(_channel.Status, Is.EqualTo(PlayerProgressStatus.Loaded));
            Assert.That(_channel.Stats, Is.EqualTo(stats));
            Assert.That(_changed, Is.EqualTo(2));
        }

        [Test]
        public void SetUnavailable_ResetsEverything()
        {
            _channel.SetLoaded(new PlayerStatsSummary(1, 0, 1, 0, 2, 3));
            _channel.SetSaveStatus(PlayerProgressSaveStatus.Saved);

            _channel.SetUnavailable();

            Assert.That(_channel.Status, Is.EqualTo(PlayerProgressStatus.Unavailable));
            Assert.That(_channel.SaveStatus, Is.EqualTo(PlayerProgressSaveStatus.Idle));
            Assert.That(_channel.Stats, Is.EqualTo(default(PlayerStatsSummary)));
        }

        [Test]
        public void SetUnavailable_OnFreshChannel_DoesNotRaiseChanged()
        {
            _channel.SetUnavailable();

            Assert.That(_changed, Is.Zero);
        }

        [Test]
        public void StatsSummary_EqualityComparesAllFields()
        {
            var a = new PlayerStatsSummary(1, 2, 3, 4, 5, 6);

            Assert.That(a, Is.EqualTo(new PlayerStatsSummary(1, 2, 3, 4, 5, 6)));
            Assert.That(a, Is.Not.EqualTo(new PlayerStatsSummary(1, 2, 3, 4, 5, 7)));
        }

        private void CountChange()
        {
            _changed++;
        }
    }
}
