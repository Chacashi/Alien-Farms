using UnityEngine;

namespace AlienFarms.Data
{
    /// <summary>Population and placement rules for the animal spawner.</summary>
    [CreateAssetMenu(fileName = "AnimalSpawnSettings", menuName = "Alien Farms/Animals/Animal Spawn Settings")]
    public sealed class AnimalSpawnSettings : ScriptableObject
    {
        [Header("Types")]
        [SerializeField, Tooltip("Animal types and their relative weights.")]
        private AnimalSpawnEntry[] _entries = new AnimalSpawnEntry[0];

        [Header("Population")]
        [SerializeField, Min(0), Tooltip("Animals spawned when the spawner starts.")]
        private int _initialCount = 12;

        [SerializeField, Min(0), Tooltip("The spawner refills until this many animals are free (not captured).")]
        private int _targetFreePopulation = 12;

        [SerializeField, Min(0.1f), Tooltip("Seconds between refill attempts (one animal per attempt).")]
        private float _respawnInterval = 3f;

        [SerializeField, Min(1), Tooltip("Hard cap of animals in the world (also the max size of each pool).")]
        private int _maxActiveAnimals = 60;

        [SerializeField, Min(0), Tooltip("Instances created per type at startup to avoid runtime instantiation.")]
        private int _prewarmPerType = 12;

        [Header("Placement")]
        [SerializeField, Min(0f), Tooltip("Spawn points are picked inside this radius around the spawn center (m).")]
        private float _spawnRadius = 6f;

        [SerializeField, Min(0f), Tooltip("Spawn points must be free of players/animals/environment within this radius (m).")]
        private float _spawnClearance = 0.6f;

        [SerializeField, Min(1), Tooltip("Random placement attempts before skipping a spawn.")]
        private int _maxPlacementAttempts = 8;

        public AnimalSpawnEntry[] Entries => _entries;
        public int InitialCount => _initialCount;
        public int TargetFreePopulation => _targetFreePopulation;
        public float RespawnInterval => _respawnInterval;
        public int MaxActiveAnimals => _maxActiveAnimals;
        public int PrewarmPerType => _prewarmPerType;
        public float SpawnRadius => _spawnRadius;
        public float SpawnClearance => _spawnClearance;
        public int MaxPlacementAttempts => _maxPlacementAttempts;

        private void OnValidate()
        {
            if (_initialCount > _maxActiveAnimals)
            {
                _initialCount = _maxActiveAnimals;
            }

            if (_targetFreePopulation > _maxActiveAnimals)
            {
                _targetFreePopulation = _maxActiveAnimals;
            }

            if (_prewarmPerType > _maxActiveAnimals)
            {
                _prewarmPerType = _maxActiveAnimals;
            }
        }
    }
}
