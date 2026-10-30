using System;
using System.Collections.Generic;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Utilities;
using AlienFarms.Zones;
using UnityEngine;
using UnityEngine.Pool;

namespace AlienFarms.Items
{
    /// <summary>
    /// Scene composition root for stun bombs: owns the bomb pool and drops bombs at random free spots of the Animal Zone
    /// while a round is playing (timer paused while the max is in play). Bombs never exist outside Playing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BombSpawner : MonoBehaviour
    {
        // Height above the floor used for the player clearance check (roughly chest height, m).
        private const float PlayerProbeHeight = 1f;

        // The floor raycast starts this far above the zone top (m).
        private const float RaycastHeadroom = 1f;

        [Header("Config")]
        [SerializeField, Tooltip("When, how many and where bombs appear.")]
        private BombSpawnSettings _settings;

        [SerializeField, Tooltip("Bomb definition (prefab, blast).")]
        private BombData _data;

        [Header("References")]
        [SerializeField, Tooltip("Zone whose area bombs spawn in.")]
        private AnimalZone _animalZone;

        [SerializeField, Tooltip("Parent for pooled bomb instances (keeps the hierarchy tidy).")]
        private Transform _bombsContainer;

        private readonly BombSpawnSchedule _schedule = new BombSpawnSchedule();
        private ObjectPool<Bomb> _pool;
        private List<Bomb> _active;
        private IRandomSource _rng;
        private int _floorMask;
        private int _groundLayer = -1;
        private int _blockingMask;
        private int _playerMask;

        /// <summary>Bombs currently in play (inert, carried or armed).</summary>
        public int ActiveCount => _active != null ? _active.Count : 0;

        private void Awake()
        {
            if (_settings == null || _data == null || _data.Prefab == null || _animalZone == null)
            {
                Debug.LogError($"[{nameof(BombSpawner)}] Missing settings, bomb data (or its prefab) or animal zone on '{name}'; spawner disabled.", this);
                enabled = false;
                return;
            }

            if (LayerMask.NameToLayer(LayerNames.Bomb) < 0)
            {
                Debug.LogError($"[{nameof(BombSpawner)}] Layer '{LayerNames.Bomb}' does not exist; bombs will not collide or block spawns correctly.", this);
            }

            _floorMask = LayerMask.GetMask(LayerNames.Ground, LayerNames.Environment);
            _groundLayer = LayerMask.NameToLayer(LayerNames.Ground);
            _blockingMask = LayerMask.GetMask(
                LayerNames.Environment, LayerNames.Animal, LayerNames.CapturedAnimal, LayerNames.Bomb);
            _playerMask = LayerMask.GetMask(LayerNames.Player);

            int maxActive = _settings.MaxActive;
            int maxSize = Mathf.Max(maxActive, _settings.PoolMaxSize);
            _active = new List<Bomb>(maxActive);
            _rng = new SystemRandomSource(Environment.TickCount);
            _pool = new ObjectPool<Bomb>(
                CreateBomb,
                null,
                OnReleaseBomb,
                OnDestroyBomb,
                Application.isEditor,
                Mathf.Clamp(maxActive, 1, maxSize),
                maxSize);
        }

        private void Start()
        {
            Prewarm(_settings.MaxActive);
        }

        private void OnEnable()
        {
            GameEvents.OnMatchPhaseChanged += HandleMatchPhaseChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnMatchPhaseChanged -= HandleMatchPhaseChanged;
        }

        private void Update()
        {
            if (!_schedule.Tick(Time.deltaTime, _active.Count, _settings.MaxActive))
            {
                return;
            }

            if (TrySpawnOne())
            {
                _schedule.OnSpawned(BombSpawnSchedule.NextInterval(_settings.RespawnIntervalRange, _rng));
            }
            else
            {
                _schedule.OnSpawnFailed(_settings.RetryDelay);
            }
        }

        /// <summary>Removes every bomb from play and stops spawning until the next round starts playing.</summary>
        public void ResetBombs()
        {
            DespawnAll();
            _schedule.Stop();
        }

        private void HandleMatchPhaseChanged(MatchPhase previous, MatchPhase current)
        {
            if (!enabled || _active == null)
            {
                return;
            }

            if (current == MatchPhase.Playing)
            {
                _schedule.Start(_settings.InitialDelay);
            }
            else if (previous == MatchPhase.Playing)
            {
                // No bombs on the results screen.
                DespawnAll();
                _schedule.Stop();
            }
        }

        private bool TrySpawnOne()
        {
            Bounds area = _animalZone.Bounds;
            float rayStart = area.max.y + RaycastHeadroom;
            float rayLength = rayStart - area.min.y + RaycastHeadroom;
            float clearance = _settings.SpawnClearance;

            for (int attempt = 0; attempt < _settings.MaxPlacementAttempts; attempt++)
            {
                Vector3 point = BombPlacement.RandomPointInArea(area, _settings.EdgeMargin, _rng);
                Vector3 origin = new Vector3(point.x, rayStart, point.z);
                if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayLength, _floorMask, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                // First hit on an obstacle top (Environment): never spawn on it.
                if (hit.collider.gameObject.layer != _groundLayer)
                {
                    continue;
                }

                Vector3 floor = hit.point;
                if (clearance > 0f
                    && Physics.CheckSphere(floor + Vector3.up * clearance, clearance, _blockingMask, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                if (_settings.PlayerClearance > 0f
                    && Physics.CheckSphere(floor + Vector3.up * PlayerProbeHeight, _settings.PlayerClearance, _playerMask, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                Bomb bomb = _pool.Get();
                _active.Add(bomb);
                bomb.Spawn(floor + Vector3.up * _settings.SpawnDropHeight);
                return true;
            }

            return false;
        }

        private void DespawnAll()
        {
            if (_active == null)
            {
                return;
            }

            // Each Despawn releases to the pool, whose callback removes the bomb from the list.
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (i >= _active.Count)
                {
                    continue;
                }

                Bomb bomb = _active[i];
                if (bomb == null)
                {
                    _active.RemoveAt(i);
                    continue;
                }

                bomb.Despawn();
            }
        }

        /// <summary>Creates instances up front so gameplay never instantiates. Only call at load time.</summary>
        private void Prewarm(int count)
        {
            int missing = count - _pool.CountAll;
            if (missing <= 0)
            {
                return;
            }

            // One-off temporary buffer at load time.
            Bomb[] buffer = new Bomb[missing];
            for (int i = 0; i < missing; i++)
            {
                buffer[i] = _pool.Get();
            }

            for (int i = 0; i < missing; i++)
            {
                _pool.Release(buffer[i]);
            }
        }

        private Bomb CreateBomb()
        {
            Bomb bomb = Instantiate(_data.Prefab, _bombsContainer);
            bomb.gameObject.SetActive(false);
            bomb.Initialize(_data, _pool, _settings.KillHeight);
            return bomb;
        }

        private void OnReleaseBomb(Bomb bomb)
        {
            _active.Remove(bomb);
            bomb.gameObject.SetActive(false);
        }

        private static void OnDestroyBomb(Bomb bomb)
        {
            if (bomb != null)
            {
                Destroy(bomb.gameObject);
            }
        }

        private void OnValidate()
        {
            if (_settings == null)
            {
                Debug.LogWarning($"[{nameof(BombSpawner)}] Missing BombSpawnSettings on '{name}'.", this);
            }

            if (_data == null)
            {
                Debug.LogWarning($"[{nameof(BombSpawner)}] Missing BombData on '{name}'.", this);
            }

            if (_animalZone == null)
            {
                Debug.LogWarning($"[{nameof(BombSpawner)}] Missing AnimalZone on '{name}'.", this);
            }

            if (_bombsContainer == null)
            {
                Debug.LogWarning($"[{nameof(BombSpawner)}] No bombs container on '{name}'; pooled bombs will sit at the scene root.", this);
            }
        }
    }
}
