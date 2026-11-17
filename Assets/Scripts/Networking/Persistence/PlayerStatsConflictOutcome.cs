namespace AlienFarms.Networking.Persistence
{
    /// <summary>What <see cref="PlayerStatsLedger.ResolveConflict"/> concluded from the reloaded record.</summary>
    public enum PlayerStatsConflictOutcome
    {
        /// <summary>No save in flight: nothing was done.</summary>
        Ignored = 0,

        /// <summary>The remote record already carries the batch in flight (the response was lost): the save succeeded.</summary>
        AlreadyApplied = 1,

        /// <summary>Another device wrote first: the pending stats now sit on the reloaded record; save again.</summary>
        Rebased = 2,

        /// <summary>The reloaded record is of a newer schema or corrupt: the ledger stops writing.</summary>
        ReadOnly = 3
    }
}
