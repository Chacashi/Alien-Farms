namespace AlienFarms.Animals
{
    /// <summary>High-level behaviour state of an <see cref="Animal"/>.</summary>
    public enum AnimalState
    {
        /// <summary>In the pool, not in the world.</summary>
        Inactive = 0,
        Wandering = 1,
        Fleeing = 2,
        Carried = 3,

        /// <summary>Settled in a player zone: confined to that pen, never flees, can be grabbed by either player.</summary>
        Captured = 4,

        /// <summary>Released or thrown, flying until it lands; cannot be grabbed. Landing decides Captured or free.</summary>
        Airborne = 5
    }
}
