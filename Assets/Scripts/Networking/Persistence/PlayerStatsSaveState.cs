namespace AlienFarms.Networking.Persistence
{
    /// <summary>Save state of the <see cref="PlayerStatsLedger"/> (mirrors <c>PlayerProgressSaveStatus</c>).</summary>
    public enum PlayerStatsSaveState
    {
        Idle = 0,
        Saving = 1,
        Saved = 2,
        Failed = 3
    }
}
