using System.Collections.Generic;
using System.Threading.Tasks;
using AlienFarms.Networking.Persistence;

namespace AlienFarms.Networking.Tests.Persistence
{
    /// <summary>
    /// In-memory <see cref="IPlayerStatsStore"/> answering synchronously from queued results (defaults: a missing record,
    /// a successful save). A queued null task stands for a call that never answers.
    /// </summary>
    public sealed class FakePlayerStatsStore : IPlayerStatsStore
    {
        private readonly Queue<Task<StatsStoreResult>> _loads = new Queue<Task<StatsStoreResult>>();
        private readonly Queue<Task<StatsStoreResult>> _saves = new Queue<Task<StatsStoreResult>>();
        private readonly Queue<Task<StatsStoreResult>> _publicLoads = new Queue<Task<StatsStoreResult>>();

        public int LoadCount { get; private set; }
        public int SaveCount { get; private set; }
        public int PublicLoadCount { get; private set; }
        public string LastPublicId { get; private set; }
        public PlayerStatsRecord LastSaved { get; private set; }
        public string LastSaveLock { get; private set; }

        /// <summary>Answer of every save once the queue is empty (null = a successful save).</summary>
        public StatsStoreResult? DefaultSave { get; set; }

        public void QueueLoad(StatsStoreResult result) => _loads.Enqueue(Task.FromResult(result));

        public void QueueSave(StatsStoreResult result) => _saves.Enqueue(Task.FromResult(result));

        public void QueuePendingSave(TaskCompletionSource<StatsStoreResult> source) => _saves.Enqueue(source.Task);

        public void QueuePublicLoad(StatsStoreResult result) => _publicLoads.Enqueue(Task.FromResult(result));

        public Task<StatsStoreResult> LoadOwnAsync()
        {
            LoadCount++;
            return _loads.Count > 0
                ? _loads.Dequeue()
                : Task.FromResult(StatsStoreResult.Loaded(StatsReadResult.Missing, PlayerStatsRecord.Empty, null));
        }

        public Task<StatsStoreResult> LoadPublicAsync(string playerId)
        {
            PublicLoadCount++;
            LastPublicId = playerId;
            return _publicLoads.Count > 0
                ? _publicLoads.Dequeue()
                : Task.FromResult(StatsStoreResult.Loaded(StatsReadResult.Missing, PlayerStatsRecord.Empty, null));
        }

        public Task<StatsStoreResult> SaveOwnAsync(PlayerStatsRecord record, string writeLock)
        {
            SaveCount++;
            LastSaved = record;
            LastSaveLock = writeLock;
            if (_saves.Count > 0)
            {
                return _saves.Dequeue();
            }

            return Task.FromResult(DefaultSave ?? StatsStoreResult.Saved("lock-" + SaveCount));
        }
    }
}
