using System.Collections.Generic;
using System.Threading.Tasks;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Networking.Persistence;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AlienFarms.Networking.Tests.Persistence
{
    public sealed class PlayerStatsSyncTests
    {
        private const string Owner = "ugs-owner";
        private const string Rival = "ugs-rival";

        private static readonly PlayerStatsSummary Remote = new PlayerStatsSummary(5, 3, 1, 1, 20, 0);
        private static readonly PlayerStatsSummary WinDelta = new PlayerStatsSummary(1, 1, 0, 0, 4, 0);

        private FakePendingStatsStore _pending;
        private FakePlayerStatsStore _store;
        private PlayerStatsLedger _ledger;
        private OnlineSessionChannel _session;
        private PlayerProgressChannel _progress;
        private NetworkSettings _settings;
        private List<float> _delays;
        private PlayerStatsSync _sync;

        [SetUp]
        public void SetUp()
        {
            _pending = new FakePendingStatsStore();
            _store = new FakePlayerStatsStore();
            _ledger = new PlayerStatsLedger(_pending);
            _session = ScriptableObject.CreateInstance<OnlineSessionChannel>();
            _progress = ScriptableObject.CreateInstance<PlayerProgressChannel>();
            _settings = ScriptableObject.CreateInstance<NetworkSettings>();
            SetBool(_settings, "_logPersistence", false);
            _delays = new List<float>();
            _sync = new PlayerStatsSync(_ledger, _store, _session, _progress, _settings, null, (seconds, token) =>
            {
                _delays.Add(seconds);
                return Task.CompletedTask;
            });
        }

        [TearDown]
        public void TearDown()
        {
            _sync.Dispose();
            Object.DestroyImmediate(_session);
            Object.DestroyImmediate(_progress);
            Object.DestroyImmediate(_settings);
        }

        [Test]
        public void SignIn_LoadsAndPublishesTheRecord()
        {
            _store.QueueLoad(StatsStoreResult.Loaded(StatsReadResult.Ok, new PlayerStatsRecord(1, Remote, "b0"), "lock-0"));
            _sync.Start();
            Assert.That(_store.LoadCount, Is.EqualTo(0), "Signed out: nothing to load.");
            Assert.That(_progress.Status, Is.EqualTo(PlayerProgressStatus.Unavailable));

            _session.SetLocalPlayerId(Owner);

            Assert.That(_store.LoadCount, Is.EqualTo(1));
            Assert.That(_ledger.OwnerId, Is.EqualTo(Owner));
            Assert.That(_ledger.WriteLock, Is.EqualTo("lock-0"));
            Assert.That(_progress.Status, Is.EqualTo(PlayerProgressStatus.Loaded));
            Assert.That(_progress.Stats, Is.EqualTo(Remote));
            Assert.That(_store.SaveCount, Is.EqualTo(0), "Nothing pending: no save.");
        }

        [Test]
        public void PendingFromPlayerPrefs_IsSavedRightAfterTheLoad()
        {
            _pending.Put(Owner, new PendingStats(WinDelta, "b1"));
            _store.QueueLoad(StatsStoreResult.Loaded(StatsReadResult.Ok, new PlayerStatsRecord(1, Remote, "b0"), "lock-0"));
            _session.SetLocalPlayerId(Owner);

            _sync.Start();

            Assert.That(_store.SaveCount, Is.EqualTo(1));
            Assert.That(_store.LastSaved.Stats, Is.EqualTo(PlayerStatsRules.Add(Remote, WinDelta)));
            Assert.That(_store.LastSaved.LastBatchId, Is.EqualTo("b1"));
            Assert.That(_store.LastSaveLock, Is.EqualTo("lock-0"));
            Assert.That(_ledger.HasPending, Is.False);
            Assert.That(_pending.Has(Owner), Is.False);
            Assert.That(_progress.SaveStatus, Is.EqualTo(PlayerProgressSaveStatus.Saved));
            Assert.That(_progress.Stats, Is.EqualTo(PlayerStatsRules.Add(Remote, WinDelta)));
        }

        [Test]
        public void EnqueuedRound_IsSavedWithTheNewLock()
        {
            _session.SetLocalPlayerId(Owner);
            _sync.Start();

            Assert.That(_ledger.TryEnqueue(WinDelta, "d1"), Is.True);

            Assert.That(_store.SaveCount, Is.EqualTo(1));
            Assert.That(_store.LastSaveLock, Is.Null, "First write of a missing key goes without a lock.");
            Assert.That(_ledger.WriteLock, Is.EqualTo("lock-1"));
            Assert.That(_ledger.Confirmed, Is.EqualTo(WinDelta));

            _ledger.TryEnqueue(WinDelta, "d2");
            Assert.That(_store.LastSaveLock, Is.EqualTo("lock-1"));
            Assert.That(_ledger.Confirmed, Is.EqualTo(PlayerStatsRules.Add(WinDelta, WinDelta)));
        }

        [Test]
        public void Conflict_WithTheBatchAlreadyApplied_CompletesWithoutSavingAgain()
        {
            _session.SetLocalPlayerId(Owner);
            _sync.Start();
            _store.QueueSave(StatsStoreResult.Conflict());
            _store.QueueLoad(StatsStoreResult.Loaded(StatsReadResult.Ok, new PlayerStatsRecord(1, WinDelta, "d1"), "lock-remote"));

            _ledger.TryEnqueue(WinDelta, "d1");

            Assert.That(_store.SaveCount, Is.EqualTo(1));
            Assert.That(_ledger.HasPending, Is.False);
            Assert.That(_ledger.WriteLock, Is.EqualTo("lock-remote"));
            Assert.That(_progress.SaveStatus, Is.EqualTo(PlayerProgressSaveStatus.Saved));
        }

        [Test]
        public void Conflict_FromAnotherDevice_RebasesAndSavesTheSum()
        {
            _session.SetLocalPlayerId(Owner);
            _sync.Start();
            _store.QueueSave(StatsStoreResult.Conflict());
            _store.QueueLoad(StatsStoreResult.Loaded(StatsReadResult.Ok, new PlayerStatsRecord(1, Remote, "other"), "lock-remote"));

            _ledger.TryEnqueue(WinDelta, "d1");

            Assert.That(_store.SaveCount, Is.EqualTo(2));
            Assert.That(_store.LastSaveLock, Is.EqualTo("lock-remote"));
            Assert.That(_store.LastSaved.Stats, Is.EqualTo(PlayerStatsRules.Add(Remote, WinDelta)));
            Assert.That(_ledger.HasPending, Is.False);
        }

        [Test]
        public void RetryableFailure_RetriesWithBackoffThenGivesUpUntilTheNextRound()
        {
            _session.SetLocalPlayerId(Owner);
            _sync.Start();
            _store.DefaultSave = StatsStoreResult.Retryable(PlayerProgressErrorKeys.Offline);

            _ledger.TryEnqueue(WinDelta, "d1");

            Assert.That(_store.SaveCount, Is.EqualTo(3));
            Assert.That(_delays, Is.EqualTo(new[] { 4f, 12f }));
            Assert.That(_progress.SaveStatus, Is.EqualTo(PlayerProgressSaveStatus.Failed));
            Assert.That(_progress.Stats, Is.EqualTo(WinDelta), "Optimistic totals include the pending round.");
            Assert.That(_pending.Get(Owner).Delta, Is.EqualTo(WinDelta));

            _store.DefaultSave = null;
            _ledger.TryEnqueue(WinDelta, "d2");

            Assert.That(_store.SaveCount, Is.EqualTo(5), "A new round budgets again: the sent head is resent as is, then the new round.");
            Assert.That(_ledger.HasPending, Is.False);
            Assert.That(_ledger.Confirmed, Is.EqualTo(PlayerStatsRules.Add(WinDelta, WinDelta)));
        }

        [Test]
        public void RateLimit_RetryAfterWinsWhenLonger()
        {
            _session.SetLocalPlayerId(Owner);
            _sync.Start();
            _store.QueueSave(StatsStoreResult.Retryable(PlayerProgressErrorKeys.RateLimited, 30f));

            _ledger.TryEnqueue(WinDelta, "d1");

            Assert.That(_delays, Is.EqualTo(new[] { 30f }));
            Assert.That(_store.SaveCount, Is.EqualTo(2));
            Assert.That(_progress.SaveStatus, Is.EqualTo(PlayerProgressSaveStatus.Saved));
        }

        [Test]
        public void PermanentLoadFailure_PublishesTheErrorAndNeverSaves()
        {
            _store.QueueLoad(StatsStoreResult.Failed(PlayerProgressErrorKeys.SignedOut));
            _session.SetLocalPlayerId(Owner);
            _sync.Start();

            Assert.That(_store.LoadCount, Is.EqualTo(1));
            Assert.That(_delays, Is.Empty);
            Assert.That(_progress.Status, Is.EqualTo(PlayerProgressStatus.Failed));
            Assert.That(_progress.LastErrorKey, Is.EqualTo(PlayerProgressErrorKeys.SignedOut));

            _store.QueueLoad(StatsStoreResult.Failed(PlayerProgressErrorKeys.SignedOut));
            _ledger.TryEnqueue(WinDelta, "d1");

            Assert.That(_store.SaveCount, Is.EqualTo(0), "Never saves before a successful load.");
        }

        [Test]
        public void NewerSchema_IsShownButNeverWritten()
        {
            _store.QueueLoad(StatsStoreResult.Loaded(StatsReadResult.NewerSchema, new PlayerStatsRecord(2, Remote, "x"), "lock"));
            _session.SetLocalPlayerId(Owner);
            _sync.Start();

            _ledger.TryEnqueue(WinDelta, "d1");

            Assert.That(_ledger.LoadState, Is.EqualTo(PlayerStatsLoadState.ReadOnly));
            Assert.That(_store.SaveCount, Is.EqualTo(0));
            Assert.That(_progress.HasStats, Is.True);
        }

        [Test]
        public void SignOut_MakesTheProgressUnavailable()
        {
            _session.SetLocalPlayerId(Owner);
            _sync.Start();
            Assert.That(_progress.Status, Is.EqualTo(PlayerProgressStatus.Loaded));

            _session.SetLocalPlayerId(null);

            Assert.That(_ledger.OwnerId, Is.Null);
            Assert.That(_progress.Status, Is.EqualTo(PlayerProgressStatus.Unavailable));
        }

        [Test]
        public void Dispose_DuringASave_LeavesNothingHalfDone()
        {
            _session.SetLocalPlayerId(Owner);
            _sync.Start();
            var never = new TaskCompletionSource<StatsStoreResult>();
            _store.QueuePendingSave(never);

            _ledger.TryEnqueue(WinDelta, "d1");
            Assert.That(_ledger.IsSaving, Is.True);

            _sync.Dispose();

            Assert.That(_ledger.IsSaving, Is.False);
            Assert.That(_ledger.LoadState, Is.EqualTo(PlayerStatsLoadState.NotLoaded), "The next service reloads.");
            Assert.That(_ledger.Pending.Delta, Is.EqualTo(WinDelta), "The pending round comes back from PlayerPrefs.");
        }

        [Test]
        public void InSession_ReadsTheRivalsPublicStats()
        {
            var rivalStats = new PlayerStatsSummary(9, 4, 2, 3, 30, 0);
            _store.QueuePublicLoad(StatsStoreResult.Loaded(StatsReadResult.Ok, new PlayerStatsRecord(1, rivalStats, null), "l"));
            _session.SetLocalPlayerId(Owner);
            _sync.Start();

            EnterSession();

            Assert.That(_store.PublicLoadCount, Is.EqualTo(1));
            Assert.That(_store.LastPublicId, Is.EqualTo(Rival));
            Assert.That(_progress.HasRivalStats, Is.True);
            Assert.That(_progress.RivalStats, Is.EqualTo(rivalStats));

            _session.TrySetStatus(OnlineSessionStatus.Ready);
            Assert.That(_progress.RivalStatus, Is.EqualTo(PlayerProgressStatus.Unavailable));
        }

        [Test]
        public void RivalStatsOff_ReadsNoRival()
        {
            _session.SetLocalPlayerId(Owner);
            _sync.Start();
            SetBool(_settings, "_readRivalStats", false);
            EnterSession();
            Assert.That(_store.PublicLoadCount, Is.EqualTo(0));
            Assert.That(_progress.HasRivalStats, Is.False);
        }

        [Test]
        public void ConflictThenFailedReload_CountsOneAttempt()
        {
            _session.SetLocalPlayerId(Owner);
            _sync.Start();
            _store.QueueSave(StatsStoreResult.Conflict());
            _store.QueueLoad(StatsStoreResult.Retryable(PlayerProgressErrorKeys.Offline));

            _ledger.TryEnqueue(WinDelta, "d1");

            Assert.That(_delays, Is.EqualTo(new[] { 4f }), "first retry delay: one attempt used");
            Assert.That(_store.SaveCount, Is.EqualTo(2));
            Assert.That(_progress.SaveStatus, Is.EqualTo(PlayerProgressSaveStatus.Saved));
        }

        [Test]
        public void ReadOnly_PublishesTheErrorAndAFailedSaveForUnsavableRounds()
        {
            _store.QueueLoad(StatsStoreResult.Loaded(StatsReadResult.Corrupt, PlayerStatsRecord.Empty, "lock"));
            _session.SetLocalPlayerId(Owner);
            _sync.Start();

            Assert.That(_progress.HasStats, Is.True);
            Assert.That(_progress.LastErrorKey, Is.EqualTo(PlayerProgressErrorKeys.Corrupt));
            Assert.That(_progress.SaveStatus, Is.EqualTo(PlayerProgressSaveStatus.Idle));

            _ledger.TryEnqueue(WinDelta, "d1");

            Assert.That(_progress.SaveStatus, Is.EqualTo(PlayerProgressSaveStatus.Failed));
            Assert.That(_progress.Stats, Is.EqualTo(default(PlayerStatsSummary)), "an unsavable round is not shown");
        }

        [Test]
        public void HostLeftWinNotice_IsPublishedAndConsumedOnRequest()
        {
            _session.SetLocalPlayerId(Owner);
            _sync.Start();

            _ledger.RaiseHostLeftWinNotice();
            Assert.That(_progress.HasHostLeftWinNotice, Is.True);

            Assert.That(_progress.RequestConsumeHostLeftWinNotice(), Is.True);
            Assert.That(_ledger.HasHostLeftWinNotice, Is.False);
            Assert.That(_progress.HasHostLeftWinNotice, Is.False);
        }

        private void EnterSession()
        {
            if (_session.Status == OnlineSessionStatus.SignedOut)
            {
                _session.TrySetStatus(OnlineSessionStatus.SigningIn);
                _session.TrySetStatus(OnlineSessionStatus.Ready);
            }

            _session.TrySetStatus(OnlineSessionStatus.Joining);
            _session.TrySetStatus(OnlineSessionStatus.InSession);
            var participants = new[]
            {
                new OnlineParticipant(Rival, "Rival", true, false),
                new OnlineParticipant(Owner, "Me", false, true)
            };
            _session.SetParticipants(participants, participants.Length);
        }

        private static void SetBool(Object target, string field, bool value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
