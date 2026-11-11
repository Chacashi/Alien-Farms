namespace AlienFarms.Core
{
    /// <summary>State of the last write of the player's persistent progress.</summary>
    public enum PlayerProgressSaveStatus
    {
        Idle = 0,
        Saving = 1,
        Saved = 2,
        Failed = 3
    }
}
