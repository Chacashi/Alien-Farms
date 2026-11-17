using System.Collections.Generic;
using AlienFarms.Networking.Persistence;

namespace AlienFarms.Networking.Tests.Persistence
{
    /// <summary>In-memory <see cref="IPendingStatsStore"/> that counts writes.</summary>
    public sealed class FakePendingStatsStore : IPendingStatsStore
    {
        private readonly Dictionary<string, PendingStats> _entries = new Dictionary<string, PendingStats>();

        public int SaveCount { get; private set; }
        public int ClearCount { get; private set; }

        public bool Has(string ownerId) => _entries.ContainsKey(ownerId);

        public PendingStats Get(string ownerId) => _entries.TryGetValue(ownerId, out PendingStats pending) ? pending : default;

        public void Put(string ownerId, PendingStats pending) => _entries[ownerId] = pending;

        public PendingStats Load(string ownerId) => Get(ownerId);

        public void Save(string ownerId, in PendingStats pending)
        {
            SaveCount++;
            _entries[ownerId] = pending;
        }

        public void Clear(string ownerId)
        {
            ClearCount++;
            _entries.Remove(ownerId);
        }
    }
}
