using System;
using AlienFarms.Core;
using AlienFarms.Data;
using UnityEngine;
using UnityEngine.Pool;

namespace AlienFarms.Items
{
    /// <summary>
    /// Pooled stun bomb. Inert while lying around or dropped (Idle): anyone can carry it, it never explodes. Thrown, it
    /// is Armed and explodes on its first contact that is not its thrower (or after a timeout), stunning every other
    /// player in the blast radius and scaring nearby free animals. It never touches capture or score.
    /// Does not share a base with Animal on purpose (its armed/inert behavior is different); the hold snap and the
    /// release order (dynamic before writing position/velocity) are duplicated consciously from it.
    /// As a <see cref="SimulationRole.Replica"/> it never arms, detonates or raises GameEvents: its body is kinematic and
    /// its state comes from <see cref="ApplyReplicatedState"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Bomb : MonoBehaviour, IThrowable, ISimulationRoleTarget
    {
        private const float Epsilon = 0.0001f;

        // Enough for every free animal of a crowded Animal Zone inside the scare radius.
        private const int MaxScareHits = 32;

        // Shared by all bombs (no allocation per blast). Safe because the query loops only read: stuns (which drop
        // objects and raise events) are applied after the player loop, and Scare never queries physics.
        private static readonly Collider[] s_playerHits = new Collider[8];
        private static readonly Collider[] s_animalHits = new Collider[MaxScareHits];

        [Header("References")]
        [SerializeField, Tooltip("Rigidbody on this root (freeze rotation, interpolate, ContinuousSpeculative, no damping).")]
        private Rigidbody _rb;

        [SerializeField, Tooltip("Sphere collider on this root, center at (0, radius, 0) so the pivot is the base.")]
        private SphereCollider _collider;

        [SerializeField, Tooltip("Emissive child (with its light) that blinks while armed. Inactive by default.")]
        private GameObject _armedIndicator;

        private BombData _data;
        private IObjectPool<Bomb> _pool;
        private float _killHeight = float.NegativeInfinity;
        private BombState _state = BombState.Inactive;
        private PlayerId _holder = PlayerId.None;
        private PlayerId _thrower = PlayerId.None;
        private Transform _holdPoint;
        private float _grabCooldownUntil;
        private float _armedSince;
        private float _nextBlinkTime;
        private float _radius;
        private int _playerMask;
        private int _animalMask;
        private int _playerLayer = -1;
        private bool _cached;
        private SimulationRole _role = SimulationRole.Authority;
        private IBombNetworkHook _networkHook;

        /// <summary>
        /// Raised after each completed transition (state, holder or thrower changed), before the matching GameEvent.
        /// </summary>
        public event Action<Bomb> StateChanged;

        /// <summary>
        /// Authority only: raised when the bomb explodes, right before <see cref="GameEvents.OnBombDetonated"/> and the
        /// stuns. Payload: this bomb, blast center and blast radius (m).
        /// </summary>
        public event Action<Bomb, Vector3, float> Detonated;

        public BombState State => _state;

        public SimulationRole Role => _role;

        /// <summary>Player who threw the bomb; valid while <see cref="BombState.Armed"/>.</summary>
        public PlayerId Thrower => _thrower;

        public bool CanBeGrabbed => _state == BombState.Idle && Time.time >= _grabCooldownUntil;
        public bool CanBeThrown => _state == BombState.Carried;

        // Explicit Unity null check: the ?. operator would bypass UnityEngine.Object's overloaded equality.
        public ThrowSettings ThrowSettings => _data != null ? _data.ThrowSettings : null;

        public float CollisionRadius => _radius;
        public float HalfHeight => _radius;
        public PlayerId Holder => _holder;
        public Vector3 Position => transform.position;
        public float CarrySpeedMultiplier => _data != null ? _data.CarrySpeedMultiplier : 1f;
        public float ThrowDistanceMultiplier => 1f;

        private void Awake()
        {
            CacheSetup();
        }

        /// <summary>Called once by the pool right after instantiation.</summary>
        public void Initialize(BombData data, IObjectPool<Bomb> pool, float killHeight)
        {
            CacheSetup();
            _data = data;
            _pool = pool;
            _killHeight = killHeight;
            _state = BombState.Inactive;
        }

        /// <summary>Places the bomb inert at <paramref name="position"/> (its base) and activates it.</summary>
        public void Spawn(Vector3 position)
        {
            _rb.isKinematic = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.position = position;
            _rb.rotation = Quaternion.identity;
            transform.SetPositionAndRotation(position, Quaternion.identity);
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;

            // Velocities are cleared while still dynamic: writing them on a kinematic body is not supported.
            if (_role == SimulationRole.Replica)
            {
                _rb.isKinematic = true;
            }

            if (_collider != null)
            {
                _collider.enabled = true;
            }

            _holder = PlayerId.None;
            _thrower = PlayerId.None;
            _holdPoint = null;
            _grabCooldownUntil = 0f;
            SetIndicator(false);
            _state = BombState.Idle;

            gameObject.SetActive(true);
            if (_role == SimulationRole.Replica)
            {
                NotifyStateChanged();
                return;
            }

            _networkHook?.OnSpawned(this);
            NotifyStateChanged();
            GameEvents.RaiseBombSpawned(this);
        }

        /// <summary>Network hook this bomb reports its authority spawns and despawns to (null = local pooling).</summary>
        public void SetNetworkHook(IBombNetworkHook hook)
        {
            _networkHook = hook;
        }

        /// <summary>
        /// Replica: never arms, detonates or grabs, no GameEvents, kinematic body. Authority (default): full simulation.
        /// </summary>
        public void SetSimulationRole(SimulationRole role)
        {
            CacheSetup();
            _role = role;
            if (role == SimulationRole.Replica)
            {
                _rb.isKinematic = true;
            }
            else if (_state != BombState.Inactive)
            {
                _rb.isKinematic = _state == BombState.Carried;
            }
        }

        /// <summary>
        /// Replica: applies the authority's state, holder and thrower (the transform is synchronized separately).
        /// Updates collider and armed indicator like the authority's transitions do, and raises
        /// <see cref="StateChanged"/>. Inactive leaves play (<see cref="Despawn"/>). Never raises GameEvents; ignored on
        /// the authority.
        /// </summary>
        public void ApplyReplicatedState(BombState state, PlayerId holder, PlayerId thrower)
        {
            if (_role != SimulationRole.Replica)
            {
                return;
            }

            if (state == BombState.Inactive)
            {
                Despawn();
                return;
            }

            if (state == _state && holder == _holder && thrower == _thrower)
            {
                return;
            }

            bool becameArmed = state == BombState.Armed && _state != BombState.Armed;
            _state = state;
            _holder = state == BombState.Carried ? holder : PlayerId.None;
            _thrower = state == BombState.Armed ? thrower : PlayerId.None;
            if (state != BombState.Carried)
            {
                _holdPoint = null;
            }

            if (_collider != null)
            {
                _collider.enabled = state != BombState.Carried;
            }

            if (becameArmed)
            {
                _armedSince = Time.time;
                _nextBlinkTime = Time.time + ArmedBlinkInterval;
            }

            SetIndicator(state == BombState.Armed);
            NotifyStateChanged();
        }

        public bool TryGrab(PlayerId holder, Transform holdPoint)
        {
            if (_role == SimulationRole.Replica
                || !CanBeGrabbed || holder == PlayerId.None || holdPoint == null || _holder != PlayerId.None)
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

            _holder = holder;
            _holdPoint = holdPoint;
            _state = BombState.Carried;
            SnapToHoldPoint();
            NotifyStateChanged();
            GameEvents.RaiseBombGrabbed(holder, this);
            return true;
        }

        /// <summary>
        /// Thrown: armed (detonates on contact). Any other reason: inert on the ground. No-op unless carried (a grabber
        /// may still release a bomb that was already returned to the pool).
        /// </summary>
        public void Release(Vector3 position, Vector3 velocity, DropReason reason)
        {
            if (_state != BombState.Carried || _role == SimulationRole.Replica)
            {
                return;
            }

            PlayerId previousHolder = _holder;

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

            _holder = PlayerId.None;
            _holdPoint = null;

            if (reason == DropReason.Thrown)
            {
                _thrower = previousHolder;
                _armedSince = Time.time;
                _nextBlinkTime = Time.time + ArmedBlinkInterval;
                SetIndicator(true);
                _state = BombState.Armed;
                NotifyStateChanged();
                GameEvents.RaiseBombArmed(previousHolder, this);
                return;
            }

            _thrower = PlayerId.None;
            _grabCooldownUntil = Time.time + (_data != null ? _data.GrabCooldown : 0f);
            SetIndicator(false);
            _state = BombState.Idle;
            NotifyStateChanged();
        }

        /// <summary>
        /// Removes the bomb from play (any state) and returns it to its pool, or hands it to the network hook, which then
        /// returns it. Never detonates. A replica just leaves play, without events.
        /// </summary>
        public void Despawn()
        {
            if (_state == BombState.Inactive)
            {
                // A replica whose Inactive state was replicated before its despawn still goes back to the pool.
                if (_role == SimulationRole.Replica && !IsInPool)
                {
                    ReturnToPool();
                }

                return;
            }

            // A holder sees Holder != its id and clears its side on its next physics step.
            _holder = PlayerId.None;
            _holdPoint = null;
            _thrower = PlayerId.None;
            SetIndicator(false);
            _state = BombState.Inactive;
            NotifyStateChanged();

            if (_role == SimulationRole.Authority)
            {
                // Before returning to the pool, so listeners still see this instance as the one that left play.
                GameEvents.RaiseBombDespawned(this);

                if (_networkHook != null && _networkHook.TryDespawn(this))
                {
                    return;
                }
            }

            ReturnToPool();
        }

        /// <summary>True while the instance sits inactive in its pool (maintained by <see cref="BombSpawner"/>).</summary>
        internal bool IsInPool { get; set; }

        private void ReturnToPool()
        {
            if (_pool != null)
            {
                _pool.Release(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_state != BombState.Armed || _role == SimulationRole.Replica)
            {
                return;
            }

            // Physics event: resolving the contact's components here is allowed.
            Rigidbody body = collision.rigidbody;
            bool isThrowerBody = body != null
                && body.gameObject.layer == _playerLayer
                && body.TryGetComponent(out PlayerIdentity identity)
                && identity.Id == _thrower;

            if (BombBlastRules.ShouldDetonateOnContact(_state, isThrowerBody))
            {
                Detonate();
            }
        }

        private void FixedUpdate()
        {
            if (_role == SimulationRole.Replica || _state == BombState.Inactive || _state == BombState.Carried)
            {
                return;
            }

            // Fell off the map: lost, never explodes.
            if (_rb.position.y < _killHeight)
            {
                Despawn();
                return;
            }

            if (_state == BombState.Armed && BombBlastRules.HasTimedOut(Time.time - _armedSince, MaxArmedTime))
            {
                Detonate();
            }
        }

        private void Update()
        {
            if (_state != BombState.Armed || _armedIndicator == null || Time.time < _nextBlinkTime)
            {
                return;
            }

            _nextBlinkTime = Time.time + ArmedBlinkInterval;
            _armedIndicator.SetActive(!_armedIndicator.activeSelf);
        }

        private void LateUpdate()
        {
            if (_state != BombState.Carried)
            {
                return;
            }

            // Holder destroyed without dropping us: never stay frozen in the air. A replica has no hold point of its
            // own (its transform is synchronized from the authority).
            if (_holdPoint == null)
            {
                if (_role == SimulationRole.Authority)
                {
                    Release(transform.position, Vector3.zero, DropReason.Forced);
                }

                return;
            }

            SnapToHoldPoint();
        }

        private void NotifyStateChanged() => StateChanged?.Invoke(this);

        /// <summary>Stuns every player in range except the thrower, scares free animals, then returns to the pool.</summary>
        private void Detonate()
        {
            if (_state != BombState.Armed || _data == null)
            {
                Despawn();
                return;
            }

            PlayerId thrower = _thrower;
            Vector3 center = _rb.position + Vector3.up * _radius;
            float blastRadius = _data.BlastRadius;

            Detonated?.Invoke(this, center, blastRadius);
            GameEvents.RaiseBombDetonated(thrower, center, blastRadius);

            if (_playerMask != 0 && blastRadius > 0f)
            {
                StunPlayers(thrower, center, blastRadius);
            }

            if (_data.ScaresAnimals && _animalMask != 0 && _data.AnimalScareRadius > 0f)
            {
                ScareAnimals(center, _data.AnimalScareRadius, _data.AnimalScareDuration);
            }

            Despawn();
        }

        private void StunPlayers(PlayerId thrower, Vector3 center, float blastRadius)
        {
            int count = Physics.OverlapSphereNonAlloc(center, blastRadius, s_playerHits, _playerMask, QueryTriggerInteraction.Ignore);
            // Collected first and applied after the loop: ReceiveStun drops objects and raises events. A player may have
            // several colliders, so each player is stunned at most once.
            IStunnable player1 = null;
            IStunnable player2 = null;
            for (int i = 0; i < count; i++)
            {
                Collider hit = s_playerHits[i];
                s_playerHits[i] = null;
                Rigidbody body = hit.attachedRigidbody;
                if (body == null || !body.TryGetComponent(out IStunnable stunnable))
                {
                    continue;
                }

                PlayerId target = stunnable.Id;
                if ((target == PlayerId.Player1 && player1 != null) || (target == PlayerId.Player2 && player2 != null))
                {
                    continue;
                }

                float sqrDistance = (hit.ClosestPoint(center) - center).sqrMagnitude;
                if (!BombBlastRules.ShouldStun(thrower, target, sqrDistance, blastRadius))
                {
                    continue;
                }

                if (target == PlayerId.Player1)
                {
                    player1 = stunnable;
                }
                else if (target == PlayerId.Player2)
                {
                    player2 = stunnable;
                }
            }

            player1?.ReceiveStun(thrower, center, _data.StunDuration, _data.KnockbackSpeed);
            player2?.ReceiveStun(thrower, center, _data.StunDuration, _data.KnockbackSpeed);
        }

        private void ScareAnimals(Vector3 center, float radius, float duration)
        {
            int count = Physics.OverlapSphereNonAlloc(center, radius, s_animalHits, _animalMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider hit = s_animalHits[i];
                s_animalHits[i] = null;
                Rigidbody body = hit.attachedRigidbody;
                if (body != null && body.TryGetComponent(out IScareable scareable))
                {
                    scareable.Scare(center, duration);
                }
            }
        }

        private void SnapToHoldPoint()
        {
            if (_holdPoint == null)
            {
                return;
            }

            Vector3 offset = _data != null ? _data.HoldOffset : Vector3.zero;
            transform.SetPositionAndRotation(_holdPoint.TransformPoint(offset), Quaternion.identity);
        }

        private void SetIndicator(bool active)
        {
            if (_armedIndicator != null && _armedIndicator.activeSelf != active)
            {
                _armedIndicator.SetActive(active);
            }
        }

        private float ArmedBlinkInterval => _data != null ? _data.ArmedBlinkInterval : 0.12f;

        private float MaxArmedTime => _data != null ? _data.MaxArmedTime : 4f;

        private void CacheSetup()
        {
            if (_cached)
            {
                return;
            }

            _cached = true;
            if (_rb == null)
            {
                _rb = GetComponent<Rigidbody>();
            }

            if (_collider == null)
            {
                TryGetComponent(out _collider);
            }

            _playerMask = LayerMask.GetMask(LayerNames.Player);
            _animalMask = LayerMask.GetMask(LayerNames.Animal);
            _playerLayer = LayerMask.NameToLayer(LayerNames.Player);

            // Read once from the prefab's collider (it is disabled while carried, so bounds would be empty).
            if (_collider != null)
            {
                Vector3 scale = transform.lossyScale;
                float planarScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
                _radius = _collider.radius * Mathf.Max(planarScale, Mathf.Abs(scale.y));
            }
        }

        private void OnValidate()
        {
            if (_rb == null)
            {
                TryGetComponent(out _rb);
            }

            if (_collider == null)
            {
                TryGetComponent(out _collider);
            }

            if (_rb != null)
            {
                if (_rb.linearDamping > Epsilon)
                {
                    Debug.LogWarning($"[{nameof(Bomb)}] '{name}' has linear damping {_rb.linearDamping}; it must be 0 so the flight matches the throw preview.", this);
                }

                if (!_rb.useGravity)
                {
                    Debug.LogWarning($"[{nameof(Bomb)}] '{name}' Rigidbody does not use gravity; throws will not follow the preview.", this);
                }
            }

            if (_collider == null)
            {
                Debug.LogWarning($"[{nameof(Bomb)}] Missing SphereCollider on '{name}'.", this);
            }
            else if (Mathf.Abs(_collider.center.y - _collider.radius) > Epsilon)
            {
                Debug.LogWarning($"[{nameof(Bomb)}] '{name}' collider center.y should equal its radius (pivot at the base).", this);
            }

            int bombLayer = LayerMask.NameToLayer(LayerNames.Bomb);
            if (bombLayer < 0)
            {
                Debug.LogWarning($"[{nameof(Bomb)}] Layer '{LayerNames.Bomb}' does not exist; add it in Tags and Layers.", this);
            }
            else if (gameObject.layer != bombLayer)
            {
                Debug.LogWarning($"[{nameof(Bomb)}] '{name}' root must be on the '{LayerNames.Bomb}' layer.", this);
            }

            if (_armedIndicator == null)
            {
                Debug.LogWarning($"[{nameof(Bomb)}] '{name}' has no armed indicator; armed bombs will not blink.", this);
            }
        }
    }
}
