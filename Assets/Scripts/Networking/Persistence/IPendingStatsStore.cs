namespace AlienFarms.Networking.Persistence
{
    /// <summary>
    /// Local storage of the unconfirmed stats, one entry per account (survives closing the game). Implementations ignore
    /// (and delete) invalid or newer entries.
    /// </summary>
    public interface IPendingStatsStore
    {
        /// <summary>The pending stats of <paramref name="ownerId"/>; default (empty) when none or invalid.</summary>
        PendingStats Load(string ownerId);

        /// <summary>Writes <paramref name="pending"/> durably (flushed right away).</summary>
        void Save(string ownerId, in PendingStats pending);

        /// <summary>Deletes the entry of <paramref name="ownerId"/>, if any.</summary>
        void Clear(string ownerId);
    }
}
