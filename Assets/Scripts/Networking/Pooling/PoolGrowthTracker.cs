namespace AlienFarms.Networking.Pooling
{
    /// <summary>
    /// Pure: how many instances a pool created beyond everything seen so far. Starts at the prewarm size, so instances
    /// created at load time (even when a replicated spawn arrives before the prewarm) never count as growth, while the
    /// first spawn that instantiates past it does.
    /// </summary>
    public sealed class PoolGrowthTracker
    {
        private int _reference;

        /// <param name="prewarm">Instances the pool creates up front (the expected stable "created" count).</param>
        public PoolGrowthTracker(int prewarm)
        {
            _reference = prewarm > 0 ? prewarm : 0;
        }

        /// <summary>Highest "created" count accounted for (the prewarm size or a larger reading).</summary>
        public int Reference => _reference;

        /// <summary>Instances created past the reference by this reading (0 if none); the reference then moves up.</summary>
        public int Observe(int created)
        {
            int grew = PoolLifecycleRules.Growth(_reference, created);
            if (created > _reference)
            {
                _reference = created;
            }

            return grew;
        }
    }
}
