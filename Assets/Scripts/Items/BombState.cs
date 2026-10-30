namespace AlienFarms.Items
{
    /// <summary>Lifecycle of a pooled <see cref="Bomb"/>.</summary>
    public enum BombState
    {
        /// <summary>In the pool, not in play.</summary>
        Inactive = 0,

        /// <summary>Inert on the ground (or rolling): can be picked up, never explodes.</summary>
        Idle = 1,

        /// <summary>At a player's hold point.</summary>
        Carried = 2,

        /// <summary>Thrown: explodes on the first contact that is not its thrower, or after a timeout.</summary>
        Armed = 3
    }
}
