using System;
using AlienFarms.Core;

namespace AlienFarms.Networking.Persistence
{
    /// <summary>
    /// The persisted stats of one player (cloud key <see cref="PlayerStatsCodec.Key"/>): schema version, absolute totals
    /// and the id of the last batch applied (idempotence across lost responses and conflicts).
    /// </summary>
    public readonly struct PlayerStatsRecord : IEquatable<PlayerStatsRecord>
    {
        public PlayerStatsRecord(int version, in PlayerStatsSummary stats, string lastBatchId)
        {
            Version = version;
            Stats = stats;
            LastBatchId = string.IsNullOrEmpty(lastBatchId) ? null : lastBatchId;
        }

        /// <summary>Current schema, zero stats, no batch (an account without saved stats).</summary>
        public static PlayerStatsRecord Empty => new PlayerStatsRecord(PlayerStatsCodec.CurrentVersion, default, null);

        public int Version { get; }

        public PlayerStatsSummary Stats { get; }

        /// <summary>Id of the last batch applied to <see cref="Stats"/>; null when none.</summary>
        public string LastBatchId { get; }

        public bool Equals(PlayerStatsRecord other)
        {
            return Version == other.Version
                && Stats.Equals(other.Stats)
                && string.Equals(LastBatchId, other.LastBatchId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is PlayerStatsRecord other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Version, Stats, LastBatchId);
    }
}
