using AlienFarms.Core;
using AlienFarms.Networking.Persistence;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests.Persistence
{
    public sealed class PlayerStatsLedgerTests
    {
        private const string Owner = "ugs-owner";

        private static readonly PlayerStatsSummary WinDelta = new PlayerStatsSummary(1, 1, 0, 0, 4, 0);
        private static readonly PlayerStatsSummary LossDelta = new PlayerStatsSummary(1, 0, 0, 1, 2, 0);

        private FakePendingStatsStore _store;
        private PlayerStatsLedger _ledger;
        private int _changed;

        [SetUp]
        public void SetUp()
        {
            _store = new FakePendingStatsStore();
            _ledger = new PlayerStatsLedger(_store);
            _ledger.Changed += () => _changed++;
            _changed = 0;
        }

        [Test]
        public void NewLedger_IsNotLoadedWithoutOwner()
        {
            Assert.That(_ledger.OwnerId, Is.Null);
            Assert.That(_ledger.LoadState, Is.EqualTo(PlayerStatsLoadState.NotLoaded));
            Assert.That(_ledger.SaveState, Is.EqualTo(PlayerStatsSaveState.Idle));
            Assert.That(_ledger.HasPending, Is.False);
            Assert.That(_ledger.BeginLoad(), Is.False);
        }

        [Test]
        public void Reset_ReadsPendingFromStoreAndBumpsEpoch()
        {
            _store.Put(Owner, new PendingStats(WinDelta, "b1"));
            int epoch = _ledger.Epoch;

            _ledger.Reset(Owner);

            Assert.That(_ledger.Epoch, Is.EqualTo(epoch + 1));
            Assert.That(_ledger.HasPending, Is.True);
            Assert.That(_ledger.Pending.BatchId, Is.EqualTo("b1"));
            Assert.That(_ledger.Displayed, Is.EqualTo(WinDelta));
        }

        [Test]
        public void ApplyLoaded_Missing_LoadsEmptyWithoutLock()
        {
            _ledger.Reset(Owner);
            _ledger.BeginLoad();

            Assert.That(_ledger.ApplyLoaded(StatsReadResult.Missing, default, "ignored"), Is.True);

            Assert.That(_ledger.LoadState, Is.EqualTo(PlayerStatsLoadState.Loaded));
            Assert.That(_ledger.Confirmed, Is.EqualTo(default(PlayerStatsSummary)));
            Assert.That(_ledger.WriteLock, Is.Null);
        }

        [Test]
        public void ApplyLoaded_WhenNotLoading_IsIgnored()
        {
            _ledger.Reset(Owner);

            Assert.That(_ledger.ApplyLoaded(StatsReadResult.Ok, Record(WinDelta, null), "l1"), Is.False);
            Assert.That(_ledger.LoadState, Is.EqualTo(PlayerStatsLoadState.NotLoaded));
        }

        [Test]
        public void ApplyLoaded_RemoteCarriesPendingBatch_DropsPendingWithoutDoubleCounting()
        {
            // The game closed after the cloud applied batch b1 but before the response arrived.
            _store.Put(Owner, new PendingStats(WinDelta, "b1"));
            _ledger.Reset(Owner);
            _ledger.BeginLoad();

            _ledger.ApplyLoaded(StatsReadResult.Ok, Record(WinDelta, "b1"), "l1");

            Assert.That(_ledger.HasPending, Is.False);
            Assert.That(_ledger.Displayed, Is.EqualTo(WinDelta));
            Assert.That(_store.Has(Owner), Is.False);
        }

        [Test]
        public void ApplyLoaded_RemoteWithOtherBatch_KeepsPendingOnTop()
        {
            _store.Put(Owner, new PendingStats(LossDelta, "b2"));
            _ledger.Reset(Owner);
            _ledger.BeginLoad();

            _ledger.ApplyLoaded(StatsReadResult.Ok, Record(WinDelta, "b1"), "l1");

            Assert.That(_ledger.HasPending, Is.True);
            Assert.That(_ledger.Displayed, Is.EqualTo(new PlayerStatsSummary(2, 1, 0, 1, 6, 0)));
        }

        [Test]
        public void ApplyLoaded_NewerSchema_IsReadOnlyAndNeverSaves()
        {
            _ledger.Reset(Owner);
            _ledger.BeginLoad();
            _ledger.ApplyLoaded(StatsReadResult.NewerSchema, new PlayerStatsRecord(2, WinDelta, null), null);
            _ledger.TryEnqueue(LossDelta, "d1");

            Assert.That(_ledger.LoadState, Is.EqualTo(PlayerStatsLoadState.ReadOnly));
            Assert.That(_ledger.LastErrorKey, Is.EqualTo(PlayerProgressErrorKeys.NewerSchema));
            Assert.That(_ledger.Confirmed, Is.EqualTo(WinDelta));
            Assert.That(_ledger.TryBeginSave(out _, out _), Is.False);
        }

        [Test]
        public void ApplyLoaded_Corrupt_IsReadOnly()
        {
            _ledger.Reset(Owner);
            _ledger.BeginLoad();

            _ledger.ApplyLoaded(StatsReadResult.Corrupt, default, null);

            Assert.That(_ledger.LoadState, Is.EqualTo(PlayerStatsLoadState.ReadOnly));
            Assert.That(_ledger.LastErrorKey, Is.EqualTo(PlayerProgressErrorKeys.Corrupt));
        }

        [Test]
        public void FailLoad_RecordsErrorAndAllowsRetry()
        {
            _ledger.Reset(Owner);
            _ledger.BeginLoad();

            _ledger.FailLoad(PlayerProgressErrorKeys.Offline);

            Assert.That(_ledger.LoadState, Is.EqualTo(PlayerStatsLoadState.Failed));
            Assert.That(_ledger.LastErrorKey, Is.EqualTo(PlayerProgressErrorKeys.Offline));
            Assert.That(_ledger.BeginLoad(), Is.True);
        }

        [Test]
        public void TryEnqueue_PersistsEveryTimeAndKeepsTheFirstBatchId()
        {
            _ledger.Reset(Owner);

            Assert.That(_ledger.TryEnqueue(WinDelta, "d1"), Is.True);
            Assert.That(_ledger.TryEnqueue(LossDelta, "d2"), Is.True);

            Assert.That(_store.SaveCount, Is.EqualTo(2));
            PendingStats stored = _store.Get(Owner);
            Assert.That(stored.BatchId, Is.EqualTo("d1"));
            Assert.That(stored.Delta, Is.EqualTo(new PlayerStatsSummary(2, 1, 0, 1, 6, 0)));
        }

        [Test]
        public void TryEnqueue_RejectsDuplicateIdEmptyDeltaAndNoOwner()
        {
            Assert.That(_ledger.TryEnqueue(WinDelta, "d1"), Is.False, "no owner");

            _ledger.Reset(Owner);
            Assert.That(_ledger.TryEnqueue(default, "d1"), Is.False, "empty delta");
            Assert.That(_ledger.TryEnqueue(WinDelta, null), Is.False, "no id");
            Assert.That(_ledger.TryEnqueue(WinDelta, "d1"), Is.True);
            Assert.That(_ledger.TryEnqueue(WinDelta, "d1"), Is.False, "same id twice");
            Assert.That(_ledger.Pending.Delta, Is.EqualTo(WinDelta));
        }

        [Test]
        public void TryBeginSave_BeforeLoad_IsRefused()
        {
            _ledger.Reset(Owner);
            _ledger.TryEnqueue(WinDelta, "d1");

            Assert.That(_ledger.TryBeginSave(out _, out _), Is.False);
        }

        [Test]
        public void TryBeginSave_WritesAbsoluteTotalsWithPendingBatchAndLock()
        {
            LoadWith(Record(WinDelta, "b0"), "lock-1");
            _ledger.TryEnqueue(LossDelta, "d1");

            Assert.That(_ledger.TryBeginSave(out PlayerStatsRecord toWrite, out string writeLock), Is.True);

            Assert.That(toWrite.Stats, Is.EqualTo(new PlayerStatsSummary(2, 1, 0, 1, 6, 0)));
            Assert.That(toWrite.LastBatchId, Is.EqualTo("d1"));
            Assert.That(toWrite.Version, Is.EqualTo(PlayerStatsCodec.CurrentVersion));
            Assert.That(writeLock, Is.EqualTo("lock-1"));
            Assert.That(_ledger.SaveState, Is.EqualTo(PlayerStatsSaveState.Saving));
            Assert.That(_ledger.TryBeginSave(out _, out _), Is.False, "one save at a time");
        }

        [Test]
        public void TryBeginSave_FirstWrite_UsesNullLock()
        {
            LoadWith(default, null, StatsReadResult.Missing);
            _ledger.TryEnqueue(WinDelta, "d1");

            _ledger.TryBeginSave(out _, out string writeLock);

            Assert.That(writeLock, Is.Null);
        }

        [Test]
        public void CompleteSave_ConfirmsClearsPendingAndStore()
        {
            LoadWith(default, null, StatsReadResult.Missing);
            _ledger.TryEnqueue(WinDelta, "d1");
            _ledger.TryBeginSave(out _, out _);

            _ledger.CompleteSave("lock-2");

            Assert.That(_ledger.SaveState, Is.EqualTo(PlayerStatsSaveState.Saved));
            Assert.That(_ledger.Confirmed, Is.EqualTo(WinDelta));
            Assert.That(_ledger.WriteLock, Is.EqualTo("lock-2"));
            Assert.That(_ledger.HasPending, Is.False);
            Assert.That(_store.Has(Owner), Is.False);
            Assert.That(_store.ClearCount, Is.EqualTo(1));
        }

        [Test]
        public void CompleteSave_KeepsWhatWasEnqueuedDuringTheFlightWithANewBatch()
        {
            LoadWith(default, null, StatsReadResult.Missing);
            _ledger.TryEnqueue(WinDelta, "d1");
            _ledger.TryBeginSave(out _, out _);
            _ledger.TryEnqueue(LossDelta, "d2");

            _ledger.CompleteSave("lock-2");

            Assert.That(_ledger.Confirmed, Is.EqualTo(WinDelta));
            Assert.That(_ledger.Pending.Delta, Is.EqualTo(LossDelta));
            Assert.That(_ledger.Pending.BatchId, Is.EqualTo("d2"));
            Assert.That(_store.Get(Owner).BatchId, Is.EqualTo("d2"));
            Assert.That(_ledger.CanSave, Is.True);
        }

        [Test]
        public void FailSave_KeepsPendingAndCountsAttempts()
        {
            LoadWith(default, null, StatsReadResult.Missing);
            _ledger.TryEnqueue(WinDelta, "d1");
            _ledger.TryBeginSave(out _, out _);

            _ledger.FailSave(PlayerProgressErrorKeys.Offline);

            Assert.That(_ledger.SaveState, Is.EqualTo(PlayerStatsSaveState.Failed));
            Assert.That(_ledger.FailedAttempts, Is.EqualTo(1));
            Assert.That(_ledger.LastErrorKey, Is.EqualTo(PlayerProgressErrorKeys.Offline));
            Assert.That(_ledger.Displayed, Is.EqualTo(WinDelta));
            Assert.That(_store.Get(Owner).BatchId, Is.EqualTo("d1"));
            Assert.That(_ledger.TryBeginSave(out PlayerStatsRecord retry, out _), Is.True);
            Assert.That(retry.LastBatchId, Is.EqualTo("d1"), "the batch id is stable across retries");
        }

        [Test]
        public void ResolveConflict_RemoteHasInFlightBatch_IsAlreadyApplied()
        {
            LoadWith(Record(LossDelta, "b0"), "lock-1");
            _ledger.TryEnqueue(WinDelta, "d1");
            _ledger.TryBeginSave(out PlayerStatsRecord written, out _);

            PlayerStatsConflictOutcome outcome = _ledger.ResolveConflict(StatsReadResult.Ok, written, "lock-2");

            Assert.That(outcome, Is.EqualTo(PlayerStatsConflictOutcome.AlreadyApplied));
            Assert.That(_ledger.Confirmed, Is.EqualTo(written.Stats));
            Assert.That(_ledger.WriteLock, Is.EqualTo("lock-2"));
            Assert.That(_ledger.HasPending, Is.False);
            Assert.That(_ledger.SaveState, Is.EqualTo(PlayerStatsSaveState.Saved));
        }

        [Test]
        public void ResolveConflict_OtherDeviceWrote_RebasesAndSavesAgain()
        {
            LoadWith(Record(LossDelta, "b0"), "lock-1");
            _ledger.TryEnqueue(WinDelta, "d1");
            _ledger.TryBeginSave(out _, out _);
            var other = new PlayerStatsSummary(5, 2, 1, 2, 20, 0);

            PlayerStatsConflictOutcome outcome = _ledger.ResolveConflict(StatsReadResult.Ok, Record(other, "x9"), "lock-9");

            Assert.That(outcome, Is.EqualTo(PlayerStatsConflictOutcome.Rebased));
            Assert.That(_ledger.TryBeginSave(out PlayerStatsRecord retry, out string writeLock), Is.True);
            Assert.That(retry.Stats, Is.EqualTo(PlayerStatsRules.Add(other, WinDelta)));
            Assert.That(retry.LastBatchId, Is.EqualTo("d1"));
            Assert.That(writeLock, Is.EqualTo("lock-9"));
        }

        [Test]
        public void ResolveConflict_NewerRemote_BecomesReadOnly()
        {
            LoadWith(default, null, StatsReadResult.Missing);
            _ledger.TryEnqueue(WinDelta, "d1");
            _ledger.TryBeginSave(out _, out _);

            PlayerStatsConflictOutcome outcome = _ledger.ResolveConflict(StatsReadResult.NewerSchema, new PlayerStatsRecord(2, LossDelta, "z"), "l");

            Assert.That(outcome, Is.EqualTo(PlayerStatsConflictOutcome.ReadOnly));
            Assert.That(_ledger.LoadState, Is.EqualTo(PlayerStatsLoadState.ReadOnly));
            Assert.That(_ledger.TryBeginSave(out _, out _), Is.False);
        }

        [Test]
        public void ResolveConflict_WithoutSave_IsIgnored()
        {
            LoadWith(default, null, StatsReadResult.Missing);

            Assert.That(_ledger.ResolveConflict(StatsReadResult.Ok, Record(WinDelta, "x"), "l"), Is.EqualTo(PlayerStatsConflictOutcome.Ignored));
        }

        [Test]
        public void MarkRoundStarted_ResetsFinishedSaveStatusOnly()
        {
            LoadWith(default, null, StatsReadResult.Missing);
            _ledger.TryEnqueue(WinDelta, "d1");
            _ledger.TryBeginSave(out _, out _);

            _ledger.MarkRoundStarted();
            Assert.That(_ledger.SaveState, Is.EqualTo(PlayerStatsSaveState.Saving));

            _ledger.CompleteSave("l");
            _ledger.MarkRoundStarted();
            Assert.That(_ledger.SaveState, Is.EqualTo(PlayerStatsSaveState.Idle));
        }

        [Test]
        public void Reset_ToAnotherOwner_ForgetsEverything()
        {
            LoadWith(Record(WinDelta, "b0"), "lock-1");
            _ledger.TryEnqueue(LossDelta, "d1");

            _ledger.Reset("other");

            Assert.That(_ledger.OwnerId, Is.EqualTo("other"));
            Assert.That(_ledger.LoadState, Is.EqualTo(PlayerStatsLoadState.NotLoaded));
            Assert.That(_ledger.HasPending, Is.False);
            Assert.That(_ledger.Confirmed, Is.EqualTo(default(PlayerStatsSummary)));
            Assert.That(_store.Has(Owner), Is.True, "the previous owner's pending stays stored");
        }

        [Test]
        public void Changes_RaiseChanged()
        {
            _ledger.Reset(Owner);
            _ledger.BeginLoad();
            _ledger.ApplyLoaded(StatsReadResult.Missing, default, null);
            _ledger.TryEnqueue(WinDelta, "d1");

            Assert.That(_changed, Is.EqualTo(4));
        }

        [Test]
        public void LostResponse_ThenNewRound_ResendsTheSameHeadAndKeepsTheRound()
        {
            // a) The head landed but the response was lost; a round enqueued afterwards must survive the conflict.
            LoadWith(default, null, StatsReadResult.Missing);
            _ledger.TryEnqueue(WinDelta, "d1");
            _ledger.TryBeginSave(out PlayerStatsRecord first, out _);
            _ledger.FailSave(PlayerProgressErrorKeys.Offline);
            _ledger.TryEnqueue(LossDelta, "d2");

            Assert.That(_ledger.TryBeginSave(out PlayerStatsRecord retry, out _), Is.True);
            Assert.That(retry, Is.EqualTo(first), "a sent head is resent unchanged");

            PlayerStatsConflictOutcome outcome = _ledger.ResolveConflict(StatsReadResult.Ok, first, "lock-2");

            Assert.That(outcome, Is.EqualTo(PlayerStatsConflictOutcome.AlreadyApplied));
            Assert.That(_ledger.Confirmed, Is.EqualTo(WinDelta));
            Assert.That(_ledger.Pending.Delta, Is.EqualTo(LossDelta));
            Assert.That(_ledger.Pending.BatchId, Is.EqualTo("d2"));
            Assert.That(_ledger.Pending.HeadSent, Is.False);
            Assert.That(_ledger.TryBeginSave(out PlayerStatsRecord next, out string nextLock), Is.True);
            Assert.That(next.Stats, Is.EqualTo(PlayerStatsRules.Add(WinDelta, LossDelta)));
            Assert.That(next.LastBatchId, Is.EqualTo("d2"));
            Assert.That(nextLock, Is.EqualTo("lock-2"));
        }

        [Test]
        public void LostResponse_NewRound_GameClosed_ReloadKeepsTheRound()
        {
            // b) Like a), but the game closes before retrying: the reload finds the head applied.
            LoadWith(default, null, StatsReadResult.Missing);
            _ledger.TryEnqueue(WinDelta, "d1");
            _ledger.TryBeginSave(out PlayerStatsRecord first, out _);
            _ledger.FailSave(PlayerProgressErrorKeys.Offline);
            _ledger.TryEnqueue(LossDelta, "d2");

            var reopened = new PlayerStatsLedger(_store);
            reopened.Reset(Owner);
            reopened.BeginLoad();
            reopened.ApplyLoaded(StatsReadResult.Ok, first, "lock-2");

            Assert.That(reopened.Confirmed, Is.EqualTo(WinDelta));
            Assert.That(reopened.Pending.Delta, Is.EqualTo(LossDelta));
            Assert.That(reopened.Pending.BatchId, Is.EqualTo("d2"));
            Assert.That(reopened.Displayed, Is.EqualTo(PlayerStatsRules.Add(WinDelta, LossDelta)));
            Assert.That(_store.Get(Owner).BatchId, Is.EqualTo("d2"));
        }

        [Test]
        public void RoundDuringFlight_ThenClosed_ReloadKeepsTheRoundWhetherOrNotTheHeadLanded()
        {
            // c) A round enqueued while the head is in flight, then the game closes (or the service is torn down).
            LoadWith(default, null, StatsReadResult.Missing);
            _ledger.TryEnqueue(WinDelta, "d1");
            _ledger.TryBeginSave(out PlayerStatsRecord inFlight, out _);
            _ledger.TryEnqueue(LossDelta, "d2");

            PendingStats stored = _store.Get(Owner);
            Assert.That(stored.HeadSent, Is.True);
            Assert.That(stored.HeadDelta, Is.EqualTo(WinDelta));
            Assert.That(stored.TailDelta, Is.EqualTo(LossDelta));

            var other = new FakePendingStatsStore();
            other.Put(Owner, stored);

            // The head landed.
            var landed = new PlayerStatsLedger(_store);
            landed.Reset(Owner);
            landed.BeginLoad();
            landed.ApplyLoaded(StatsReadResult.Ok, inFlight, "lock-2");
            Assert.That(landed.Pending.Delta, Is.EqualTo(LossDelta));
            Assert.That(landed.Displayed, Is.EqualTo(PlayerStatsRules.Add(WinDelta, LossDelta)));

            // The head did not land.
            var lost = new PlayerStatsLedger(other);
            lost.Reset(Owner);
            lost.BeginLoad();
            lost.ApplyLoaded(StatsReadResult.Missing, default, null);
            Assert.That(lost.Pending.HeadSent, Is.False);
            Assert.That(lost.Pending.HasTail, Is.False);
            Assert.That(lost.TryBeginSave(out PlayerStatsRecord resend, out _), Is.True);
            Assert.That(resend.Stats, Is.EqualTo(PlayerStatsRules.Add(WinDelta, LossDelta)));
            Assert.That(resend.LastBatchId, Is.EqualTo("d1"));
        }

        [Test]
        public void ResolveConflict_RemoteCarriesTheTailBatch_DropsBoth()
        {
            LoadWith(default, null, StatsReadResult.Missing);
            _store.Put(Owner, new PendingStats(WinDelta, "d1", true, LossDelta, "d2"));
            _ledger.Reset(Owner);
            _ledger.BeginLoad();
            var both = PlayerStatsRules.Add(WinDelta, LossDelta);

            _ledger.ApplyLoaded(StatsReadResult.Ok, Record(both, "d2"), "l");

            Assert.That(_ledger.HasPending, Is.False);
            Assert.That(_ledger.Displayed, Is.EqualTo(both));
        }

        [Test]
        public void ReadOnly_DisplaysConfirmedOnly()
        {
            _ledger.Reset(Owner);
            _ledger.BeginLoad();
            _ledger.ApplyLoaded(StatsReadResult.NewerSchema, new PlayerStatsRecord(2, WinDelta, null), null);
            _ledger.TryEnqueue(LossDelta, "d1");

            Assert.That(_ledger.HasPending, Is.True);
            Assert.That(_ledger.Displayed, Is.EqualTo(WinDelta));
        }

        [Test]
        public void HostLeftWinNotice_IsConsumedOnceAndSurvivesReset()
        {
            _ledger.RaiseHostLeftWinNotice();
            _ledger.Reset(Owner);

            Assert.That(_ledger.HasHostLeftWinNotice, Is.True);
            Assert.That(_ledger.ConsumeHostLeftWinNotice(), Is.True);
            Assert.That(_ledger.ConsumeHostLeftWinNotice(), Is.False);
        }

        private void LoadWith(PlayerStatsRecord record, string writeLock, StatsReadResult read = StatsReadResult.Ok)
        {
            _ledger.Reset(Owner);
            _ledger.BeginLoad();
            _ledger.ApplyLoaded(read, record, writeLock);
            Assert.That(_ledger.LoadState, Is.EqualTo(PlayerStatsLoadState.Loaded));
        }

        private static PlayerStatsRecord Record(PlayerStatsSummary stats, string batch) =>
            new PlayerStatsRecord(PlayerStatsCodec.CurrentVersion, stats, batch);
    }
}
