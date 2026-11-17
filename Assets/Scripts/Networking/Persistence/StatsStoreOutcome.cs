namespace AlienFarms.Networking.Persistence
{
    /// <summary>How an <see cref="IPlayerStatsStore"/> call ended.</summary>
    public enum StatsStoreOutcome
    {
        /// <summary>Loaded (see <see cref="StatsStoreResult.Read"/>) or saved.</summary>
        Ok = 0,

        /// <summary>The save's write lock is stale: another write happened first (reload and resolve).</summary>
        Conflict = 1,

        /// <summary>Transient failure (offline, rate limited, service unavailable): worth retrying with backoff.</summary>
        RetryableError = 2,

        /// <summary>Permanent failure for this attempt (not signed in, invalid request, unknown).</summary>
        Error = 3
    }
}
