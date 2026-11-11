namespace AlienFarms.Networking.Pooling
{
    /// <summary>Pool counters for the debug overlay: instances in play, waiting in the pool, and ever created.</summary>
    public readonly struct NetworkPoolStats
    {
        public NetworkPoolStats(int active, int inactive, int created)
        {
            Active = active;
            Inactive = inactive;
            Created = created;
        }

        public int Active { get; }

        public int Inactive { get; }

        /// <summary>Should stay flat during a match: every spawn reuses a pooled instance.</summary>
        public int Created { get; }
    }
}
