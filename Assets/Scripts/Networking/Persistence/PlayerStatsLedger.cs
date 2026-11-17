using System;
using AlienFarms.Core;

namespace AlienFarms.Networking.Persistence
{
    /// <summary>
    /// Synchronization model of the local player's cloud stats (plain C#, no I/O, no collections). Holds the owner, the
    /// confirmed record and its write lock, the aggregated pending delta (persisted through <see cref="IPendingStatsStore"/>)
    /// and the batch in flight. The cloud save service drives it: load, enqueue rounds, save totals, resolve conflicts.
    /// <list type="bullet">
    /// <item>Totals are absolute (confirmed + pending), so retrying the same batch gives the same record.</item>
    /// <item>Never saves before a successful load (summing the pending onto zero would overwrite the cloud).</item>
    /// <item>The pending stats have two segments (<see cref="PendingStats"/>): the head is what a write carries and is
    /// frozen once sent (a retry resends exactly it, so one batch id never stands for two contents); rounds enqueued
    /// meanwhile wait in the tail. A loaded or reloaded record that carries the head's batch means the head landed
    /// (lost response, or the game closed first): the head is dropped and the tail moves up, nothing counted twice or
    /// lost. Any other batch means it did not land: the head is unsent again and takes the tail.</item>
    /// <item>A newer or corrupt cloud record makes it <see cref="PlayerStatsLoadState.ReadOnly"/>: shown, never written.</item>
    /// </list>
    /// </summary>
    public sealed class PlayerStatsLedger
    {
        private readonly IPendingStatsStore _store;

        private PlayerStatsSummary _confirmed;
        private string _writeLock;
        private PendingStats _pending;
        private string _lastDeltaId;
        private bool _saving;
        private bool _hostLeftWinNotice;

        /// <param name="store">Local persistence of the pending stats; null keeps them in memory only.</param>
        public PlayerStatsLedger(IPendingStatsStore store)
        {
            _store = store;
        }

        /// <summary>Raised after any state change.</summary>
        public event Action Changed;

        /// <summary>UGS player id the stats belong to; null before <see cref="Reset"/>.</summary>
        public string OwnerId { get; private set; }

        /// <summary>Incremented by every <see cref="Reset"/>: async callers compare it to drop stale results.</summary>
        public int Epoch { get; private set; }

        public PlayerStatsLoadState LoadState { get; private set; }

        public PlayerStatsSaveState SaveState { get; private set; }

        /// <summary>Localization key (<see cref="PlayerProgressErrorKeys"/>) of the last load or save failure; null when none.</summary>
        public string LastErrorKey { get; private set; }

        /// <summary>Consecutive failed saves (reset by a successful one).</summary>
        public int FailedAttempts { get; private set; }

        /// <summary>Confirmed totals (last record loaded or saved).</summary>
        public PlayerStatsSummary Confirmed => _confirmed;

        /// <summary>Write lock of the confirmed record; null when the key does not exist yet.</summary>
        public string WriteLock => _writeLock;

        /// <summary>Unconfirmed stats (persisted locally).</summary>
        public PendingStats Pending => _pending;

        public bool HasPending => !_pending.IsEmpty;

        /// <summary>True while a save is in flight (between <see cref="TryBeginSave"/> and its outcome).</summary>
        public bool IsSaving => _saving;

        /// <summary>
        /// Totals shown to the player: confirmed plus pending (optimistic). Read only: confirmed only, since the pending
        /// stats will never be written by this version.
        /// </summary>
        public PlayerStatsSummary Displayed => LoadState == PlayerStatsLoadState.ReadOnly
            ? _confirmed
            : PlayerStatsRules.Add(_confirmed, _pending.Delta);

        /// <summary>
        /// A win was recorded because the host left during Playing: the online menu shows it once (see
        /// <see cref="ConsumeHostLeftWinNotice"/>). Survives <see cref="Reset"/> (a UI notice, not account data).
        /// </summary>
        public bool HasHostLeftWinNotice => _hostLeftWinNotice;

        /// <summary>True when the ledger may write now: loaded, something pending and no save in flight.</summary>
        public bool CanSave => LoadState == PlayerStatsLoadState.Loaded && HasPending && !_saving;

        /// <summary>
        /// Starts over for <paramref name="ownerId"/> (null / empty = signed out): forgets everything and reads that
        /// account's pending stats from the store.
        /// </summary>
        public void Reset(string ownerId)
        {
            Epoch++;
            OwnerId = string.IsNullOrEmpty(ownerId) ? null : ownerId;
            LoadState = PlayerStatsLoadState.NotLoaded;
            SaveState = PlayerStatsSaveState.Idle;
            LastErrorKey = null;
            FailedAttempts = 0;
            _confirmed = default;
            _writeLock = null;
            _lastDeltaId = null;
            _saving = false;
            _pending = OwnerId != null && _store != null ? _store.Load(OwnerId) : default;
            RaiseChanged();
        }

        /// <summary>Marks the load in flight. False without an owner or while a save is in flight.</summary>
        public bool BeginLoad()
        {
            if (OwnerId == null || _saving)
            {
                return false;
            }

            LoadState = PlayerStatsLoadState.Loading;
            LastErrorKey = null;
            RaiseChanged();
            return true;
        }

        /// <summary>
        /// Applies a loaded record (<paramref name="read"/> from <see cref="PlayerStatsCodec.Read"/>). Ok / Missing: loaded,
        /// and a pending batch the record already carries is dropped. NewerSchema / Corrupt: read only. False (ignored)
        /// unless loading.
        /// </summary>
        public bool ApplyLoaded(StatsReadResult read, in PlayerStatsRecord remote, string writeLock)
        {
            if (LoadState != PlayerStatsLoadState.Loading)
            {
                return false;
            }

            switch (read)
            {
                case StatsReadResult.Ok:
                case StatsReadResult.Missing:
                    PlayerStatsRecord record = read == StatsReadResult.Ok ? remote : PlayerStatsRecord.Empty;
                    _confirmed = record.Stats;
                    _writeLock = read == StatsReadResult.Ok && !string.IsNullOrEmpty(writeLock) ? writeLock : null;
                    LoadState = PlayerStatsLoadState.Loaded;
                    LastErrorKey = null;
                    ResolvePendingAgainst(read == StatsReadResult.Ok ? record.LastBatchId : null);
                    break;
                case StatsReadResult.NewerSchema:
                    _confirmed = remote.Stats;
                    _writeLock = null;
                    LoadState = PlayerStatsLoadState.ReadOnly;
                    LastErrorKey = PlayerProgressErrorKeys.NewerSchema;
                    break;
                default:
                    _confirmed = default;
                    _writeLock = null;
                    LoadState = PlayerStatsLoadState.ReadOnly;
                    LastErrorKey = PlayerProgressErrorKeys.Corrupt;
                    break;
            }

            RaiseChanged();
            return true;
        }

        /// <summary>The load failed (retryable later with <see cref="BeginLoad"/>).</summary>
        public void FailLoad(string errorKey)
        {
            if (LoadState != PlayerStatsLoadState.Loading)
            {
                return;
            }

            LoadState = PlayerStatsLoadState.Failed;
            LastErrorKey = string.IsNullOrEmpty(errorKey) ? PlayerProgressErrorKeys.Unknown : errorKey;
            RaiseChanged();
        }

        /// <summary>Records that the host left during Playing and the win was enqueued (the menu notice).</summary>
        public void RaiseHostLeftWinNotice()
        {
            if (_hostLeftWinNotice)
            {
                return;
            }

            _hostLeftWinNotice = true;
            RaiseChanged();
        }

        /// <summary>True (once) when the host-left win notice was pending; clears it.</summary>
        public bool ConsumeHostLeftWinNotice()
        {
            if (!_hostLeftWinNotice)
            {
                return false;
            }

            _hostLeftWinNotice = false;
            RaiseChanged();
            return true;
        }

        /// <summary>
        /// Adds one round (or forfeit) to the pending stats and persists them right away (to the head while unsent,
        /// otherwise to the tail). Works in any load state (it is
        /// saved once loaded). False without an owner, for an empty delta, an empty id or the same id twice in a row.
        /// </summary>
        public bool TryEnqueue(in PlayerStatsSummary delta, string deltaId)
        {
            if (OwnerId == null || string.IsNullOrEmpty(deltaId) || PlayerStatsRules.IsZero(delta)
                || string.Equals(deltaId, _lastDeltaId, StringComparison.Ordinal))
            {
                return false;
            }

            _lastDeltaId = deltaId;
            _pending = _pending.Enqueue(PlayerStatsRules.Sanitize(delta), deltaId);
            _store?.Save(OwnerId, _pending);
            RaiseChanged();
            return true;
        }

        /// <summary>
        /// Starts saving the head: <paramref name="toWrite"/> is the absolute total (confirmed + head) tagged with the
        /// head's batch, <paramref name="writeLock"/> the lock to write with (null = first write). The head is frozen
        /// (and persisted as sent) before it leaves; the tail waits for the next write. False unless <see cref="CanSave"/>.
        /// </summary>
        public bool TryBeginSave(out PlayerStatsRecord toWrite, out string writeLock)
        {
            if (!CanSave)
            {
                toWrite = default;
                writeLock = null;
                return false;
            }

            _saving = true;
            if (!_pending.HeadSent)
            {
                _pending = _pending.MarkHeadSent();
                _store?.Save(OwnerId, _pending);
            }

            PlayerStatsSummary total = PlayerStatsRules.Add(_confirmed, _pending.HeadDelta);
            toWrite = new PlayerStatsRecord(PlayerStatsCodec.CurrentVersion, total, _pending.HeadBatchId);
            writeLock = _writeLock;
            SaveState = PlayerStatsSaveState.Saving;
            RaiseChanged();
            return true;
        }

        /// <summary>
        /// The cloud confirmed the head: it becomes confirmed with <paramref name="newWriteLock"/> and the tail (what was
        /// enqueued after it was sent) becomes the new, unsent head. The local pending entry follows.
        /// </summary>
        public void CompleteSave(string newWriteLock)
        {
            if (!_saving)
            {
                return;
            }

            _confirmed = PlayerStatsRules.Add(_confirmed, _pending.HeadDelta);
            _writeLock = string.IsNullOrEmpty(newWriteLock) ? null : newWriteLock;
            SetPending(_pending.DropHead());
            _saving = false;
            SaveState = PlayerStatsSaveState.Saved;
            LastErrorKey = null;
            FailedAttempts = 0;
            RaiseChanged();
        }

        /// <summary>
        /// The save failed: the pending stats stay (locally persisted). The head stays sent (it may have landed), so the
        /// next attempt resends it unchanged and a conflict tells whether it did.
        /// </summary>
        public void FailSave(string errorKey)
        {
            if (!_saving)
            {
                return;
            }

            _saving = false;
            SaveState = PlayerStatsSaveState.Failed;
            LastErrorKey = string.IsNullOrEmpty(errorKey) ? PlayerProgressErrorKeys.Unknown : errorKey;
            FailedAttempts++;
            RaiseChanged();
        }

        /// <summary>
        /// A save hit a write-lock conflict and the record was reloaded (<paramref name="read"/>, <paramref name="remote"/>,
        /// <paramref name="remoteLock"/>). If it carries the head's batch (or the tail's), that write landed (lost
        /// response): the remote record is confirmed and only what it does not include stays pending. Otherwise another
        /// device wrote first: the pending stats are rebased on it with the head unsent again (the save stays Saving:
        /// call <see cref="TryBeginSave"/> again). A newer / corrupt record makes the ledger read only.
        /// </summary>
        public PlayerStatsConflictOutcome ResolveConflict(StatsReadResult read, in PlayerStatsRecord remote, string remoteLock)
        {
            if (!_saving)
            {
                return PlayerStatsConflictOutcome.Ignored;
            }

            if (read == StatsReadResult.Ok || read == StatsReadResult.Missing)
            {
                string remoteBatch = read == StatsReadResult.Ok ? remote.LastBatchId : null;
                PendingStats resolved = _pending.ResolveAgainst(remoteBatch, out bool applied);
                _confirmed = read == StatsReadResult.Ok ? remote.Stats : default;
                _writeLock = read == StatsReadResult.Ok && !string.IsNullOrEmpty(remoteLock) ? remoteLock : null;
                SetPending(resolved);
                _saving = false;
                if (applied)
                {
                    // The remote totals already include it: confirmed as is.
                    SaveState = PlayerStatsSaveState.Saved;
                    LastErrorKey = null;
                    FailedAttempts = 0;
                    RaiseChanged();
                    return PlayerStatsConflictOutcome.AlreadyApplied;
                }

                RaiseChanged();
                return PlayerStatsConflictOutcome.Rebased;
            }

            _saving = false;

            _confirmed = read == StatsReadResult.NewerSchema ? remote.Stats : default;
            _writeLock = null;
            LoadState = PlayerStatsLoadState.ReadOnly;
            SaveState = PlayerStatsSaveState.Failed;
            LastErrorKey = read == StatsReadResult.NewerSchema ? PlayerProgressErrorKeys.NewerSchema : PlayerProgressErrorKeys.Corrupt;
            RaiseChanged();
            return PlayerStatsConflictOutcome.ReadOnly;
        }

        /// <summary>A new round started: a finished save status (Saved / Failed) goes back to Idle. Pending stays.</summary>
        public void MarkRoundStarted()
        {
            if (_saving || SaveState == PlayerStatsSaveState.Idle)
            {
                return;
            }

            SaveState = PlayerStatsSaveState.Idle;
            RaiseChanged();
        }

        // A loaded record tells which segments landed: drop them, or make the head unsent again.
        private void ResolvePendingAgainst(string remoteBatchId)
        {
            if (!_pending.IsEmpty)
            {
                SetPending(_pending.ResolveAgainst(remoteBatchId, out _));
            }
        }

        private void SetPending(in PendingStats pending)
        {
            if (pending.Equals(_pending))
            {
                return;
            }

            _pending = pending;
            if (_pending.IsEmpty)
            {
                _pending = default;
                _store?.Clear(OwnerId);
            }
            else
            {
                _store?.Save(OwnerId, _pending);
            }
        }

        private void RaiseChanged() => Changed?.Invoke();
    }
}
