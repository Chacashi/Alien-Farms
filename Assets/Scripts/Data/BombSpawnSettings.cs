using UnityEngine;

namespace AlienFarms.Data
{
    /// <summary>When and where stun bombs appear in the Animal Zone.</summary>
    [CreateAssetMenu(fileName = "BombSpawnSettings", menuName = "Alien Farms/Items/Bomb Spawn Settings")]
    public sealed class BombSpawnSettings : ScriptableObject
    {
        [Header("Timing")]
        [SerializeField, Range(1, 2), Tooltip("Max bombs in play at once (inert, carried or armed).")]
        private int _maxActive = 1;

        [SerializeField, Min(0f), Tooltip("Seconds after the round starts before the first bomb appears.")]
        private float _initialDelay = 12f;

        [SerializeField, Tooltip("Random wait (min, max) with a free slot before the next bomb appears (s).")]
        private Vector2 _respawnIntervalRange = new Vector2(15f, 25f);

        [SerializeField, Min(0.1f), Tooltip("Retry delay when no valid spot was found (s).")]
        private float _retryDelay = 1f;

        [Header("Placement")]
        [SerializeField, Min(0f), Tooltip("Bombs never spawn closer than this to the Animal Zone edges (m).")]
        private float _edgeMargin = 3f;

        [SerializeField, Min(0f), Tooltip("Spawn spots must be free of obstacles, animals and bombs within this radius (m).")]
        private float _spawnClearance = 0.8f;

        [SerializeField, Min(0f), Tooltip("Spawn spots must be free of players within this radius (m).")]
        private float _playerClearance = 3f;

        [SerializeField, Min(1), Tooltip("Random placement attempts per spawn before retrying later.")]
        private int _maxPlacementAttempts = 12;

        [SerializeField, Min(0f), Tooltip("Height above the floor the bomb is dropped from (m).")]
        private float _spawnDropHeight = 0.3f;

        [Header("Pool & Safety")]
        [SerializeField, Min(1), Tooltip("Max pooled bomb instances (must be >= max active).")]
        private int _poolMaxSize = 4;

        [SerializeField, Tooltip("World height below which a bomb is considered lost and despawned (m).")]
        private float _killHeight = -10f;

        public int MaxActive => _maxActive;
        public float InitialDelay => _initialDelay;
        public Vector2 RespawnIntervalRange => _respawnIntervalRange;
        public float RetryDelay => _retryDelay;
        public float EdgeMargin => _edgeMargin;
        public float SpawnClearance => _spawnClearance;
        public float PlayerClearance => _playerClearance;
        public int MaxPlacementAttempts => _maxPlacementAttempts;
        public float SpawnDropHeight => _spawnDropHeight;
        public int PoolMaxSize => _poolMaxSize;
        public float KillHeight => _killHeight;

        private void OnValidate()
        {
            if (_respawnIntervalRange.x < 0f)
            {
                _respawnIntervalRange.x = 0f;
            }

            if (_respawnIntervalRange.y < _respawnIntervalRange.x)
            {
                _respawnIntervalRange.y = _respawnIntervalRange.x;
            }

            if (_poolMaxSize < _maxActive)
            {
                _poolMaxSize = _maxActive;
            }
        }
    }
}
