using AlienFarms.Core;
using AlienFarms.Data;
using UnityEngine;

namespace AlienFarms.Animals
{
    /// <summary>
    /// Pooled, Rigidbody-driven animal that wanders inside the Animal Zone, flees from nearby players
    /// and can be carried and thrown by a player. Derived types only change data and the flee direction.
    /// State flow: Wandering &lt;-&gt; Fleeing -&gt; Carried -&gt; (any release) Airborne -&gt; (lands) Captured when it
    /// settles inside a player zone (counts for that zone's owner), otherwise Wandering. Captured -&gt; Carried when
    /// either player grabs it. Only the private transition methods write the state; the transitions into and out of
    /// Captured are the single source of <see cref="GameEvents.OnAnimalCaptured"/> / <see cref="GameEvents.OnAnimalLeftZone"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public abstract class Animal : MonoBehaviour, IThrowable, IScareable
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

        // An airborne animal cannot land before this long, so the launch frame never counts as grounded (s).
        private const float MinAirTime = 0.08f;

        // Landing is accepted only when not moving up faster than this (m/s).
        private const float MaxLandingUpSpeed = 0.5f;

        // Extra length of the ground probe below the collider (m).
        private const float GroundProbeSkin = 0.1f;

        // Horizontal speed that pushes an airborne animal off a player it is resting on (m/s).
        private const float SlideOffSpeed = 3f;

        // Each slide direction must be free of obstacles for this distance (m).
        private const float SlideOffProbeDistance = 0.6f;

        // Resting on a player longer than this moves the animal to free ground beside the player (s).
        private const float MaxOnPlayerTime = 1.5f;

        // Gap kept between the player's collider and a relocated animal (m).
        private const float RelocateGap = 0.3f;

        // Candidate directions tried around the player when relocating (every 360 / count degrees).
        private const int RelocateDirections = 8;

        // Airborne ground probe: the body's footprint (planar extents shrunk by this factor so side contacts with walls
        // never count) swept down as a thin box, so resting on an edge narrower than the body counts as grounded.
        private const float FootprintInset = 0.9f;
        private const float FootprintHalfThickness = 0.02f;

        // Airborne, touching Environment, slower than this and not grounded for MaxStuckTime: pushed off (m/s, s).
        private const float MaxStuckSpeed = 0.2f;
        private const float MaxStuckTime = 0.3f;

        // Push-offs allowed per flight; past this a wedged animal is left to the air timeout (despawn + refill).
        private const int MaxPushOffsPerFlight = 3;

        // Obstacle probe sphere radius, as a fraction of the body's smallest planar half extent.
        private const float ProbeRadiusFactor = 0.9f;

        // A relocation ground hit higher than this above the player's feet is a ledge or a fence top, not the floor (m).
        private const float RelocateMaxStepUp = 0.5f;

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
        private IZoneResolver _zones;
        private float _killHeight = float.NegativeInfinity;
        private AnimalState _state = AnimalState.Inactive;
        private PlayerId _holder = PlayerId.None;
        private PlayerId _owner = PlayerId.None;
        private int _freeLayer;
        private int _capturedLayer;
        private bool _layersCached;
        private Transform _holdPoint;
        private int _playerMask;
        private int _groundMask;
        // Airborne landing only: an inert bomb is something to stand on. Relocation keeps using _groundMask so it
        // never places an animal on top of a bomb.
        private int _landingMask;
        private float _airborneSince;
        private float _onPlayerSince;
        private bool _onPlayer;
        private int _relocateBlockingMask;
        private float _activeMargin;
        private float _collisionRadius;
        private float _halfHeight;
        private float _probeRadius;
        private int _obstacleMask;
        private int _environmentLayer = -1;
        private bool _hitEnvironmentInFlight;
        private bool _environmentContact;
        private Vector3 _environmentContactPoint;
        private Vector3 _environmentContactNormal;
        private float _stuckSince;
        private int _pushOffCount;
        private AvoidanceSide _avoidance;

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

        /// <summary>True while the animal is in play and not captured (carried and airborne included).</summary>
        public bool IsFree => _state != AnimalState.Inactive && _state != AnimalState.Captured;

        /// <summary>Player whose pen holds this animal, or <see cref="PlayerId.None"/> while not captured.</summary>
        public PlayerId Owner => _owner;

        /// <summary>On the ground (free or captured) and past the drop cooldown. Never while carried or airborne.</summary>
        public bool CanBeGrabbed =>
            (_state == AnimalState.Wandering || _state == AnimalState.Fleeing || _state == AnimalState.Captured)
            && Time.time >= _grabCooldownUntil;

        public bool CanBeThrown => _state == AnimalState.Carried;

        // Explicit Unity null check: the ?. operator would bypass UnityEngine.Object's overloaded equality.
        public ThrowSettings ThrowSettings => _data != null ? _data.ThrowSettings : null;

        // Read from the prefab's collider size (the collider is disabled while carried, so bounds would be empty).
        public float CollisionRadius => _collisionRadius;
        public float HalfHeight => _halfHeight;
        public PlayerId Holder => _holder;
        public Vector3 Position => transform.position;
        public float CarrySpeedMultiplier => _data != null ? _data.CarrySpeedMultiplier : 1f;
        public float ThrowDistanceMultiplier => _data != null ? _data.ThrowDistanceMultiplier : 1f;

        /// <summary>Points this animal is worth while captured in a player's zone.</summary>
        public int Points => _data != null ? _data.Points : 1;

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
            _groundMask = LayerMask.GetMask(LayerNames.Ground, LayerNames.Environment);
            _landingMask = LayerMask.GetMask(LayerNames.Ground, LayerNames.Environment, LayerNames.Bomb);
            _relocateBlockingMask = LayerMask.GetMask(LayerNames.Environment, LayerNames.Player, LayerNames.Bomb);
            _obstacleMask = LayerMask.GetMask(LayerNames.Environment);
            _environmentLayer = LayerMask.NameToLayer(LayerNames.Environment);
            CacheLayers();
            CacheCollisionSize();
        }

        /// <summary>
        /// Called once by the pool right after instantiation. <paramref name="zones"/> resolves which player zone the
        /// animal lands in (null: it never gets captured). Below <paramref name="killHeight"/> it is despawned as lost.
        /// </summary>
        public void Initialize(AnimalData data, Bounds confinement, AnimalTypePool pool, IZoneResolver zones, float killHeight)
        {
            // The pool instantiates inactive-parented clones; make sure cached refs exist before use.
            if (_rb == null)
            {
                _rb = GetComponent<Rigidbody>();
            }

            CacheLayers();
            CacheCollisionSize();

            _data = data;
            _confinement = confinement;
            _homeConfinement = confinement;
            _pool = pool;
            _zones = zones;
            _killHeight = killHeight;
            _state = AnimalState.Inactive;
            _activeMargin = _data != null ? _data.ConfinementMargin : 0f;

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
            _activeMargin = _data != null ? _data.ConfinementMargin : 0f;
            _owner = PlayerId.None;
            _holder = PlayerId.None;
            _holdPoint = null;
            _grabCooldownUntil = 0f;
            _steeringResumeTime = 0f;
            _cornerEscape.Clear();
            _avoidance.Clear();
            _state = AnimalState.Wandering;

            float interval = _data != null ? _data.DetectionInterval : 0.2f;
            // Random phase spreads detection queries of many animals across frames.
            _nextDetectionTime = Time.time + Random.Range(0f, interval);
            EnterIdle();

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Removes the animal from play and returns it to its pool. A carried animal is dropped first (Forced) and a
        /// captured one leaves its zone first, so listeners always see a balanced event sequence.
        /// </summary>
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
            else if (_state == AnimalState.Captured)
            {
                ExitCaptured();
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

            // Taking it out of a pen (own or rival) removes its point right away.
            if (_state == AnimalState.Captured)
            {
                ExitCaptured();
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
            _grabCooldownUntil = Time.time + (_data != null ? _data.GrabCooldown : 0f);
            EnterAirborne();

            // MUST stay the last statement: listeners may react synchronously and rely on the release state above
            // being fully applied.
            GameEvents.RaiseAnimalDropped(previousHolder, this, reason);
        }

        /// <summary>
        /// Sudden threat (a blast): a free animal on the ground flees from <paramref name="threatPosition"/> for at least
        /// <paramref name="minDuration"/> seconds. Carried, airborne, captured and inactive animals ignore it.
        /// </summary>
        public void Scare(Vector3 threatPosition, float minDuration)
        {
            if (_data == null || (_state != AnimalState.Wandering && _state != AnimalState.Fleeing))
            {
                return;
            }

            float fleeEnd = Time.time + Mathf.Max(minDuration, _data.MinFleeTime);
            _threatPosition = threatPosition;
            _minFleeEndTime = _state == AnimalState.Fleeing ? Mathf.Max(_minFleeEndTime, fleeEnd) : fleeEnd;
            _state = AnimalState.Fleeing;
        }

        private void EnterAirborne()
        {
            _state = AnimalState.Airborne;
            _airborneSince = Time.time;
            _onPlayer = false;
            _hitEnvironmentInFlight = false;
            _environmentContact = false;
            _stuckSince = Time.time;
            _pushOffCount = 0;
            _cornerEscape.Clear();
            _avoidance.Clear();
        }

        /// <summary>
        /// First Environment contact in flight (wall, fence, rounded obstacle): the horizontal momentum is removed so the
        /// animal drops where the throw preview stops, instead of skimming a curved surface. Ground contacts are left to
        /// the normal landing. Never applies on a player (SlideOff/Relocate keep working).
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            if (!RecordEnvironmentContact(collision) || _hitEnvironmentInFlight)
            {
                return;
            }

            _hitEnvironmentInFlight = true;
            Vector3 velocity = _rb.linearVelocity;
            _rb.linearVelocity = new Vector3(0f, Mathf.Min(velocity.y, 0f), 0f);
        }

        private void OnCollisionStay(Collision collision)
        {
            RecordEnvironmentContact(collision);
        }

        /// <summary>Airborne contact with Environment: remembered for the next step's stuck check. No allocations.</summary>
        private bool RecordEnvironmentContact(Collision collision)
        {
            if (_state != AnimalState.Airborne || _environmentLayer < 0 || collision.gameObject.layer != _environmentLayer)
            {
                return false;
            }

            _environmentContact = true;
            if (collision.contactCount > 0)
            {
                ContactPoint contact = collision.GetContact(0);
                _environmentContactPoint = contact.point;
                _environmentContactNormal = contact.normal;
            }
            else
            {
                _environmentContactPoint = _rb.position;
                _environmentContactNormal = Vector3.zero;
            }

            return true;
        }

        /// <summary>
        /// Landed: the horizontal momentum is removed so the animal stays where it touched down (the spot the throw
        /// preview marks), then it is captured by the zone it settled in, otherwise free again.
        /// </summary>
        private void Land()
        {
            Vector3 velocity = _rb.linearVelocity;
            _rb.linearVelocity = new Vector3(0f, Mathf.Min(velocity.y, 0f), 0f);

            if (_zones != null && _zones.TryGetZoneAt(_rb.position, out PlayerId owner, out Bounds area))
            {
                EnterCaptured(owner, area);
                return;
            }

            EnterFree();
        }

        private void EnterFree()
        {
            _state = AnimalState.Wandering;
            // Short pause before steering takes over again (reads as a recovery after the landing).
            _steeringResumeTime = Time.time + SteeringResumeDelay;
            _cornerEscape.Clear();
            _avoidance.Clear();
            EnterIdle();
        }

        /// <summary>
        /// Settles the animal in <paramref name="owner"/>'s pen: Captured, CapturedAnimal layer (kept in by the
        /// barrier), clamped inside <paramref name="pen"/> and wandering there without fleeing.
        /// </summary>
        private void EnterCaptured(PlayerId owner, Bounds pen)
        {
            _state = AnimalState.Captured;
            _owner = owner;
            _confinement = pen;
            _activeMargin = _data != null ? _data.PenConfinementMargin : 0f;
            // The physics collider lives on this root (the visual has none), so the root layer is enough.
            gameObject.layer = _capturedLayer;

            ClampInsidePen();

            _cornerEscape.Clear();
            _avoidance.Clear();
            EnterIdle();
            _steeringResumeTime = 0f;

            // Last: listeners see the animal fully captured.
            GameEvents.RaiseAnimalCaptured(owner, this);
        }

        /// <summary>
        /// Leaves the pen (grabbed or despawned) and restores the free setup. Leaves the state at Wandering; the caller
        /// moves it on (Carried or Inactive) right after.
        /// </summary>
        private void ExitCaptured()
        {
            PlayerId previousOwner = _owner;
            _owner = PlayerId.None;
            _state = AnimalState.Wandering;
            gameObject.layer = _freeLayer;
            _confinement = _homeConfinement;
            _activeMargin = _data != null ? _data.ConfinementMargin : 0f;

            // Last: listeners see the animal already out of the zone.
            GameEvents.RaiseAnimalLeftZone(previousOwner, this);
        }

        /// <summary>Planar radius and half height of the physics collider, from its size (works while disabled).</summary>
        private void CacheCollisionSize()
        {
            if (_collider == null)
            {
                TryGetComponent(out _collider);
            }

            Vector3 extents;
            if (_collider is BoxCollider box)
            {
                extents = Vector3.Scale(box.size, transform.lossyScale) * 0.5f;
            }
            else if (_collider is CapsuleCollider capsule)
            {
                float radius = capsule.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
                extents = new Vector3(radius, capsule.height * 0.5f * Mathf.Abs(transform.lossyScale.y), radius);
            }
            else if (_collider is SphereCollider sphere)
            {
                float radius = sphere.radius * Mathf.Abs(transform.lossyScale.x);
                extents = new Vector3(radius, radius, radius);
            }
            else
            {
                extents = Vector3.zero;
            }

            _collisionRadius = Mathf.Max(Mathf.Abs(extents.x), Mathf.Abs(extents.z));
            _halfHeight = Mathf.Abs(extents.y);
            // Never taller than the body, so the probe (centered at half height) does not scrape the floor.
            _probeRadius = ProbeRadiusFactor * Mathf.Min(Mathf.Min(Mathf.Abs(extents.x), Mathf.Abs(extents.z)), _halfHeight);
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

            // Fell off the map in any state (thrown, pushed or walked off): never keep a lost animal in play.
            if (position.y < _killHeight)
            {
                Despawn();
                return;
            }

            // In flight: pure physics (no steering, no clamp) until it lands.
            if (_state == AnimalState.Airborne)
            {
                // Center ray first (cheap, also catches a body sunk into the floor), then the whole footprint (edges).
                bool grounded = Probe(_landingMask, out _) || ProbeFootprint(_landingMask);
                // Only probed when not grounded: a player under the animal is the one case that blocks the landing.
                RaycastHit playerHit = default;
                bool onPlayer = !grounded && Probe(_playerMask, out playerHit);
                AnimalLanding.TrackPlayerContact(onPlayer, _onPlayer, now, ref _airborneSince, ref _onPlayerSince);
                _onPlayer = onPlayer;

                // Collision callbacks run after the physics step: this reads the contacts of the previous step.
                bool touchingEnvironment = _environmentContact;
                _environmentContact = false;
                bool stuck = AnimalLanding.IsStuckOnEnvironment(
                    touchingEnvironment, grounded, onPlayer, _rb.linearVelocity.sqrMagnitude, MaxStuckSpeed);
                if (AnimalLanding.ShouldPushOff(stuck, now, ref _stuckSince, MaxStuckTime, _pushOffCount, MaxPushOffsPerFlight))
                {
                    _pushOffCount++;
                    PushOffEnvironment();
                    // A fresh fall: never despawned by the air timeout while it is still on the map.
                    _airborneSince = now;
                    _stuckSince = now;
                    return;
                }

                AirborneOutcome outcome = AnimalLanding.Evaluate(
                    now - _airborneSince, _rb.linearVelocity.y, grounded, onPlayer, now - _onPlayerSince, MaxOnPlayerTime,
                    position.y, _killHeight, MinAirTime, _data.MaxAirborneTime, MaxLandingUpSpeed);

                if (outcome == AirborneOutcome.Land)
                {
                    Land();
                }
                else if (outcome == AirborneOutcome.SlideOff)
                {
                    SlideOff(position, playerHit);
                }
                else if (outcome == AirborneOutcome.Relocate)
                {
                    if (TryRelocateBeside(playerHit))
                    {
                        Land();
                    }
                    else
                    {
                        // Last resort: never stuck on a player forever; the spawner refills the population.
                        Despawn();
                    }
                }
                else if (outcome == AirborneOutcome.Despawn)
                {
                    // Timed out without ground below (stuck or lost): the spawner refills the population.
                    Despawn();
                }

                return;
            }

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

            Vector3 desired;
            Vector3 sideHint;
            float speed;
            if (_state == AnimalState.Fleeing)
            {
                desired = ComputeFleeVelocity(position, out sideHint);
                speed = _data.FleeSpeed;
            }
            else
            {
                desired = ComputeWanderVelocity(position, now);
                sideHint = Vector3.zero;
                speed = _data.WanderSpeed;
            }

            desired = AvoidObstacles(position, desired, speed, sideHint, now);
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

        /// <summary>
        /// Pushes the animal horizontally off the player under it; it then falls and lands normally. Tries away from the
        /// player, then the opposite, then both perpendiculars, and takes the first one a sweep finds free. If all are
        /// blocked it waits: the on-player timeout relocates it.
        /// </summary>
        private void SlideOff(Vector3 position, RaycastHit playerHit)
        {
            Rigidbody support = playerHit.rigidbody;
            Vector3 away = AwayFromPlayer(position, playerHit);
            Vector3 right = new Vector3(away.z, 0f, -away.x);

            for (int i = 0; i < 4; i++)
            {
                Vector3 direction = i == 0 ? away : i == 1 ? -away : i == 2 ? right : -right;
                if (IsSlideBlocked(direction, support))
                {
                    continue;
                }

                Vector3 velocity = _rb.linearVelocity;
                _rb.linearVelocity = new Vector3(direction.x * SlideOffSpeed, velocity.y, direction.z * SlideOffSpeed);
                return;
            }
        }

        /// <summary>Pushes a stuck airborne animal horizontally away from the Environment contact; it then falls and lands.</summary>
        private void PushOffEnvironment()
        {
            Vector3 center = _collider != null && _collider.enabled ? _collider.bounds.center : _rb.position;
            Vector3 fallback = _visual != null ? -_visual.forward : Vector3.back;
            Vector3 direction = AnimalLanding.PushOffDirection(center, _environmentContactPoint, _environmentContactNormal, fallback);
            Vector3 velocity = _rb.linearVelocity;
            _rb.linearVelocity = new Vector3(direction.x * SlideOffSpeed, Mathf.Min(velocity.y, 0f), direction.z * SlideOffSpeed);
        }

        /// <summary>
        /// Thin box with the body's (inset) planar footprint swept from the collider center to just below its bottom.
        /// False when the collider is disabled (the center ray covers that case).
        /// </summary>
        private bool ProbeFootprint(int mask)
        {
            if (_collider == null || !_collider.enabled)
            {
                return false;
            }

            Bounds bounds = _collider.bounds;
            Vector3 extents = bounds.extents;
            Vector3 halfExtents = new Vector3(extents.x * FootprintInset, FootprintHalfThickness, extents.z * FootprintInset);
            float distance = Mathf.Max(0f, extents.y - FootprintHalfThickness) + GroundProbeSkin;
            return Physics.BoxCast(bounds.center, halfExtents, Vector3.down, Quaternion.identity, distance, mask, QueryTriggerInteraction.Ignore);
        }

        private bool IsSlideBlocked(Vector3 direction, Rigidbody support)
        {
            if (!_rb.SweepTest(direction, out RaycastHit hit, SlideOffProbeDistance, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            // The player we rest on may be reported as touching; it never blocks a sideways slide.
            return support == null || hit.rigidbody != support;
        }

        /// <summary>
        /// Moves the animal onto free floor beside the player under it (no Environment or Player overlap). Tries
        /// <see cref="RelocateDirections"/> directions starting away from the player. False if none is free.
        /// </summary>
        private bool TryRelocateBeside(RaycastHit playerHit)
        {
            Collider playerCollider = playerHit.collider;
            if (playerCollider == null)
            {
                return false;
            }

            Bounds playerBounds = playerCollider.bounds;
            Vector3 center = playerBounds.center;
            Vector3 away = AwayFromPlayer(_rb.position, playerHit);
            float distance = Mathf.Max(playerBounds.extents.x, playerBounds.extents.z) + _collisionRadius + RelocateGap;
            float probeTop = playerBounds.max.y;
            float probeLength = playerBounds.size.y + RelocateMaxStepUp + GroundProbeSkin;
            float maxFloorY = playerBounds.min.y + RelocateMaxStepUp;
            Vector3 halfExtents = new Vector3(_collisionRadius, _halfHeight, _collisionRadius);
            float stepAngle = 360f / RelocateDirections;

            for (int i = 0; i < RelocateDirections; i++)
            {
                Vector3 direction = Quaternion.AngleAxis(i * stepAngle, Vector3.up) * away;
                Vector3 probe = new Vector3(center.x + direction.x * distance, probeTop, center.z + direction.z * distance);
                if (!Physics.Raycast(probe, Vector3.down, out RaycastHit floor, probeLength, _groundMask, QueryTriggerInteraction.Ignore)
                    || floor.point.y > maxFloorY)
                {
                    continue;
                }

                // Pivot is the base of the body: the box sits just above the floor.
                Vector3 bodyCenter = floor.point + Vector3.up * (_halfHeight + GroundProbeSkin);
                if (Physics.CheckBox(bodyCenter, halfExtents, Quaternion.identity, _relocateBlockingMask, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                Vector3 placed = floor.point + Vector3.up * (0.5f * GroundProbeSkin);
                _rb.position = placed;
                transform.position = placed;
                _rb.linearVelocity = Vector3.zero;
                _onPlayer = false;
                return true;
            }

            return false;
        }

        /// <summary>Planar unit vector from the player under the animal towards it, with a fallback when centered.</summary>
        private Vector3 AwayFromPlayer(Vector3 position, RaycastHit playerHit)
        {
            Rigidbody playerBody = playerHit.rigidbody;
            Vector3 playerCenter = playerBody != null
                ? playerBody.position
                : playerHit.collider != null ? playerHit.collider.transform.position : position;
            Vector3 away = position - playerCenter;
            away.y = 0f;
            if (away.sqrMagnitude < Epsilon)
            {
                // Centered on the head: go backwards from where the carrier faced (a drop against a wall lands here).
                away = _visual != null ? -_visual.forward : Vector3.back;
                away.y = 0f;
                if (away.sqrMagnitude < Epsilon)
                {
                    away = Vector3.back;
                }
            }

            return away.normalized;
        }

        /// <summary>Ray from the collider center down to just below its bottom, against <paramref name="mask"/>.</summary>
        private bool Probe(int mask, out RaycastHit hit)
        {
            Vector3 origin;
            float length;
            if (_collider != null && _collider.enabled)
            {
                Bounds bounds = _collider.bounds;
                origin = bounds.center;
                length = bounds.extents.y + GroundProbeSkin;
            }
            else
            {
                origin = _rb.position + Vector3.up * GroundProbeSkin;
                length = 2f * GroundProbeSkin;
            }

            return Physics.Raycast(origin, Vector3.down, out hit, length, mask, QueryTriggerInteraction.Ignore);
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

        /// <summary>
        /// Forward sphere probe against Environment; on a hit, slides <paramref name="desired"/> along the obstacle
        /// (<see cref="ObstacleAvoidance"/>) and confines the result to the zone again. <paramref name="sideHint"/> picks the
        /// side to go around a head-on obstacle (away from the threat when fleeing; zero = remembered or random side).
        /// </summary>
        private Vector3 AvoidObstacles(Vector3 position, Vector3 desired, float speed, Vector3 sideHint, float now)
        {
            if (_obstacleMask == 0 || _probeRadius <= Epsilon || speed <= Epsilon || desired.sqrMagnitude < Epsilon)
            {
                return desired;
            }

            Vector3 direction = desired / speed;
            Vector3 castDirection = direction.normalized;
            float stopping = StoppingDistance(speed);
            float probeDistance = _collisionRadius + stopping + _data.ObstacleLookahead;
            Vector3 origin = position + Vector3.up * _halfHeight;

            if (!Physics.SphereCast(origin, _probeRadius, castDirection, out RaycastHit hit, probeDistance, _obstacleMask, QueryTriggerInteraction.Ignore))
            {
                if (_avoidance.Side != 0 && !_avoidance.IsActive(now))
                {
                    _avoidance.Clear();
                }

                return desired;
            }

            // Identifies the collider for the side memory (a different obstacle never inherits the side).
            int obstacleId = hit.colliderEntityId.GetHashCode();
            float holdTime = _data.ObstacleSideHoldTime;

            // No hint and no remembered side: pick one at random (inactive memory) so a herd does not all turn the same way.
            if (sideHint.sqrMagnitude < Epsilon && _avoidance.Side == 0)
            {
                _avoidance.Begin(Random.value < 0.5f ? -1 : 1, 0f, obstacleId);
            }

            Vector3 avoided = ObstacleAvoidance.Avoid(
                direction, hit.normal, hit.distance, probeDistance, obstacleId, sideHint, now, holdTime, ref _avoidance);
            Vector3 confined = AnimalSteering.ConfineDirection(position, avoided, _confinement, _activeMargin, stopping);

            // Wedge between the obstacle and a zone edge: the edge cancelled the detour (or turned it back into the
            // obstacle). Go around the other side instead, once.
            Vector3 planarNormal = new Vector3(hit.normal.x, 0f, hit.normal.z);
            float corneredSqr = AnimalSteering.CorneredRatio * AnimalSteering.CorneredRatio;
            bool detoured = Vector3.Dot(direction, planarNormal) < 0f;
            if (detoured
                && (confined.sqrMagnitude < corneredSqr * avoided.sqrMagnitude || Vector3.Dot(confined, planarNormal) < -Epsilon))
            {
                int otherSide = -ObstacleAvoidance.SideOf(avoided, hit.normal);
                _avoidance.Begin(otherSide, now + holdTime, obstacleId);
                avoided = ObstacleAvoidance.Detour(direction, hit.normal, hit.distance, probeDistance, otherSide);
                confined = AnimalSteering.ConfineDirection(position, avoided, _confinement, _activeMargin, stopping);
            }

            return confined * speed;
        }

        /// <summary>
        /// Random wander target inside the zone, re-rolled up to <see cref="AnimalData.WanderTargetRetries"/> times while
        /// it lands inside an obstacle. The last roll is kept anyway (avoidance and the leg timeout handle it).
        /// </summary>
        private Vector3 PickWanderTarget(Vector3 position)
        {
            Vector3 target = AnimalSteering.RandomPointInside(position, _data.WanderRadius, _confinement, _activeMargin);
            if (_obstacleMask == 0)
            {
                return target;
            }

            for (int i = 0; i < _data.WanderTargetRetries; i++)
            {
                if (!Physics.CheckSphere(target + Vector3.up * _halfHeight, _collisionRadius, _obstacleMask, QueryTriggerInteraction.Ignore))
                {
                    break;
                }

                target = AnimalSteering.RandomPointInside(position, _data.WanderRadius, _confinement, _activeMargin);
            }

            return target;
        }

        private Vector3 ComputeFleeVelocity(Vector3 position, out Vector3 awayFromThreat)
        {
            Vector3 away = position - _threatPosition;
            away.y = 0f;
            if (away.sqrMagnitude < Epsilon)
            {
                away = _visual != null ? _visual.forward : Vector3.forward;
                away.y = 0f;
            }

            away.Normalize();
            awayFromThreat = away;
            Vector3 direction = ComputeFleeDirection(away);
            direction.y = 0f;
            float speed = _data.FleeSpeed;
            float lookahead = StoppingDistance(speed);
            // Confinement + corner escape with hysteresis (pure logic, covered by EditMode tests).
            Vector3 confined = AnimalSteering.SteerFlee(
                position, direction, away, _confinement, _activeMargin, lookahead,
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
                _wanderTarget = PickWanderTarget(position);
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
                position, toTarget.normalized, _confinement, _activeMargin, StoppingDistance(speed));
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

            // The throw preview assumes a pure ballistic flight.
            if (_rb != null && (_rb.linearDamping != 0f || !_rb.useGravity))
            {
                Debug.LogWarning($"[{nameof(Animal)}] Rigidbody on '{name}' should use gravity with Linear Damping 0; thrown arcs will not match the preview.", this);
            }

            if (_visual == null)
            {
                Debug.LogWarning($"[{nameof(Animal)}] Missing visual transform on '{name}'.", this);
            }
        }
    }
}
