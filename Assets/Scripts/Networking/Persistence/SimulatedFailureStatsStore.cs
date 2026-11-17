using System;
using System.Threading.Tasks;
using AlienFarms.Core;

namespace AlienFarms.Networking.Persistence
{
    /// <summary>
    /// Decorator that makes every call fail as offline (retryable) without touching the network while
    /// <c>shouldFail</c> is true (read on every call, so it can be toggled at runtime). Driven by
    /// <c>NetworkSettings.SimulateStatsFailure</c>, which is always false outside the Editor and development builds.
    /// </summary>
    public sealed class SimulatedFailureStatsStore : IPlayerStatsStore
    {
        private static readonly Task<StatsStoreResult> OfflineResult =
            Task.FromResult(StatsStoreResult.Retryable(PlayerProgressErrorKeys.Offline));

        private readonly IPlayerStatsStore _inner;
        private readonly Func<bool> _shouldFail;

        public SimulatedFailureStatsStore(IPlayerStatsStore inner, Func<bool> shouldFail)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _shouldFail = shouldFail;
        }

        private bool ShouldFail => _shouldFail != null && _shouldFail();

        public Task<StatsStoreResult> LoadOwnAsync() => ShouldFail ? OfflineResult : _inner.LoadOwnAsync();

        public Task<StatsStoreResult> LoadPublicAsync(string playerId) => ShouldFail ? OfflineResult : _inner.LoadPublicAsync(playerId);

        public Task<StatsStoreResult> SaveOwnAsync(PlayerStatsRecord record, string writeLock)
        {
            return ShouldFail ? OfflineResult : _inner.SaveOwnAsync(record, writeLock);
        }
    }
}
