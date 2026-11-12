using AlienFarms.Networking.Animals;
using AlienFarms.Networking.Player;
using UnityEngine;

namespace AlienFarms.Networking.Pooling
{
    /// <summary>
    /// Optional lifecycle log of the network pools (course topics 6 and 7), on server and clients: every network spawn /
    /// despawn of a pooled animal or bomb with its use number (first use or reused), the pool counters after it and a
    /// mark when the pool had to instantiate past its prewarm size. <see cref="NetworkSpawnRegistry"/> only creates it
    /// when <c>NetworkSettings.LogPoolLifecycle</c> is on; otherwise callers hold null and nothing runs.
    /// </summary>
    internal sealed class NetworkPoolLog
    {
        private readonly NetworkSpawnRegistry _registry;
        private readonly PoolGrowthTracker _animalGrowth;
        private readonly PoolGrowthTracker _bombGrowth;

        public NetworkPoolLog(NetworkSpawnRegistry registry, int animalPrewarm, int bombPrewarm)
        {
            _registry = registry;
            _animalGrowth = new PoolGrowthTracker(animalPrewarm);
            _bombGrowth = new PoolGrowthTracker(bombPrewarm);
        }

        /// <param name="use">Network use number of this instance for the spawn being logged (1 = first use).</param>
        public void Spawned(NetworkCarriable carriable, int use)
        {
            if (carriable != null)
            {
                Write(carriable, "spawn", use);
            }
        }

        public void Despawned(NetworkCarriable carriable)
        {
            if (carriable != null)
            {
                Write(carriable, "despawn (back to pool)", carriable.SpawnCount);
            }
        }

        private void Write(NetworkCarriable carriable, string verb, int use)
        {
            bool isAnimal = carriable is NetworkAnimal;
            NetworkPoolStats stats = _registry == null ? default : isAnimal ? _registry.AnimalStats : _registry.BombStats;
            int grew = (isAnimal ? _animalGrowth : _bombGrowth).Observe(stats.Created);

            string side = carriable.IsServer ? "server" : "client";
            string growth = grew > 0 ? $" · POOL GREW +{grew}" : string.Empty;
            Debug.Log(
                $"[Pool/{side}] {verb} '{carriable.name}' · {PoolLifecycleRules.DescribeUse(use)} · {(isAnimal ? "animals" : "bombs")} active {stats.Active} / pooled {stats.Inactive} / created {stats.Created}{growth}",
                carriable);
        }
    }
}
