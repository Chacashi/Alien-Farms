namespace AlienFarms.Animals
{
    /// <summary>What an airborne animal does this physics step (see <see cref="AnimalLanding.Evaluate"/>).</summary>
    public enum AirborneOutcome
    {
        /// <summary>Still flying.</summary>
        Keep = 0,

        /// <summary>Touched down: resolve the zone it settled in.</summary>
        Land = 1,

        /// <summary>Lost (fell off the map or stuck in the air): return it to the pool.</summary>
        Despawn = 2,

        /// <summary>Resting on a player (not on the ground): push it off horizontally so it falls and lands.</summary>
        SlideOff = 3,

        /// <summary>On a player for too long (sliding is blocked): move it to free ground beside the player.</summary>
        Relocate = 4
    }
}
