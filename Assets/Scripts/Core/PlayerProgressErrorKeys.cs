namespace AlienFarms.Core
{
    /// <summary>
    /// Localization keys (UI table) of the persistent progress (cloud stats) errors the cloud save service reports
    /// through the <c>PlayerProgressChannel</c>. Null / empty means "no error".
    /// </summary>
    public static class PlayerProgressErrorKeys
    {
        /// <summary>No internet connection.</summary>
        public const string Offline = "stats.error.offline";

        /// <summary>Too many requests; retried later.</summary>
        public const string RateLimited = "stats.error.rate_limited";

        /// <summary>The service is down or not reachable.</summary>
        public const string Unavailable = "stats.error.unavailable";

        /// <summary>Not signed in (missing player id or access token, unauthorized).</summary>
        public const string SignedOut = "stats.error.signed_out";

        /// <summary>The cloud data was written by a newer version of the game; shown but never overwritten.</summary>
        public const string NewerSchema = "stats.error.newer_schema";

        /// <summary>The cloud data is invalid; shown as is but never overwritten.</summary>
        public const string Corrupt = "stats.error.corrupt";

        public const string Unknown = "stats.error.unknown";
    }
}
