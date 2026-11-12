using System.Collections.Generic;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Zones;
using UnityEngine;

namespace AlienFarms.Animals
{
    /// <summary>
    /// Scene composition root for animals: owns one pool per type, spawns the initial population around
    /// a scene-placed center and refills it over time while the free population is below target.
    /// As a <see cref="SimulationRole.Replica"/> it only keeps the pools (for replicated animals) and never populates.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AnimalSpawner : MonoBehaviour, ISimulationRoleTarget
    {
        [Header("Config")]
        [SerializeField, Tooltip("Population, types and placement rules.")]
        private AnimalSpawnSettings _settings;

        [Header("References")]
        [SerializeField, Tooltip("Center of the spawn circle (placed in the scene, inside the Animal Zone).")]
        private Transform _spawnCenter;

        [SerializeField, Tooltip("Zone whose bounds confine animals.")]
        private AnimalZone _animalZone;

        [SerializeField, Tooltip("Player zones registry (on the 'Zones' root); tells landing animals which pen they are in.")]
        private PlayerZoneRegistry _playerZones;

        [SerializeField, Tooltip("Parent for pooled animal instances (keeps the hierarchy tidy).")]
        private Transform _animalsContainer;

        private AnimalTypePool[] _pools;
        private List<Animal> _active;
        private Bounds _bounds;
        private int _totalWeight;
        private int _blockingMask;
        private float _respawnTimer;
        private bool _warnedCap;
        private SimulationRole _role = SimulationRole.Authority;
        private IAnimalNetworkHook _networkHook;

        /// <summary>Animals currently in play (including carried and captured ones).</summary>
        public int ActiveCount => _active != null ? _active.Count : 0;

        public SimulationRole Role => _role;

        /// <summary>Number of animal types (valid type indices are 0 .. TypeCount - 1; 0 before Awake).</summary>
        public int TypeCount => _pools != null ? _pools.Length : 0;

        private void Awake()
        {
            if (_settings == null || _spawnCenter == null || _animalZone == null)
            {
                Debug.LogError($"[{nameof(AnimalSpawner)}] Missing settings, spawn center or animal zone on '{name}'; spawner disabled.", this);
                enabled = false;
                return;
            }

            if (_playerZones == null)
            {
                Debug.LogError($"[{nameof(AnimalSpawner)}] Missing PlayerZoneRegistry on '{name}'; animals will never be captured.", this);
            }

            _blockingMask = LayerMask.GetMask(LayerNames.Player, LayerNames.Animal, LayerNames.Environment, LayerNames.Bomb);
            _bounds = _animalZone.Bounds;
            _active = new List<Animal>(_settings.PoolHardCap);

            AnimalSpawnEntry[] entries = _settings.Entries;
            // Unity null check before boxing to the interface (a destroyed registry must read as null).
            IZoneResolver zones = _playerZones != null ? _playerZones : null;
            _pools = new AnimalTypePool[entries.Length];
            _totalWeight = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                AnimalData data = entries[i].Data;
                if (data == null || data.Prefab == null)
                {
                    Debug.LogError($"[{nameof(AnimalSpawner)}] Spawn entry {i} has no AnimalData or prefab; it will be skipped.", this);
                    continue;
                }

                _pools[i] = new AnimalTypePool(
                    data, _animalsContainer, _bounds, zones, _settings.KillHeight, _settings.PrewarmPerType, _settings.PoolHardCap);
                _pools[i].SetNetworkHook(_networkHook);
                _totalWeight += Mathf.Max(1, entries[i].Weight);
            }
        }

        private void Start()
        {
            for (int i = 0; i < _pools.Length; i++)
            {
                _pools[i]?.Prewarm(_settings.PrewarmPerType);
            }

            // ResetPopulation may already have populated the zone (e.g. a match reset before our Start).
            if (_role == SimulationRole.Authority && _active.Count == 0)
            {
                SpawnInitial();
            }
        }

        private void Update()
        {
            if (_role == SimulationRole.Replica)
            {
                return;
            }

            _respawnTimer -= Time.deltaTime;
            if (_respawnTimer > 0f)
            {
                return;
            }

            _respawnTimer = _settings.RespawnInterval;

            // Free = in play and not captured (carried and airborne included). Captured animals never block the refill,
            // so the Animal Zone keeps its population however many get penned.
            int free = PruneAndCountFree();
            if (free >= _settings.TargetFreePopulation || free >= _settings.MaxActiveAnimals)
            {
                return;
            }

            // Absolute safety cap including captured animals. Below it, pools may instantiate past the prewarm:
            // accepted as a rare case (only after many captures in a single round), never per frame.
            if (_active.Count >= _settings.PoolHardCap)
            {
                if (!_warnedCap)
                {
                    _warnedCap = true;
                    Debug.LogWarning($"[{nameof(AnimalSpawner)}] Reached the hard cap of {_settings.PoolHardCap} animals; refill paused.", this);
                }

                return;
            }

            TrySpawnOne();
        }

        /// <summary>Replica: never populates or refills (animals come from the network). Set it before Start.</summary>
        public void SetSimulationRole(SimulationRole role)
        {
            if (role == SimulationRole.Replica && _role == SimulationRole.Authority && ActiveCount > 0)
            {
                Debug.LogWarning($"[{nameof(AnimalSpawner)}] '{name}' switched to Replica with {ActiveCount} animals already spawned locally; set the role before Start.", this);
            }

            _role = role;
        }

        /// <summary>Network hook every pool reports the authority's spawns and despawns to (null = local pooling).</summary>
        public void SetNetworkHook(IAnimalNetworkHook hook)
        {
            _networkHook = hook;
            if (_pools == null)
            {
                return;
            }

            for (int i = 0; i < _pools.Length; i++)
            {
                _pools[i]?.SetNetworkHook(hook);
            }
        }

        /// <summary>
        /// Stable index of an animal type: its position in <see cref="AnimalSpawnSettings.Entries"/> (the order both
        /// server and clients load from the same asset). False for a type this spawner does not handle.
        /// </summary>
        public bool TryGetTypeIndex(AnimalData data, out int typeIndex)
        {
            if (_pools != null && data != null)
            {
                for (int i = 0; i < _pools.Length; i++)
                {
                    if (_pools[i] != null && _pools[i].Data == data)
                    {
                        typeIndex = i;
                        return true;
                    }
                }
            }

            typeIndex = -1;
            return false;
        }

        /// <summary>Pool of the given animal type, if this spawner has one.</summary>
        public bool TryGetPool(AnimalData data, out AnimalTypePool pool)
        {
            if (_pools != null && data != null)
            {
                for (int i = 0; i < _pools.Length; i++)
                {
                    AnimalTypePool candidate = _pools[i];
                    if (candidate != null && candidate.Data == data)
                    {
                        pool = candidate;
                        return true;
                    }
                }
            }

            pool = null;
            return false;
        }

        /// <summary>Removes an animal from play and returns it to its pool.</summary>
        public void Despawn(Animal animal)
        {
            if (animal == null || _active == null)
            {
                return;
            }

            animal.Despawn();
            int index = _active.IndexOf(animal);
            if (index >= 0)
            {
                RemoveActiveAt(index);
            }
        }

        /// <summary>Despawns every animal and spawns the initial population again.</summary>
        public void ResetPopulation()
        {
            if (!enabled || _active == null || _role == SimulationRole.Replica)
            {
                return;
            }

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                _active[i].Despawn();
            }

            _active.Clear();
            _warnedCap = false;
            SpawnInitial();
        }

        private void SpawnInitial()
        {
            int count = Mathf.Min(_settings.InitialCount, Mathf.Min(_settings.MaxActiveAnimals, _settings.PoolHardCap));
            for (int i = 0; i < count; i++)
            {
                TrySpawnOne();
            }

            _respawnTimer = _settings.RespawnInterval;
        }

        private bool TrySpawnOne()
        {
            int typeIndex = PickTypeIndex();
            if (typeIndex < 0)
            {
                return false;
            }

            AnimalTypePool pool = _pools[typeIndex];
            float margin = pool.Data.ConfinementMargin;
            float clearance = _settings.SpawnClearance;
            Vector3 center = _spawnCenter.position;

            for (int attempt = 0; attempt < _settings.MaxPlacementAttempts; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle * _settings.SpawnRadius;
                Vector3 point = new Vector3(center.x + offset.x, center.y, center.z + offset.y);
                point = AnimalSteering.ClampInside(point, _bounds, margin);

                // Sphere sits on the ground (center raised by its radius) so the floor is never hit.
                Vector3 probe = point + Vector3.up * clearance;
                if (Physics.CheckSphere(probe, clearance, _blockingMask, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                Animal animal = pool.Get();
                // A pooled instance may have served as a replica before.
                animal.SetSimulationRole(SimulationRole.Authority);
                animal.Spawn(point, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                _active.Add(animal);
                return true;
            }

            // No free spot this tick; try again next interval.
            return false;
        }

        private int PickTypeIndex()
        {
            if (_totalWeight <= 0)
            {
                return -1;
            }

            AnimalSpawnEntry[] entries = _settings.Entries;
            int roll = Random.Range(0, _totalWeight);
            for (int i = 0; i < _pools.Length; i++)
            {
                if (_pools[i] == null)
                {
                    continue;
                }

                roll -= Mathf.Max(1, entries[i].Weight);
                if (roll < 0)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Drops animals that were despawned elsewhere and counts the free ones.</summary>
        private int PruneAndCountFree()
        {
            int free = 0;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Animal animal = _active[i];
                if (animal == null || animal.State == AnimalState.Inactive)
                {
                    RemoveActiveAt(i);
                    continue;
                }

                if (animal.IsFree)
                {
                    free++;
                }
            }

            return free;
        }

        private void RemoveActiveAt(int index)
        {
            // Swap-remove: order is irrelevant and this avoids shifting the list.
            int last = _active.Count - 1;
            _active[index] = _active[last];
            _active.RemoveAt(last);
        }

        private void OnValidate()
        {
            if (_settings == null)
            {
                Debug.LogWarning($"[{nameof(AnimalSpawner)}] Missing AnimalSpawnSettings on '{name}'.", this);
            }

            if (_spawnCenter == null)
            {
                Debug.LogWarning($"[{nameof(AnimalSpawner)}] Missing spawn center on '{name}'.", this);
            }

            if (_animalZone == null)
            {
                Debug.LogWarning($"[{nameof(AnimalSpawner)}] Missing AnimalZone on '{name}'.", this);
            }

            if (_playerZones == null)
            {
                Debug.LogWarning($"[{nameof(AnimalSpawner)}] Missing PlayerZoneRegistry on '{name}'; animals will never be captured.", this);
            }

            if (_animalsContainer == null)
            {
                Debug.LogWarning($"[{nameof(AnimalSpawner)}] Missing animals container on '{name}'; animals will spawn at the scene root.", this);
            }

            if (_settings != null && _spawnCenter != null && _animalZone != null)
            {
                Bounds bounds = _animalZone.Bounds;
                Vector3 c = _spawnCenter.position;
                float r = _settings.SpawnRadius;
                if (c.x - r < bounds.min.x || c.x + r > bounds.max.x || c.z - r < bounds.min.z || c.z + r > bounds.max.z)
                {
                    Debug.LogWarning($"[{nameof(AnimalSpawner)}] Spawn circle on '{name}' extends outside the Animal Zone; points will be clamped.", this);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_spawnCenter == null || _settings == null)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            Vector3 c = _spawnCenter.position;
            const int segments = 32;
            float r = _settings.SpawnRadius;
            Vector3 previous = c + new Vector3(r, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * (2f * Mathf.PI / segments);
                Vector3 next = c + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }

            if (_animalZone != null)
            {
                Bounds bounds = _animalZone.Bounds;
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
        }
    }
}
