namespace AlienFarms.Animals
{
    /// <summary>High-level behaviour state of an <see cref="Animal"/>.</summary>
    public enum AnimalState
    {
        /// <summary>In the pool, not in the world.</summary>
        Inactive = 0,
        Wandering = 1,
        Fleeing = 2,
        Carried = 3
    }
}
