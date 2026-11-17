namespace AlienFarms.Networking.Persistence
{
    /// <summary>Outcome of decoding persisted stats (cloud record or local pending).</summary>
    public enum StatsReadResult
    {
        /// <summary>Valid data of a known schema version.</summary>
        Ok = 0,

        /// <summary>Nothing stored (first time): read as empty stats.</summary>
        Missing = 1,

        /// <summary>Written by a newer schema: shown as is, never overwritten.</summary>
        NewerSchema = 2,

        /// <summary>Invalid data: never overwritten.</summary>
        Corrupt = 3
    }
}
