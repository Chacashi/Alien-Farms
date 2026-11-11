namespace AlienFarms.Core
{
    /// <summary>Load state of the player's persistent progress (cloud stats).</summary>
    public enum PlayerProgressStatus
    {
        Unavailable = 0,
        Loading = 1,
        Loaded = 2,
        Failed = 3
    }
}
