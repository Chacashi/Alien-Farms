using AlienFarms.Animals;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Networking.Animals;
using AlienFarms.Networking.Data;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Pooling
{
    /// <summary>
    /// NGO instance handler for one animal type, on top of the spawner's existing pool. Clients: a spawn takes a pooled
    /// Replica (<see cref="AnimalTypePool.GetForReplica"/>) instead of instantiating, and a despawn returns it
    /// (<see cref="Animal.Despawn"/> as Replica = back to the pool, no events). On the server instances come from the
    /// simulation itself (see <see cref="NetworkSpawnRegistry"/>); NGO only calls Destroy there while shutting down:
    /// <see cref="NetworkAnimal"/> has already switched an animal still in play to Replica in OnNetworkDespawn, so it
    /// goes back to its pool too, while an Authority instance is left alone.
    /// </summary>
    public sealed class NetworkAnimalPoolHandler : INetworkPrefabInstanceHandler
    {
        private readonly AnimalSpawner _spawner;
        private readonly AnimalData _data;
        private readonly NetworkRuntimeAnchor _anchor;
        private readonly NetworkPoolLog _log;

        internal NetworkAnimalPoolHandler(AnimalSpawner spawner, AnimalData data, NetworkRuntimeAnchor anchor, NetworkPoolLog log)
        {
            _spawner = spawner;
            _data = data;
            _anchor = anchor;
            _log = log;
        }

        public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            if (_spawner == null || !_spawner.TryGetPool(_data, out AnimalTypePool pool))
            {
                Debug.LogError($"[{nameof(NetworkAnimalPoolHandler)}] No pool for '{(_data != null ? _data.name : "null")}'; the replicated animal cannot spawn.");
                return null;
            }

            Animal animal = pool.GetForReplica(position, rotation);
            if (!animal.TryGetComponent(out NetworkObject networkObject) || !animal.TryGetComponent(out NetworkAnimal networkAnimal))
            {
                Debug.LogError($"[{nameof(NetworkAnimalPoolHandler)}] Prefab '{animal.name}' needs NetworkObject and NetworkAnimal.", animal);
                animal.Despawn();
                return null;
            }

            networkAnimal.Bind(_anchor);
            _log?.Spawned(networkAnimal, networkAnimal.SpawnCount + 1);
            return networkObject;
        }

        public void Destroy(NetworkObject networkObject)
        {
            if (networkObject == null || !networkObject.TryGetComponent(out Animal animal))
            {
                return;
            }

            if (animal.Role == SimulationRole.Replica)
            {
                // Back under the pool container (it may still sit at the root after being carried).
                PooledParenting.BeginRelease(networkObject, out bool sync);
                animal.Despawn();
                PooledParenting.EndRelease(networkObject, sync);
                if (_log != null && networkObject.TryGetComponent(out NetworkAnimal networkAnimal))
                {
                    _log.Despawned(networkAnimal);
                }
            }
        }
    }
}
