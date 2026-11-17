using AlienFarms.Core;

namespace AlienFarms.Networking.Persistence
{
    /// <summary>
    /// Outcome of an <see cref="IPlayerStatsStore"/> call, free of SDK types. A load carries the decoded
    /// <see cref="Record"/> (<see cref="Read"/>) and its <see cref="WriteLock"/>; a save carries the new write lock.
    /// </summary>
    public readonly struct StatsStoreResult
    {
        private StatsStoreResult(
            StatsStoreOutcome outcome,
            StatsReadResult read,
            in PlayerStatsRecord record,
            string writeLock,
            string errorKey,
            float retryAfter)
        {
            Outcome = outcome;
            Read = read;
            Record = record;
            WriteLock = string.IsNullOrEmpty(writeLock) ? null : writeLock;
            ErrorKey = errorKey;
            RetryAfter = retryAfter > 0f ? retryAfter : 0f;
        }

        public StatsStoreOutcome Outcome { get; }

        /// <summary>Decoding of the loaded record (loads only; <see cref="StatsReadResult.Missing"/> otherwise).</summary>
        public StatsReadResult Read { get; }

        /// <summary>Loaded record (<see cref="PlayerStatsRecord.Empty"/> when missing or not a load).</summary>
        public PlayerStatsRecord Record { get; }

        /// <summary>Write lock of the loaded or saved record; null when none.</summary>
        public string WriteLock { get; }

        /// <summary>Localization key (<see cref="PlayerProgressErrorKeys"/>) of a failure; null on success / conflict.</summary>
        public string ErrorKey { get; }

        /// <summary>Seconds the service asked to wait before retrying (rate limit); 0 when none.</summary>
        public float RetryAfter { get; }

        public bool IsOk => Outcome == StatsStoreOutcome.Ok;

        public static StatsStoreResult Loaded(StatsReadResult read, in PlayerStatsRecord record, string writeLock)
        {
            return new StatsStoreResult(StatsStoreOutcome.Ok, read, record, writeLock, null, 0f);
        }

        public static StatsStoreResult Saved(string writeLock)
        {
            return new StatsStoreResult(StatsStoreOutcome.Ok, StatsReadResult.Missing, PlayerStatsRecord.Empty, writeLock, null, 0f);
        }

        public static StatsStoreResult Conflict()
        {
            return new StatsStoreResult(StatsStoreOutcome.Conflict, StatsReadResult.Missing, PlayerStatsRecord.Empty, null, null, 0f);
        }

        public static StatsStoreResult Retryable(string errorKey, float retryAfter = 0f)
        {
            return new StatsStoreResult(StatsStoreOutcome.RetryableError, StatsReadResult.Missing, PlayerStatsRecord.Empty, null,
                string.IsNullOrEmpty(errorKey) ? PlayerProgressErrorKeys.Unknown : errorKey, retryAfter);
        }

        public static StatsStoreResult Failed(string errorKey)
        {
            return new StatsStoreResult(StatsStoreOutcome.Error, StatsReadResult.Missing, PlayerStatsRecord.Empty, null,
                string.IsNullOrEmpty(errorKey) ? PlayerProgressErrorKeys.Unknown : errorKey, 0f);
        }
    }
}
