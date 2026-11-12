using System;

namespace AlienFarms.Networking.Pooling
{
    /// <summary>Pool counters for the debug overlay: instances in play, waiting in the pool, and ever created.</summary>
    public readonly struct NetworkPoolStats : IEquatable<NetworkPoolStats>
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

        public bool Equals(NetworkPoolStats other) =>
            Active == other.Active && Inactive == other.Inactive && Created == other.Created;

        public override bool Equals(object obj) => obj is NetworkPoolStats other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Active, Inactive, Created);

        public static bool operator ==(NetworkPoolStats left, NetworkPoolStats right) => left.Equals(right);

        public static bool operator !=(NetworkPoolStats left, NetworkPoolStats right) => !left.Equals(right);
    }
}
