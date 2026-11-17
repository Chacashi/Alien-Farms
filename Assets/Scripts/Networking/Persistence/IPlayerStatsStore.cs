using System.Threading.Tasks;

namespace AlienFarms.Networking.Persistence
{
    /// <summary>
    /// Strategy for the persisted stats of the signed-in player (key <see cref="PlayerStatsCodec.Key"/>). Implementations
    /// never throw: every failure comes back as a <see cref="StatsStoreResult"/>.
    /// </summary>
    public interface IPlayerStatsStore
    {
        /// <summary>The signed-in player's record (Public access class).</summary>
        Task<StatsStoreResult> LoadOwnAsync();

        /// <summary>Another player's public record, by UGS player id.</summary>
        Task<StatsStoreResult> LoadPublicAsync(string playerId);

        /// <summary>
        /// Writes <paramref name="record"/> (absolute totals) with <paramref name="writeLock"/> (null = no conflict control,
        /// only for the first write). <see cref="StatsStoreOutcome.Conflict"/> when the lock is stale.
        /// </summary>
        Task<StatsStoreResult> SaveOwnAsync(PlayerStatsRecord record, string writeLock);
    }
}
