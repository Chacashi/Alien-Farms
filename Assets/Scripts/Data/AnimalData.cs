using AlienFarms.Animals;
using UnityEngine;

namespace AlienFarms.Data
{
    /// <summary>Tuning and prefab for one animal type.</summary>
    [CreateAssetMenu(fileName = "AnimalData", menuName = "Alien Farms/Animals/Animal Data")]
    public class AnimalData : ScriptableObject
    {
        [Header("Prefab")]
        [SerializeField, Tooltip("Pooled prefab spawned for this type (root must have an Animal component).")]
        private Animal _prefab;

        [Header("Physics")]
        [SerializeField, Min(0.01f), Tooltip("Rigidbody mass applied on initialization (kg).")]
        private float _mass = 1f;

        [Header("Locomotion")]
        [SerializeField, Min(0f), Tooltip("Speed while wandering (m/s).")]
        private float _wanderSpeed = 1f;

        [SerializeField, Min(0f), Tooltip("Speed while fleeing from a player (m/s).")]
        private float _fleeSpeed = 4f;

        [SerializeField, Min(0f), Tooltip("Horizontal acceleration towards the desired velocity (m/s^2).")]
        private float _acceleration = 10f;

        [SerializeField, Min(0f), Tooltip("How fast the visual turns towards its velocity (deg/s).")]
        private float _turnSpeed = 360f;

        [Header("Perception")]
        [SerializeField, Min(0f), Tooltip("A player closer than this makes the animal flee (m).")]
        private float _detectionRadius = 4f;

        [SerializeField, Min(0f), Tooltip("Fleeing stops once no player is within this radius (m). Must be >= detection radius.")]
        private float _fleeExitRadius = 6f;

        [SerializeField, Min(0f), Tooltip("Minimum time spent fleeing once triggered (s).")]
        private float _minFleeTime = 1f;

        [SerializeField, Min(0.02f), Tooltip("Seconds between player detection queries.")]
        private float _detectionInterval = 0.2f;

        [Header("Wander")]
        [SerializeField, Min(0f), Tooltip("Max distance of each wander target from the current position (m).")]
        private float _wanderRadius = 5f;

        [SerializeField, Tooltip("Random idle time between wander legs (s): x = min, y = max.")]
        private Vector2 _idleTimeRange = new Vector2(0.5f, 2f);

        [SerializeField, Min(0.1f), Tooltip("A wander leg is aborted after this time, to avoid getting stuck (s).")]
        private float _maxWanderLegTime = 4f;

        [SerializeField, Min(0f), Tooltip("Distance kept from the Animal Zone edges (m).")]
        private float _confinementMargin = 1.5f;

        [Header("Carry")]
        [SerializeField, Range(0.3f, 1f), Tooltip("Multiplier applied to the holder's walk speed.")]
        private float _carrySpeedMultiplier = 0.9f;

        [SerializeField, Tooltip("Offset from the hold point, in hold point space.")]
        private Vector3 _holdOffset = new Vector3(0f, 0.3f, 0f);

        [SerializeField, Min(0f), Tooltip("After being dropped the animal cannot be grabbed for this long (s).")]
        private float _grabCooldown = 0.25f;

        [Header("Throw")]
        [SerializeField, Tooltip("Optional throw tuning for this type; empty uses the thrower's default.")]
        private ThrowSettings _throwSettings;

        [SerializeField, Min(0.5f), Tooltip("Safety timeout: an airborne animal is forced to land after this long (s).")]
        private float _maxAirborneTime = 4f;

        [Header("Pen")]
        [SerializeField, Min(0f), Tooltip("Distance kept from the pen edges while captured (m).")]
        private float _penConfinementMargin = 0.5f;

        public Animal Prefab => _prefab;
        public float Mass => _mass;
        public float WanderSpeed => _wanderSpeed;
        public float FleeSpeed => _fleeSpeed;
        public float Acceleration => _acceleration;
        public float TurnSpeed => _turnSpeed;
        public float DetectionRadius => _detectionRadius;
        public float FleeExitRadius => _fleeExitRadius;
        public float MinFleeTime => _minFleeTime;
        public float DetectionInterval => _detectionInterval;
        public float WanderRadius => _wanderRadius;
        public Vector2 IdleTimeRange => _idleTimeRange;
        public float MaxWanderLegTime => _maxWanderLegTime;
        public float ConfinementMargin => _confinementMargin;
        public float CarrySpeedMultiplier => _carrySpeedMultiplier;
        public Vector3 HoldOffset => _holdOffset;
        public float GrabCooldown => _grabCooldown;
        public ThrowSettings ThrowSettings => _throwSettings;
        public float MaxAirborneTime => _maxAirborneTime;
        public float PenConfinementMargin => _penConfinementMargin;

        protected virtual void OnValidate()
        {
            if (_fleeExitRadius < _detectionRadius)
            {
                _fleeExitRadius = _detectionRadius;
            }

            if (_fleeSpeed < _wanderSpeed)
            {
                _fleeSpeed = _wanderSpeed;
            }

            _idleTimeRange.x = Mathf.Max(0f, _idleTimeRange.x);
            if (_idleTimeRange.y < _idleTimeRange.x)
            {
                _idleTimeRange.y = _idleTimeRange.x;
            }
        }
    }
}
