using AlienFarms.Animals;
using AlienFarms.Data;
using AlienFarms.Items;
using AlienFarms.Networking.Animals;
using AlienFarms.Networking.Data;
using AlienFarms.Networking.Items;
using AlienFarms.Networking.Player;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Pooling
{
    /// <summary>
    /// Network pooling of animals and bombs (online gameplay scene, under OnlineModeRoot).
    /// <list type="bullet">
    /// <item>Every instance: registers an <see cref="INetworkPrefabInstanceHandler"/> per animal prefab and for the
    /// bomb in Awake, before any spawn of this scene can arrive (NGO defers them until the scene finished loading), and
    /// removes them on destroy.</item>
    /// <item>Server: is the spawners' network hook. A simulation spawn becomes <c>NetworkObject.Spawn</c> (replicated
    /// state reset first); a despawn becomes <c>Despawn(false)</c> and the instance goes straight back to its pool.</item>
    /// </list>
    /// Exposes the pool counters (active, inactive, created) for the debug overlay.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkSpawnRegistry : MonoBehaviour, IAnimalNetworkHook, IBombNetworkHook
    {
        [Header("Config")]
        [SerializeField, Tooltip("Runtime anchor of the NetworkRoot (NetworkManager, server check, players).")]
        private NetworkRuntimeAnchor _anchor;

        [SerializeField, Tooltip("Same spawn settings as the AnimalSpawner: one handler per entry's prefab.")]
        private AnimalSpawnSettings _animalSettings;

        [SerializeField, Tooltip("Same bomb data as the BombSpawner (its prefab gets the bomb handler).")]
        private BombData _bombData;

        [Header("References")]
        [SerializeField, Tooltip("Animal spawner whose pools back the animal handlers.")]
        private AnimalSpawner _animalSpawner;

        [SerializeField, Tooltip("Optional bomb spawner whose pool backs the bomb handler.")]
        private BombSpawner _bombSpawner;

        private NetworkManager _manager;
        private NetworkPoolLog _poolLog;
        private GameObject[] _registeredPrefabs;
        private int _registeredCount;
        private bool _hooked;

        /// <summary>Animal pools summed over every type.</summary>
        public NetworkPoolStats AnimalStats
        {
            get
            {
                int active = 0;
                int inactive = 0;
                int created = 0;
                AnimalSpawnEntry[] entries = _animalSettings != null ? _animalSettings.Entries : null;
                if (entries != null && _animalSpawner != null)
                {
                    for (int i = 0; i < entries.Length; i++)
                    {
                        if (_animalSpawner.TryGetPool(entries[i].Data, out AnimalTypePool pool))
                        {
                            active += pool.CountActive;
                            inactive += pool.CountInactive;
                            created += pool.CountAll;
                        }
                    }
                }

                return new NetworkPoolStats(active, inactive, created);
            }
        }

        public NetworkPoolStats BombStats => _bombSpawner != null
            ? new NetworkPoolStats(_bombSpawner.PoolCountActive, _bombSpawner.PoolCountInactive, _bombSpawner.PoolCountAll)
            : default;

        private void Awake()
        {
            _manager = _anchor != null ? _anchor.Manager : null;
            if (_manager == null)
            {
                Debug.LogError($"[{nameof(NetworkSpawnRegistry)}] No NetworkManager published on the anchor; animals and bombs are not replicated.", this);
                return;
            }

            _poolLog = CreatePoolLog();
            AnimalSpawnEntry[] entries = _animalSettings != null ? _animalSettings.Entries : null;
            int capacity = (entries != null ? entries.Length : 0) + 1;
            _registeredPrefabs = new GameObject[capacity];
            _registeredCount = 0;

            if (entries != null && _animalSpawner != null)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    AnimalData data = entries[i].Data;
                    if (data != null && data.Prefab != null)
                    {
                        Register(data.Prefab.gameObject, new NetworkAnimalPoolHandler(_animalSpawner, data, _anchor, _poolLog));
                    }
                }
            }

            if (_bombSpawner != null && _bombData != null && _bombData.Prefab != null)
            {
                Register(_bombData.Prefab.gameObject, new NetworkBombPoolHandler(_bombSpawner, _anchor, _poolLog));
            }

            if (!_anchor.IsServer)
            {
                return;
            }

            if (_animalSpawner != null)
            {
                _animalSpawner.SetNetworkHook(this);
            }

            if (_bombSpawner != null)
            {
                _bombSpawner.SetNetworkHook(this);
            }

            _hooked = true;
        }

        private void OnDestroy()
        {
            if (_hooked)
            {
                _hooked = false;
                if (_animalSpawner != null)
                {
                    _animalSpawner.SetNetworkHook(null);
                }

                if (_bombSpawner != null)
                {
                    _bombSpawner.SetNetworkHook(null);
                }
            }

            // The NetworkRoot outlives this scene: never leave handlers pointing at destroyed pools.
            if (_manager != null && _manager.PrefabHandler != null)
            {
                for (int i = 0; i < _registeredCount; i++)
                {
                    if (_registeredPrefabs[i] != null)
                    {
                        _manager.PrefabHandler.RemoveHandler(_registeredPrefabs[i]);
                    }
                }
            }

            _registeredCount = 0;
        }

        /// <summary>
        /// The lifecycle log only exists when <c>NetworkSettings.LogPoolLifecycle</c> is on (read from the prefabs' own
        /// <see cref="NetworkCarriable.Settings"/>): off, every log call is a null check. Its baselines are the prewarm
        /// sizes, so the first spawn that has to instantiate past them is already reported as growth.
        /// </summary>
        private NetworkPoolLog CreatePoolLog()
        {
            if (!IsPoolLogEnabled())
            {
                return null;
            }

            int animalPrewarm = 0;
            AnimalSpawnEntry[] entries = _animalSettings != null ? _animalSettings.Entries : null;
            if (entries != null)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    if (entries[i].Data != null && entries[i].Data.Prefab != null)
                    {
                        animalPrewarm += _animalSettings.PrewarmPerType;
                    }
                }
            }

            int bombPrewarm = _bombSpawner != null ? _bombSpawner.PrewarmCount : 0;
            return new NetworkPoolLog(this, animalPrewarm, bombPrewarm);
        }

        private bool IsPoolLogEnabled()
        {
            AnimalSpawnEntry[] entries = _animalSettings != null ? _animalSettings.Entries : null;
            if (entries != null)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    AnimalData data = entries[i].Data;
                    if (data != null && data.Prefab != null && LogsPoolLifecycle(data.Prefab.gameObject))
                    {
                        return true;
                    }
                }
            }

            return _bombData != null && _bombData.Prefab != null && LogsPoolLifecycle(_bombData.Prefab.gameObject);
        }

        private static bool LogsPoolLifecycle(GameObject prefab) =>
            prefab.TryGetComponent(out NetworkCarriable carriable) && carriable.Settings != null && carriable.Settings.LogPoolLifecycle;

        private void Register(GameObject prefab, INetworkPrefabInstanceHandler handler)
        {
            if (!prefab.TryGetComponent(out NetworkObject _))
            {
                Debug.LogError($"[{nameof(NetworkSpawnRegistry)}] Prefab '{prefab.name}' has no NetworkObject; it cannot be replicated.", this);
                return;
            }

            // A handler left by an earlier load of this scene (same NetworkRoot) is replaced.
            _manager.PrefabHandler.RemoveHandler(prefab);
            if (_manager.PrefabHandler.AddHandler(prefab, handler) && _registeredCount < _registeredPrefabs.Length)
            {
                _registeredPrefabs[_registeredCount++] = prefab;
            }
        }

        #region Server hooks

        void IAnimalNetworkHook.OnSpawned(Animal animal)
        {
            if (!animal.TryGetComponent(out NetworkObject networkObject) || !animal.TryGetComponent(out NetworkAnimal networkAnimal))
            {
                Debug.LogError($"[{nameof(NetworkSpawnRegistry)}] Animal '{animal.name}' needs NetworkObject and NetworkAnimal to be replicated.", animal);
                return;
            }

            if (networkObject.IsSpawned || !CanSpawn)
            {
                return;
            }

            networkAnimal.Bind(_anchor);
            networkAnimal.ServerPrepareForSpawn();
            networkObject.Spawn(true);
            _poolLog?.Spawned(networkAnimal, networkAnimal.SpawnCount);
        }

        bool IAnimalNetworkHook.TryDespawn(Animal animal)
        {
            if (!animal.TryGetComponent(out NetworkObject networkObject) || !networkObject.IsSpawned)
            {
                return false;
            }

            if (animal.TryGetComponent(out NetworkAnimal networkAnimal))
            {
                networkAnimal.ServerDetach();
            }

            networkObject.Despawn(false);
            if (_animalSpawner == null || !_animalSpawner.TryGetPool(animal.Data, out AnimalTypePool pool))
            {
                Debug.LogWarning($"[{nameof(NetworkSpawnRegistry)}] No pool for '{animal.name}' in the registry's spawner; the animal returns itself to its own pool.", animal);
                return false;
            }

            PooledParenting.BeginRelease(networkObject, out bool sync);
            pool.Release(animal);
            PooledParenting.EndRelease(networkObject, sync);
            _poolLog?.Despawned(networkAnimal);
            return true;
        }

        void IBombNetworkHook.OnSpawned(Bomb bomb)
        {
            if (!bomb.TryGetComponent(out NetworkObject networkObject) || !bomb.TryGetComponent(out NetworkBomb networkBomb))
            {
                Debug.LogError($"[{nameof(NetworkSpawnRegistry)}] Bomb '{bomb.name}' needs NetworkObject and NetworkBomb to be replicated.", bomb);
                return;
            }

            if (networkObject.IsSpawned || !CanSpawn)
            {
                return;
            }

            networkBomb.Bind(_anchor);
            networkBomb.ServerPrepareForSpawn();
            networkObject.Spawn(true);
            _poolLog?.Spawned(networkBomb, networkBomb.SpawnCount);
        }

        bool IBombNetworkHook.TryDespawn(Bomb bomb)
        {
            if (!bomb.TryGetComponent(out NetworkObject networkObject) || !networkObject.IsSpawned)
            {
                return false;
            }

            if (bomb.TryGetComponent(out NetworkBomb networkBomb))
            {
                networkBomb.ServerDetach();
            }

            networkObject.Despawn(false);
            if (_bombSpawner == null)
            {
                return false;
            }

            PooledParenting.BeginRelease(networkObject, out bool sync);
            _bombSpawner.ReleaseToPool(bomb);
            PooledParenting.EndRelease(networkObject, sync);
            _poolLog?.Despawned(networkBomb);
            return true;
        }

        private bool CanSpawn => _manager != null && _manager.IsListening && _manager.IsServer && !_manager.ShutdownInProgress;

        #endregion

        private void OnValidate()
        {
            if (_anchor == null)
            {
                Debug.LogWarning($"[{nameof(NetworkSpawnRegistry)}] Missing NetworkRuntimeAnchor on '{name}'.", this);
            }

            if (_animalSettings == null || _animalSpawner == null)
            {
                Debug.LogWarning($"[{nameof(NetworkSpawnRegistry)}] Missing AnimalSpawnSettings or AnimalSpawner on '{name}'; animals are not replicated.", this);
            }

            if (_bombSpawner != null && _bombData == null)
            {
                Debug.LogWarning($"[{nameof(NetworkSpawnRegistry)}] Bomb spawner set without BombData on '{name}'; bombs are not replicated.", this);
            }
        }
    }
}
