namespace AlienFarms.Networking.Persistence
{
    /// <summary>Load state of the <see cref="PlayerStatsLedger"/>.</summary>
    public enum PlayerStatsLoadState
    {
        NotLoaded = 0,
        Loading = 1,
        Loaded = 2,
        Failed = 3,

        /// <summary>The cloud record is of a newer schema or corrupt: shown, never written.</summary>
        ReadOnly = 4
    }
}
