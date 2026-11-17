using System;
using System.Threading;
using System.Threading.Tasks;
using AlienFarms.Core;
using AlienFarms.Data;
using UnityEngine;

namespace AlienFarms.Networking.Persistence
{
    /// <summary>
    /// Orchestration behind <see cref="CloudSaveStatsService"/> (plain C#, so it is tested in EditMode with a fake store):
    /// <list type="bullet">
    /// <item>Follows the signed-in UGS id of <see cref="OnlineSessionChannel"/>: a new id resets the ledger and loads.</item>
    /// <item>Saves whenever the ledger has something pending; conflicts are reloaded and resolved by the ledger.</item>
    /// <item>Retryable failures retry with backoff (delay × 3 per attempt, a Retry-After wins when longer), up to
    /// <see cref="NetworkSettings.StatsSaveAttempts"/>; a new round, a new account or a new root budgets again.</item>
    /// <item>Reads the rival's public stats while in a session (<see cref="RivalRules"/>, <see cref="NetworkSettings.ReadRivalStats"/>).</item>
    /// <item>Only writer of <see cref="PlayerProgressChannel"/> (stats, save status, rival, host-left win notice; a
    /// read-only ledger publishes its error key and, with rounds it cannot save, a failed save).</item>
    /// </list>
    /// One I/O at a time; event driven (no per-frame work). After every await it checks that it was not disposed and that
    /// the ledger's epoch did not change (stale results are dropped).
    /// </summary>
    public sealed class PlayerStatsSync : IDisposable
    {
        private readonly PlayerStatsLedger _ledger;
        private readonly IPlayerStatsStore _store;
        private readonly OnlineSessionChannel _session;
        private readonly PlayerProgressChannel _progress;
        private readonly NetworkSettings _settings;
        private readonly UnityEngine.Object _logContext;
        private readonly OnlineParticipant[] _participants = new OnlineParticipant[OnlineSessionChannel.MaxParticipants];
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private readonly Func<float, CancellationToken, Task> _delay;

        private bool _started;
        private bool _disposed;
        private bool _evaluating;
        private bool _evaluateAgain;

        // The single I/O in flight (own load / save, or the rival read).
        private bool _busy;
        private bool _busyIsOwn;

        // Retry budget of the current pending stats (reset by a new epoch, a new pending or a success).
        private int _attempts;
        private bool _exhausted;
        private bool _retryScheduled;
        private int _retryGeneration;
        private int _budgetEpoch = -1;
        // Total pending delta the budget belongs to (a new round changes it; freezing or merging segments does not).
        private PlayerStatsSummary _budgetDelta;

        private string _rivalId;
        private bool _rivalDone;

        /// <param name="delay">Waits before a retry (seconds); null = <see cref="Task.Delay(TimeSpan, CancellationToken)"/>.</param>
        public PlayerStatsSync(
            PlayerStatsLedger ledger,
            IPlayerStatsStore store,
            OnlineSessionChannel session,
            PlayerProgressChannel progress,
            NetworkSettings settings,
            UnityEngine.Object logContext = null,
            Func<float, CancellationToken, Task> delay = null)
        {
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _session = session != null ? session : throw new ArgumentNullException(nameof(session));
            _progress = progress != null ? progress : throw new ArgumentNullException(nameof(progress));
            _settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
            _logContext = logContext;
            _delay = delay ?? DefaultDelay;
        }

        /// <summary>True while a retry waits for its delay.</summary>
        public bool IsRetryScheduled => _retryScheduled;

        /// <summary>Consecutive failed attempts of the current budget.</summary>
        public int Attempts => _attempts;

        private int MaxAttempts => Mathf.Max(1, _settings.StatsSaveAttempts);

        /// <summary>Subscribes and evaluates right away (load, pending save, rival).</summary>
        public void Start()
        {
            if (_started || _disposed)
            {
                return;
            }

            _started = true;
            _ledger.Changed += Evaluate;
            _session.Changed += Evaluate;
            _progress.HostLeftWinNoticeConsumeRequested += HandleHostLeftWinNoticeConsumed;

            // A previous sync torn down mid-I/O leaves nothing half done (see Dispose); this is only a safety net.
            if (_ledger.LoadState == PlayerStatsLoadState.Loading || _ledger.IsSaving)
            {
                _ledger.Reset(_ledger.OwnerId);
            }

            _progress.ClearRival();
            Evaluate();
        }

        /// <summary>
        /// Stops: cancels retries and drops results still in flight. An own load / save in flight is abandoned by
        /// resetting the ledger (the pending stats come back from PlayerPrefs; the next sync reloads, and the batch id
        /// makes a save that did land count once).
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_started)
            {
                _ledger.Changed -= Evaluate;
                _session.Changed -= Evaluate;
                _progress.HostLeftWinNoticeConsumeRequested -= HandleHostLeftWinNoticeConsumed;
            }

            _cancellation.Cancel();
            _cancellation.Dispose();
            if (_busy && _busyIsOwn)
            {
                _ledger.Reset(_ledger.OwnerId);
            }

            _busy = false;
        }

        private void Evaluate()
        {
            if (_disposed)
            {
                return;
            }

            // Ledger and channel events are raised from inside the steps below: run again instead of nesting.
            if (_evaluating)
            {
                _evaluateAgain = true;
                return;
            }

            _evaluating = true;
            try
            {
                do
                {
                    _evaluateAgain = false;
                    EvaluateOnce();
                }
                while (_evaluateAgain && !_disposed);
            }
            finally
            {
                _evaluating = false;
            }
        }

        private void EvaluateOnce()
        {
            string owner = _session.LocalPlayerId;
            if (string.IsNullOrEmpty(owner))
            {
                owner = null;
            }

            if (!string.Equals(owner, _ledger.OwnerId, StringComparison.Ordinal))
            {
                // New account or signed out: whatever was retrying belongs to the previous one.
                _retryGeneration++;
                _retryScheduled = false;
                _rivalId = null;
                _rivalDone = false;
                _ledger.Reset(owner);
                Log(owner != null ? $"UGS id {owner}: loading stats." : "Signed out: stats unavailable.");
            }

            UpdateBudget();
            Publish();
            if (_busy || _ledger.OwnerId == null)
            {
                return;
            }

            if (!_retryScheduled && !_exhausted)
            {
                PlayerStatsLoadState load = _ledger.LoadState;
                if (load == PlayerStatsLoadState.NotLoaded || load == PlayerStatsLoadState.Failed)
                {
                    LoadOwn();
                    return;
                }

                if (_ledger.CanSave)
                {
                    SaveOwn();
                    return;
                }
            }

            EvaluateRival();
        }

        // ---- Own record ----

        private async void LoadOwn()
        {
            PlayerStatsLedger ledger = _ledger;
            int epoch = ledger.Epoch;
            _busy = true;
            _busyIsOwn = true;
            if (!ledger.BeginLoad())
            {
                _busy = false;
                return;
            }

            StatsStoreResult result = await SafeLoadOwnAsync();
            if (IsStale(epoch))
            {
                return;
            }

            if (result.IsOk)
            {
                ledger.ApplyLoaded(result.Read, result.Record, result.WriteLock);
                ResetBudget();
                Log($"Stats loaded (v{result.Record.Version}, {result.Read}): {Describe(ledger.Confirmed)}; pending {Describe(ledger.Pending.Delta)}; state {ledger.LoadState}.");
            }
            else
            {
                ledger.FailLoad(result.ErrorKey);
                Log($"Stats load failed ({result.Outcome}, {result.ErrorKey}).");
                HandleFailure(result);
            }

            EndOperation();
        }

        private async void SaveOwn()
        {
            PlayerStatsLedger ledger = _ledger;
            int epoch = ledger.Epoch;
            _busy = true;
            _busyIsOwn = true;
            if (!ledger.TryBeginSave(out PlayerStatsRecord toWrite, out string writeLock))
            {
                _busy = false;
                return;
            }

            Log($"Saving stats (batch {toWrite.LastBatchId}, lock {(writeLock ?? "none")}): {Describe(toWrite.Stats)}.");
            StatsStoreResult result = await SafeSaveAsync(toWrite, writeLock);
            if (IsStale(epoch))
            {
                return;
            }

            switch (result.Outcome)
            {
                case StatsStoreOutcome.Ok:
                    ledger.CompleteSave(result.WriteLock);
                    ResetBudget();
                    Log($"Stats saved (lock {result.WriteLock}): {Describe(ledger.Confirmed)}.");
                    break;
                case StatsStoreOutcome.Conflict:
                    _attempts++;
                    if (_attempts >= MaxAttempts)
                    {
                        ledger.FailSave(PlayerProgressErrorKeys.Unknown);
                        _exhausted = true;
                        Log("Stats save conflict: out of attempts.");
                        break;
                    }

                    Log("Stats save conflict: reloading.");
                    StatsStoreResult reload = await SafeLoadOwnAsync();
                    if (IsStale(epoch))
                    {
                        return;
                    }

                    if (reload.IsOk)
                    {
                        PlayerStatsConflictOutcome outcome = ledger.ResolveConflict(reload.Read, reload.Record, reload.WriteLock);
                        if (outcome == PlayerStatsConflictOutcome.AlreadyApplied)
                        {
                            ResetBudget();
                        }

                        Log($"Stats conflict resolved: {outcome}.");
                    }
                    else
                    {
                        // The conflict already used this attempt.
                        ledger.FailSave(reload.ErrorKey);
                        HandleFailure(reload, false);
                    }

                    break;
                default:
                    ledger.FailSave(result.ErrorKey);
                    Log($"Stats save failed ({result.Outcome}, {result.ErrorKey}).");
                    HandleFailure(result);
                    break;
            }

            EndOperation();
        }

        private void HandleFailure(in StatsStoreResult result, bool countAttempt = true)
        {
            if (countAttempt)
            {
                _attempts++;
            }

            if (result.Outcome != StatsStoreOutcome.RetryableError || _attempts >= MaxAttempts)
            {
                _exhausted = true;
                Log($"Stats: giving up after {_attempts} attempt(s) until the next round or sign-in.");
                return;
            }

            float delay = _settings.StatsRetryDelaySeconds * Mathf.Pow(3f, _attempts - 1);
            ScheduleRetry(Mathf.Max(delay, result.RetryAfter));
        }

        private async void ScheduleRetry(float seconds)
        {
            int generation = ++_retryGeneration;
            _retryScheduled = true;
            Log($"Stats: retrying in {seconds:0.#} s (attempt {_attempts + 1}/{MaxAttempts}).");
            try
            {
                await _delay(seconds, _cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            if (_disposed || generation != _retryGeneration)
            {
                return;
            }

            _retryScheduled = false;
            Evaluate();
        }

        // ---- Rival ----

        private void EvaluateRival()
        {
            string rival = null;
            if (_settings.ReadRivalStats && _session.Status == OnlineSessionStatus.InSession)
            {
                int count = Mathf.Min(_session.ParticipantCount, _participants.Length);
                for (int i = 0; i < count; i++)
                {
                    _participants[i] = _session.GetParticipant(i);
                }

                RivalRules.TryFindRival(new ReadOnlySpan<OnlineParticipant>(_participants, 0, count), out rival);
            }

            if (!string.Equals(rival, _rivalId, StringComparison.Ordinal))
            {
                _rivalId = rival;
                _rivalDone = false;
                if (rival == null)
                {
                    _progress.ClearRival();
                }
                else
                {
                    _progress.SetRivalLoading();
                }
            }

            if (_rivalId != null && !_rivalDone && !_busy)
            {
                LoadRival(_rivalId);
            }
        }

        private async void LoadRival(string rivalId)
        {
            _busy = true;
            _busyIsOwn = false;
            StatsStoreResult result;
            try
            {
                result = await _store.LoadPublicAsync(rivalId);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, _logContext);
                result = StatsStoreResult.Failed(PlayerProgressErrorKeys.Unknown);
            }

            if (_disposed)
            {
                return;
            }

            _busy = false;
            if (string.Equals(rivalId, _rivalId, StringComparison.Ordinal))
            {
                _rivalDone = true;
                if (result.IsOk && result.Read != StatsReadResult.Corrupt)
                {
                    _progress.SetRival(result.Read == StatsReadResult.Missing ? default : result.Record.Stats);
                    Log($"Rival {rivalId} stats loaded ({result.Read}).");
                }
                else
                {
                    _progress.SetRivalFailed();
                    Log($"Rival {rivalId} stats unavailable ({result.Outcome}, {result.Read}, {result.ErrorKey}).");
                }
            }

            Evaluate();
        }

        // ---- Helpers ----

        private void HandleHostLeftWinNoticeConsumed()
        {
            if (!_disposed)
            {
                _ledger.ConsumeHostLeftWinNotice();
            }
        }

        private void Publish()
        {
            _progress.SetHostLeftWinNotice(_ledger.HasHostLeftWinNotice);
            if (_ledger.OwnerId == null)
            {
                _progress.SetUnavailable();
                return;
            }

            switch (_ledger.LoadState)
            {
                case PlayerStatsLoadState.Loaded:
                    _progress.SetLoaded(_ledger.Displayed);
                    break;
                case PlayerStatsLoadState.ReadOnly:
                    // Newer / corrupt cloud data: shown with its error; rounds recorded meanwhile are never saved.
                    _progress.SetLoaded(_ledger.Displayed, _ledger.LastErrorKey);
                    _progress.SetSaveStatus(_ledger.HasPending ? PlayerProgressSaveStatus.Failed : ToSaveStatus(_ledger.SaveState));
                    return;
                case PlayerStatsLoadState.Failed:
                    _progress.SetFailed(_ledger.LastErrorKey);
                    break;
                default:
                    _progress.SetLoading();
                    break;
            }

            _progress.SetSaveStatus(ToSaveStatus(_ledger.SaveState));
        }

        private static PlayerProgressSaveStatus ToSaveStatus(PlayerStatsSaveState state)
        {
            switch (state)
            {
                case PlayerStatsSaveState.Saving:
                    return PlayerProgressSaveStatus.Saving;
                case PlayerStatsSaveState.Saved:
                    return PlayerProgressSaveStatus.Saved;
                case PlayerStatsSaveState.Failed:
                    return PlayerProgressSaveStatus.Failed;
                default:
                    return PlayerProgressSaveStatus.Idle;
            }
        }

        private void UpdateBudget()
        {
            if (_budgetEpoch == _ledger.Epoch && _budgetDelta.Equals(_ledger.Pending.Delta))
            {
                return;
            }

            _budgetEpoch = _ledger.Epoch;
            _budgetDelta = _ledger.Pending.Delta;
            _attempts = 0;
            _exhausted = false;
        }

        private void ResetBudget()
        {
            _attempts = 0;
            _exhausted = false;
            _budgetEpoch = _ledger.Epoch;
            _budgetDelta = _ledger.Pending.Delta;
        }

        /// <summary>True (and handled) when the result must be dropped: disposed, or the ledger was reset meanwhile.</summary>
        private bool IsStale(int epoch)
        {
            if (_disposed)
            {
                return true;
            }

            if (_ledger.Epoch == epoch)
            {
                return false;
            }

            EndOperation();
            return true;
        }

        private void EndOperation()
        {
            _busy = false;
            Evaluate();
        }

        private async Task<StatsStoreResult> SafeLoadOwnAsync()
        {
            try
            {
                return await _store.LoadOwnAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, _logContext);
                return StatsStoreResult.Failed(PlayerProgressErrorKeys.Unknown);
            }
        }

        private async Task<StatsStoreResult> SafeSaveAsync(PlayerStatsRecord record, string writeLock)
        {
            try
            {
                return await _store.SaveOwnAsync(record, writeLock);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, _logContext);
                return StatsStoreResult.Failed(PlayerProgressErrorKeys.Unknown);
            }
        }

        private void Log(string message)
        {
            if (_settings.LogPersistence)
            {
                Debug.Log($"[{nameof(CloudSaveStatsService)}] {message}", _logContext);
            }
        }

        private static string Describe(in PlayerStatsSummary stats)
        {
            return $"{stats.MatchesPlayed} matches, {stats.Wins}W/{stats.Draws}D/{stats.Losses}L, {stats.AnimalsScored} animals, {stats.TacklesLanded} tackles";
        }

        private static Task DefaultDelay(float seconds, CancellationToken token)
        {
            return Task.Delay(TimeSpan.FromSeconds(Mathf.Max(0f, seconds)), token);
        }
    }
}
