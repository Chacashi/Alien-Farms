using AlienFarms.Core;
using AlienFarms.Items;
using AlienFarms.Networking.Data;
using AlienFarms.Networking.Items;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Pooling
{
    /// <summary>
    /// NGO instance handler for the bomb prefab, on top of the <see cref="BombSpawner"/> pool. Clients: a spawn takes a
    /// pooled Replica (<see cref="BombSpawner.GetForReplica"/>), a despawn returns it (<see cref="Bomb.Despawn"/> as
    /// Replica). On the server NGO only calls Destroy while shutting down: <see cref="NetworkBomb"/> has already switched a
    /// bomb still in play to Replica in OnNetworkDespawn, so it goes back to the pool too; an Authority one is left alone.
    /// </summary>
    public sealed class NetworkBombPoolHandler : INetworkPrefabInstanceHandler
    {
        private readonly BombSpawner _spawner;
        private readonly NetworkRuntimeAnchor _anchor;
        private readonly NetworkPoolLog _log;

        internal NetworkBombPoolHandler(BombSpawner spawner, NetworkRuntimeAnchor anchor, NetworkPoolLog log)
        {
            _spawner = spawner;
            _anchor = anchor;
            _log = log;
        }

        public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            Bomb bomb = _spawner != null ? _spawner.GetForReplica(position) : null;
            if (bomb == null)
            {
                Debug.LogError($"[{nameof(NetworkBombPoolHandler)}] Bomb spawner unavailable; the replicated bomb cannot spawn.");
                return null;
            }

            if (!bomb.TryGetComponent(out NetworkObject networkObject) || !bomb.TryGetComponent(out NetworkBomb networkBomb))
            {
                Debug.LogError($"[{nameof(NetworkBombPoolHandler)}] Prefab '{bomb.name}' needs NetworkObject and NetworkBomb.", bomb);
                bomb.Despawn();
                return null;
            }

            networkBomb.Bind(_anchor);
            _log?.Spawned(networkBomb, networkBomb.SpawnCount + 1);
            return networkObject;
        }

        public void Destroy(NetworkObject networkObject)
        {
            if (networkObject == null || !networkObject.TryGetComponent(out Bomb bomb))
            {
                return;
            }

            if (bomb.Role == SimulationRole.Replica)
            {
                // Back under the pool container (it may still sit at the root after being carried).
                PooledParenting.BeginRelease(networkObject, out bool sync);
                bomb.Despawn();
                PooledParenting.EndRelease(networkObject, sync);
                if (_log != null && networkObject.TryGetComponent(out NetworkBomb networkBomb))
                {
                    _log.Despawned(networkBomb);
                }
            }
        }
    }
}
