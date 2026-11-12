namespace AlienFarms.Networking.Pooling
{
    /// <summary>Pure rules of the pool lifecycle log (unit tested).</summary>
    public static class PoolLifecycleRules
    {
        /// <summary>"first use" for the first network spawn of an instance, "reused (use N)" afterwards, "unspawned" for 0 or less.</summary>
        public static string DescribeUse(int use)
        {
            if (use <= 0)
            {
                return "unspawned";
            }

            return use == 1 ? "first use" : $"reused (use {use})";
        }

        /// <summary>
        /// Instances the pool created since the previous reading (0 if it did not grow). A negative previous value means
        /// no reading yet: nothing to compare with.
        /// </summary>
        public static int Growth(int previousCreated, int currentCreated)
        {
            if (previousCreated < 0 || currentCreated <= previousCreated)
            {
                return 0;
            }

            return currentCreated - previousCreated;
        }
    }
}
