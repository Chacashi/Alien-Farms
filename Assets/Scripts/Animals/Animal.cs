using AlienFarms.Core;
using AlienFarms.Data;
using UnityEngine;

namespace AlienFarms.Animals
{
    /// <summary>
    /// Pooled, Rigidbody-driven animal that wanders inside the Animal Zone, flees from nearby players
    /// and can be carried by a player. Derived types only change data and the flee direction.
    /// State flow: Wandering &lt;-&gt; Fleeing -&gt; Carried -&gt; (released) Wandering, or Captured when the capture
    /// referee accepts the drop (confined to the owner's pen until despawned).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public abstract class Animal : MonoBehaviour, IGrabbable
    {
        private const float ArrivalDistanceSqr = 0.3f * 0.3f;
        private const float MinVisualSpeedSqr = 0.2f * 0.2f;
        private const float Epsilon = 0.0001f;
        // Once cornered, the chosen edge direction is kept at least this long to avoid oscillating (s).
        private const float CornerEscapeHoldTime = 0.75f;

        // After a drop, steering waits this long so the drop velocity is not overridden immediately (s).
        private const float SteeringResumeDelay = 0.35f;

        // Extra inset when clamping a captured animal inside its pen (m).
        private const float CaptureClampPadding = 0.1f;

        // A captured animal pushed further than this outside its pen is snapped back (m).
        private const float CaptureEscapeTolerance = 0.25f;

        // Shared by all animals: queries run sequentially on the main thread and are consumed immediately.
        private static readonly Collider[] s_playerHits = new Collider[8];

        [Header("References")]
        [SerializeField, Tooltip("Rigidbody on this root (rotation frozen, Interpolate).")]
        private Rigidbody _rb;

        [SerializeField, Tooltip("Physics collider on this root; disabled while carried.")]
        private Collider _collider;

        [SerializeField, Tooltip("Child that holds the model and turns towards the velocity.")]
        private Transform _visual;

        private AnimalData _data;
        private Bounds _confinement;
        private Bounds _homeConfinement;
        private AnimalTypePool _pool;
        private AnimalState _state = AnimalState.Inactive;
        private PlayerId _holder = PlayerId.None;
        private PlayerId _owner = PlayerId.None;
        private int _freeLayer;
        private int _capturedLayer;
        private bool _layersCached;
        private Transform _holdPoint;
        private int _playerMask;

        private float _nextDetectionTime;
        private bool _isIdle;
        private float _idleUntil;
        private float _legEndTime;
        private float _minFleeEndTime;
        private Vector3 _wanderTarget;
        private Vector3 _threatPosition;
        private float _grabCooldownUntil;
        private float _steeringResumeTime;
        private CornerEscape _cornerEscape;

        public AnimalData Data => _data;
        public AnimalState State => _state;

        /// <summary>True while the animal is in play and not captured.</summary>
        public bool IsFree => _state != AnimalState.Inactive && _state != AnimalState.Captured;

        /// <summary>Player whose pen holds this animal, or <see cref="PlayerId.None"/> while free.</summary>
        public PlayerId Owner => _owner;

        public bool CanBeGrabbed => IsFree && _state != AnimalState.Carried && Time.time >= _grabCooldownUntil;
        public PlayerId Holder => _holder;
        public Vector3 Position => transform.position;
        public float CarrySpeedMultiplier => _data != null ? _data.CarrySpeedMultiplier : 1f;

        protected virtual void Awake()
        {
            if (_rb == null)
            {
                _rb = GetComponent<Rigidbody>();
            }

            if (_collider == null)
            {
                TryGetComponent(out _collider);
            }

            _playerMask = LayerMask.GetMask(LayerNames.Player);
            CacheLayers();
        }

        /// <summary>Called once by the pool right after instantiation.</summary>
        public void Initialize(AnimalData data, Bounds confinement, AnimalTypePool pool)
        {
            // The pool instantiates inactive-parented clones; make sure cached refs exist before use.
            if (_rb == null)
            {
                _rb = GetComponent<Rigidbody>();
            }

            CacheLayers();

            _data = data;
            _confinement = confinement;
            _homeConfinement = confinement;
            _pool = pool;
            _state = AnimalState.Inactive;

            if (_data != null)
            {
                _rb.mass = _data.Mass;
            }

            OnInitialized();
        }

        /// <summary>Places the animal in the world and activates it in the Wandering state.</summary>
        public void Spawn(Vector3 position, Quaternion rotation)
        {
            _rb.isKinematic = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.position = position;
            _rb.rotation = Quaternion.identity;
            transform.SetPositionAndRotation(position, Quaternion.identity);
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;

            if (_visual != null)
            {
                _visual.rotation = rotation;
            }

            if (_collider != null)
            {
                _collider.enabled = true;
            }

            // Pooled instances may come back from a pen: restore the free setup.
            gameObject.layer = _freeLayer;
            _confinement = _homeConfinement;
            _owner = PlayerId.None;
            _holder = PlayerId.None;
            _holdPoint = null;
            _grabCooldownUntil = 0f;
            _steeringResumeTime = 0f;
            _cornerEscape.Clear();
            _state = AnimalState.Wandering;

            float interval = _data != null ? _data.DetectionInterval : 0.2f;
            // Random phase spreads detection queries of many animals across frames.
            _nextDetectionTime = Time.time + Random.Range(0f, interval);
            EnterIdle();

            gameObject.SetActive(true);
        }

        /// <summary>Removes the animal from play, dropping it first if carried, and returns it to its pool.</summary>
        public void Despawn()
        {
            if (_state == AnimalState.Inactive)
            {
                return;
            }

            if (_state == AnimalState.Carried)
            {
                Release(transform.position, Vector3.zero, DropReason.Forced);
            }

            _state = AnimalState.Inactive;

            if (_pool != null)
            {
                _pool.Release(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public bool TryGrab(PlayerId holder, Transform holdPoint)
        {
            if (!CanBeGrabbed || holder == PlayerId.None || holdPoint == null || _holder != PlayerId.None)
            {
                return false;
            }

            // Clear velocity while still dynamic: writing velocity on a kinematic body is not supported.
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
            _rb.interpolation = RigidbodyInterpolation.None;

            if (_collider != null)
            {
                _collider.enabled = false;
            }

            if (_visual != null)
            {
                _visual.localRotation = Quaternion.identity;
            }

            _holder = holder;
            _holdPoint = holdPoint;
            _state = AnimalState.Carried;
            SnapToHoldPoint();

            GameEvents.RaiseAnimalGrabbed(holder, this);
            return true;
        }

        public void Release(Vector3 position, Vector3 velocity, DropReason reason)
        {
            if (_state != AnimalState.Carried)
            {
                return;
            }

            PlayerId previousHolder = _holder;

            // Keep the facing of the carried visual, but reset the root (only the visual rotates).
            Vector3 visualForward = _visual != null ? _visual.forward : Vector3.forward;
            visualForward.y = 0f;

            // Order matters: become dynamic BEFORE writing position/velocity.
            _rb.isKinematic = false;
            _rb.position = position;
            _rb.rotation = Quaternion.identity;
            transform.SetPositionAndRotation(position, Quaternion.identity);
            _rb.linearVelocity = velocity;
            _rb.angularVelocity = Vector3.zero;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;

            if (_collider != null)
            {
                _collider.enabled = true;
            }

            if (_visual != null && visualForward.sqrMagnitude > Epsilon)
            {
                _visual.rotation = Quaternion.LookRotation(visualForward, Vector3.up);
            }

            _holder = PlayerId.None;
            _holdPoint = null;
            _state = AnimalState.Wandering;
            _grabCooldownUntil = Time.time + (_data != null ? _data.GrabCooldown : 0f);
            _steeringResumeTime = Time.time + SteeringResumeDelay;
            _cornerEscape.Clear();
            EnterIdle();

            // MUST stay the last statement: listeners (CaptureReferee) may call TryCapture synchronously
            // and rely on the release state above being fully applied.
            GameEvents.RaiseAnimalDropped(previousHolder, this, reason);
        }

        /// <summary>
        /// Moves a free, just-released animal into <paramref name="owner"/>'s pen: it becomes Captured, switches to the
        /// CapturedAnimal layer, is clamped inside <paramref name="pen"/> and wanders there without fleeing.
        /// Returns false unless the animal is Wandering or Fleeing.
        /// </summary>
        public bool TryCapture(PlayerId owner, Bounds pen)
        {
            if (owner == PlayerId.None || (_state != AnimalState.Wandering && _state != AnimalState.Fleeing))
            {
                return false;
            }

            _state = AnimalState.Captured;
            _owner = owner;
            _confinement = pen;
            // The physics collider lives on this root (the visual has none), so the root layer is enough.
            gameObject.layer = _capturedLayer;

            ClampInsidePen();

            _cornerEscape.Clear();
            EnterIdle();
            _steeringResumeTime = 0f;
            return true;
        }

        /// <summary>Resolves the free (prefab) and captured layers once; safe to call before Awake.</summary>
        private void CacheLayers()
        {
            if (_layersCached)
            {
                return;
            }

            _layersCached = true;
            _freeLayer = gameObject.layer;
            _capturedLayer = LayerMask.NameToLayer(LayerNames.CapturedAnimal);
            if (_capturedLayer < 0)
            {
                Debug.LogError($"[{nameof(Animal)}] Layer '{LayerNames.CapturedAnimal}' is missing; captured animals keep their layer.", this);
                _capturedLayer = _freeLayer;
            }
        }

        /// <summary>Direction to flee in, given the planar unit vector pointing away from the threat.</summary>
        protected abstract Vector3 ComputeFleeDirection(Vector3 awayFromThreat);

        /// <summary>Hook called after <see cref="Initialize"/> (data is available).</summary>
        protected virtual void OnInitialized()
        {
        }

        private void FixedUpdate()
        {
            if (_data == null || _state == AnimalState.Inactive || _state == AnimalState.Carried)
            {
                return;
            }

            float now = Time.time;
            Vector3 position = _rb.position;

            if (_state == AnimalState.Captured)
            {
                // Safety net: something (a crush against the barrier, a shove) pushed it out of its pen.
                if (IsOutsidePen(position))
                {
                    ClampInsidePen();
                    position = _rb.position;
                }

                ApplyDesiredVelocity(ComputeWanderVelocity(position, now));
                return;
            }

            if (now >= _nextDetectionTime)
            {
                _nextDetectionTime = now + _data.DetectionInterval;
                UpdateThreat(position, now);
            }

            // Just dropped: let physics (drop velocity, gravity) act before steering takes over again.
            if (now < _steeringResumeTime)
            {
                return;
            }

            Vector3 desired = _state == AnimalState.Fleeing
                ? ComputeFleeVelocity(position)
                : ComputeWanderVelocity(position, now);

            ApplyDesiredVelocity(desired);
        }

        private void ApplyDesiredVelocity(Vector3 desired)
        {
            Vector3 velocity = _rb.linearVelocity;
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            horizontal = Vector3.MoveTowards(horizontal, desired, _data.Acceleration * Time.fixedDeltaTime);
            _rb.linearVelocity = new Vector3(horizontal.x, velocity.y, horizontal.z);
        }

        /// <summary>Clamps the body inside the pen (inset by its half size) and zeroes horizontal velocity (Y kept).</summary>
        private void ClampInsidePen()
        {
            float halfSize = PlanarHalfSize();
            Vector3 clamped = AnimalSteering.ClampInside(_rb.position, _confinement, halfSize + CaptureClampPadding);
            _rb.position = clamped;
            transform.position = clamped;

            Vector3 velocity = _rb.linearVelocity;
            _rb.linearVelocity = new Vector3(0f, velocity.y, 0f);
        }

        private bool IsOutsidePen(Vector3 position)
        {
            Vector3 min = _confinement.min;
            Vector3 max = _confinement.max;
            return position.x < min.x - CaptureEscapeTolerance || position.x > max.x + CaptureEscapeTolerance
                || position.z < min.z - CaptureEscapeTolerance || position.z > max.z + CaptureEscapeTolerance;
        }

        private float PlanarHalfSize()
        {
            if (_collider == null || !_collider.enabled)
            {
                return 0f;
            }

            Vector3 extents = _collider.bounds.extents;
            return Mathf.Max(extents.x, extents.z);
        }

        private void Update()
        {
            if (_visual == null || _data == null || _state == AnimalState.Inactive || _state == AnimalState.Carried)
            {
                return;
            }

            Vector3 velocity = _rb.linearVelocity;
            velocity.y = 0f;
            if (velocity.sqrMagnitude < MinVisualSpeedSqr)
            {
                return;
            }

            Quaternion target = Quaternion.LookRotation(velocity, Vector3.up);
            _visual.rotation = Quaternion.RotateTowards(_visual.rotation, target, _data.TurnSpeed * Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (_state != AnimalState.Carried)
            {
                return;
            }

            // Holder destroyed without dropping us: never stay frozen in the air.
            if (_holdPoint == null)
            {
                Release(transform.position, Vector3.zero, DropReason.Forced);
                return;
            }

            SnapToHoldPoint();
        }

        private void SnapToHoldPoint()
        {
            if (_holdPoint == null)
            {
                return;
            }

            Vector3 offset = _data != null ? _data.HoldOffset : Vector3.zero;
            transform.SetPositionAndRotation(_holdPoint.TransformPoint(offset), _holdPoint.rotation);
        }

        private void UpdateThreat(Vector3 position, float now)
        {
            float radius = _state == AnimalState.Fleeing ? _data.FleeExitRadius : _data.DetectionRadius;
            int count = Physics.OverlapSphereNonAlloc(position, radius, s_playerHits, _playerMask, QueryTriggerInteraction.Ignore);

            bool found = false;
            float bestSqr = float.MaxValue;
            Vector3 best = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                Vector3 hitPosition = s_playerHits[i].transform.position;
                float sqr = (hitPosition - position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = hitPosition;
                    found = true;
                }

                s_playerHits[i] = null;
            }

            if (found)
            {
                _threatPosition = best;
                if (_state != AnimalState.Fleeing)
                {
                    _state = AnimalState.Fleeing;
                    _minFleeEndTime = now + _data.MinFleeTime;
                }

                return;
            }

            if (_state == AnimalState.Fleeing && now >= _minFleeEndTime)
            {
                _state = AnimalState.Wandering;
                EnterIdle();
            }
        }

        private Vector3 ComputeFleeVelocity(Vector3 position)
        {
            Vector3 away = position - _threatPosition;
            away.y = 0f;
            if (away.sqrMagnitude < Epsilon)
            {
                away = _visual != null ? _visual.forward : Vector3.forward;
                away.y = 0f;
            }

            away.Normalize();
            Vector3 direction = ComputeFleeDirection(away);
            direction.y = 0f;
            float speed = _data.FleeSpeed;
            float lookahead = StoppingDistance(speed);
            // Confinement + corner escape with hysteresis (pure logic, covered by EditMode tests).
            Vector3 confined = AnimalSteering.SteerFlee(
                position, direction, away, _confinement, _data.ConfinementMargin, lookahead,
                Time.time, CornerEscapeHoldTime, ref _cornerEscape);

            return confined * speed;
        }

        private Vector3 ComputeWanderVelocity(Vector3 position, float now)
        {
            if (_isIdle)
            {
                if (now < _idleUntil)
                {
                    return Vector3.zero;
                }

                _isIdle = false;
                _wanderTarget = AnimalSteering.RandomPointInside(position, _data.WanderRadius, _confinement, _data.ConfinementMargin);
                _legEndTime = now + _data.MaxWanderLegTime;
            }

            Vector3 toTarget = _wanderTarget - position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= ArrivalDistanceSqr || now >= _legEndTime)
            {
                EnterIdle();
                return Vector3.zero;
            }

            float speed = _data.WanderSpeed;
            Vector3 direction = AnimalSteering.ConfineDirection(
                position, toTarget.normalized, _confinement, _data.ConfinementMargin, StoppingDistance(speed));
            return direction * speed;
        }

        /// <summary>Distance needed to stop from <paramref name="speed"/> with the configured acceleration.</summary>
        private float StoppingDistance(float speed)
        {
            float acceleration = _data.Acceleration;
            return acceleration > Epsilon ? speed * speed / (2f * acceleration) : 0f;
        }

        private void EnterIdle()
        {
            _isIdle = true;
            Vector2 range = _data != null ? _data.IdleTimeRange : Vector2.zero;
            _idleUntil = Time.time + Random.Range(range.x, range.y);
        }

        protected virtual void OnValidate()
        {
            if (_rb == null)
            {
                TryGetComponent(out _rb);
            }

            if (_collider == null)
            {
                TryGetComponent(out _collider);
            }

            if (_collider == null)
            {
                Debug.LogWarning($"[{nameof(Animal)}] Missing physics Collider on '{name}'.", this);
            }

            if (_visual == null)
            {
                Debug.LogWarning($"[{nameof(Animal)}] Missing visual transform on '{name}'.", this);
            }
        }
    }
}
