using AlienFarms.Core;
using AlienFarms.Data;
using UnityEngine;

namespace AlienFarms.Player
{
    /// <summary>
    /// Hold-to-carry grabbing: while Grab is held with empty hands, picks the closest <see cref="IGrabbable"/>
    /// in a cone in front of the player (also catching ones that walk into range, free or penned). Releasing Grab drops
    /// it in front, or throws it along the aimed arc while <see cref="PlayerAim"/> is aiming. Carries at most one object.
    /// With <see cref="ReleaseMode.External"/> the release is decided from outside (<see cref="ReleaseHeld"/>,
    /// <see cref="ThrowWithView"/>) instead of by the Grab button. As a <see cref="SimulationRole.Replica"/> it never
    /// grabs, drops or throws: it only mirrors the held object set with <see cref="SetReplicatedHeld"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerGrabber : MonoBehaviour, ISimulationRoleTarget
    {
        private const float DropHeight = 0.6f;
        private const float DropClearanceRadius = 0.3f;

        // A target whose collider bottom is this far above the player's feet stands on something (fence, engine, disc):
        // its line of sight is retried once from the hold point, over the obstacle it stands on (m).
        private const float ElevatedTargetHeight = 0.5f;
        private const float Epsilon = 0.0001f;

        [Header("Config")]
        [SerializeField, Tooltip("Shared grab tuning.")]
        private PlayerSettings _settings;

        [Header("References")]
        // Name kept from when it was typed PlayerInputHandler, so existing prefab references stay wired.
        [SerializeField, Tooltip("Input source for this player: a component implementing IPlayerCommandSource (PlayerInputHandler).")]
        private MonoBehaviour _input;

        [SerializeField, Tooltip("Identity of this player (holder id).")]
        private PlayerIdentity _identity;

        [SerializeField, Tooltip("Movement on this root (facing, carry load, stun).")]
        private PlayerMovement _movement;

        [SerializeField, Tooltip("Point the carried object follows (child of the visual, above the head).")]
        private Transform _holdPoint;

        [SerializeField, Tooltip("Optional: aim on this root. When aiming, releasing Grab throws instead of dropping.")]
        private PlayerAim _aim;

        [SerializeField, Tooltip("Layers searched for grabbables (Animal, CapturedAnimal and Bomb).")]
        private LayerMask _grabbableMask;

        private readonly Collider[] _hits = new Collider[8];
        private int _environmentMask;
        private int _dropBlockingMask;
        private IGrabbable _held;
        private float _nextRetryTime;
        private bool _requiresRepress;
        private bool _releaseRequested;
        private bool _inputEnabled = true;
        private bool _paused;
        private IPlayerCommandSource _commands;
        private bool _subscribed;
        private ReleaseMode _releaseMode = ReleaseMode.Auto;
        private SimulationRole _role = SimulationRole.Authority;

        // Frame of the last unpause. Re-enabled actions only report their held state on the next input update (and
        // FixedUpdate runs before it), so the "lost cancel" poll is skipped until then.
        private int _resumeFrame = int.MinValue;

        public bool IsHolding => _held != null;
        public IGrabbable Held => _held;

        /// <summary>False while the match freezes this player: nothing can be grabbed.</summary>
        public bool IsInputEnabled => _inputEnabled;

        public ReleaseMode ReleaseMode => _releaseMode;

        public SimulationRole Role => _role;

        private PlayerId Id => _identity != null ? _identity.Id : PlayerId.None;

        private void Awake()
        {
            _environmentMask = LayerMask.GetMask(LayerNames.Environment);
            _dropBlockingMask = LayerMask.GetMask(
                LayerNames.Environment, LayerNames.Player, LayerNames.Animal, LayerNames.CapturedAnimal, LayerNames.Bomb);

            // An injected source (SetCommandSource before Awake) wins over the serialized one.
            if (_commands == null)
            {
                _commands = _input as IPlayerCommandSource;
            }
        }

        private void OnEnable()
        {
            _requiresRepress = false;
            _releaseRequested = false;
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();

            // A player leaving (or being disabled) must not keep an animal frozen at its hold point.
            ForceDrop(DropReason.Forced, Vector3.zero);
        }

        private void FixedUpdate()
        {
            if (_role == SimulationRole.Replica)
            {
                return;
            }

            if (_held != null)
            {
                // Released externally (despawn, capture...): clear our side.
                if (IsHeldDestroyed() || _held.Holder != Id)
                {
                    _held = null;
                    ClearCarryLoad();
                }
                else if (_releaseMode == ReleaseMode.Auto
                         && (_releaseRequested || (!_paused && !IsInResumeGrace() && _commands != null && !_commands.IsGrabHeld)))
                {
                    // Grab released (deferred from HandleGrabCanceled), or 'canceled' was missed (focus loss...).
                    ReleaseHeld();
                }

                return;
            }

            if (_paused || IsInResumeGrace())
            {
                return;
            }

            // A release missed while paused (its 'canceled' is ignored) still re-arms grabbing.
            if (_requiresRepress && _commands != null && !_commands.IsGrabHeld)
            {
                _requiresRepress = false;
            }

            if (!_inputEnabled || _settings == null || _commands == null || _requiresRepress || !_commands.IsGrabHeld)
            {
                return;
            }

            if (_movement != null && _movement.IsStunned)
            {
                return;
            }

            if (Time.time >= _nextRetryTime)
            {
                _nextRetryTime = Time.time + _settings.GrabRetryInterval;
                TryAcquire();
            }
        }

        /// <summary>Drops the carried object (if any) in front of the player, adding <paramref name="extraVelocity"/>.</summary>
        public void ForceDrop(DropReason reason, Vector3 extraVelocity)
        {
            // The authority drops the object; a replica only forgets its mirrored reference.
            if (_role == SimulationRole.Replica)
            {
                ClearReplicatedHeld();
                return;
            }

            bool wasHolding = _held != null;
            Drop(reason, extraVelocity);

            // After a steal or a blast the button must be released and pressed again to grab (only if something was
            // knocked out of the hands: empty hands keep grabbing as soon as the stun ends).
            if (wasHolding && IsKnockedOut(reason))
            {
                _requiresRepress = true;
            }
        }

        /// <summary>
        /// Disabling drops whatever is carried (Forced) and blocks grabbing. Re-enabling requires releasing Grab first
        /// if it is still held, so a button held through a freeze does not grab instantly.
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled)
            {
                ForceDrop(DropReason.Forced, Vector3.zero);
                return;
            }

            _requiresRepress = _commands != null && _commands.IsGrabHeld;
        }

        /// <summary>Replaces the input source (e.g. a network-fed one), moving the Grab subscriptions to it.</summary>
        public void SetCommandSource(IPlayerCommandSource source)
        {
            if (ReferenceEquals(source, _commands))
            {
                return;
            }

            bool resubscribe = _subscribed;
            Unsubscribe();
            _commands = source;
            if (resubscribe)
            {
                Subscribe();
            }
        }

        /// <summary>Auto (default): the Grab button releases. External: only ReleaseHeld / ThrowWithView do.</summary>
        public void SetReleaseMode(ReleaseMode mode)
        {
            _releaseMode = mode;
            if (mode == ReleaseMode.External)
            {
                _releaseRequested = false;
            }
        }

        /// <summary>
        /// Lets go of the carried object as releasing Grab would: thrown along the current aim when a throw is possible,
        /// otherwise dropped (Released). No-op with empty hands.
        /// </summary>
        public void ReleaseHeld()
        {
            if (_held == null || _role == SimulationRole.Replica)
            {
                return;
            }

            // The launch is read while still holding: aiming requires a held throwable.
            if (_aim != null && _aim.TryGetLaunch(out Vector3 origin, out Vector3 velocity))
            {
                Throw(origin, velocity);
            }
            else
            {
                Drop(DropReason.Released, Vector3.zero);
            }
        }

        /// <summary>
        /// Lets go of the carried object with the throw computed from an explicit view (deg) instead of the camera's.
        /// Falls back to a normal drop when no throw is possible (not aiming, wall in between...). True if thrown.
        /// </summary>
        public bool ThrowWithView(float yaw, float pitch)
        {
            if (_held == null || _role == SimulationRole.Replica)
            {
                return false;
            }

            if (_aim != null && _aim.TryGetLaunchWithView(yaw, pitch, out Vector3 origin, out Vector3 velocity))
            {
                Throw(origin, velocity);
                return true;
            }

            Drop(DropReason.Released, Vector3.zero);
            return false;
        }

        /// <summary>
        /// Replica: mirrors what this player carries without grabbing it (the authority already did). Only updates the
        /// held reference and the carry load; never calls TryGrab / Release on the object.
        /// </summary>
        public void SetReplicatedHeld(IGrabbable held)
        {
            if (_role != SimulationRole.Replica)
            {
                return;
            }

            if (IsDestroyed(held))
            {
                held = null;
            }

            _held = held;
            _releaseRequested = false;
            if (held != null && _movement != null)
            {
                _movement.SetCarryLoad(held.CarrySpeedMultiplier);
            }
            else
            {
                ClearCarryLoad();
            }
        }

        /// <summary>
        /// Replica: no acquisition, no physical drops (ForceDrop / disable only clear the mirrored reference).
        /// Switching roles forgets whatever was held under the previous one.
        /// </summary>
        public void SetSimulationRole(SimulationRole role)
        {
            if (role == _role)
            {
                return;
            }

            if (role == SimulationRole.Replica)
            {
                // Leaving authority: let go physically first so nothing stays frozen at the hold point.
                ForceDrop(DropReason.Forced, Vector3.zero);
            }
            else
            {
                ClearReplicatedHeld();
            }

            _role = role;
            _releaseRequested = false;
            _requiresRepress = false;
        }

        private void ClearReplicatedHeld()
        {
            _held = null;
            _releaseRequested = false;
            ClearCarryLoad();
        }

        private void Subscribe()
        {
            if (_subscribed || _commands == null)
            {
                return;
            }

            _commands.GrabStarted += HandleGrabStarted;
            _commands.GrabCanceled += HandleGrabCanceled;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _commands.GrabStarted -= HandleGrabStarted;
            _commands.GrabCanceled -= HandleGrabCanceled;
            _subscribed = false;
        }

        /// <summary>
        /// While paused, Grab 'canceled' (raised when the gameplay action map is disabled for the menu) is ignored, so the
        /// carried object is kept. On resume the held state is polled again (Grab uses initialStateCheck) and decides.
        /// </summary>
        public void SetPaused(bool paused)
        {
            if (_paused == paused)
            {
                return;
            }

            _paused = paused;
            if (!paused)
            {
                _resumeFrame = Time.frameCount;
            }
        }

        private void HandleGrabStarted()
        {
            if (_role == SimulationRole.Replica || _paused || !_inputEnabled || _held != null || _requiresRepress || (_movement != null && _movement.IsStunned))
            {
                return;
            }

            TryAcquire();
            if (_settings != null)
            {
                _nextRetryTime = Time.time + _settings.GrabRetryInterval;
            }
        }

        private void HandleGrabCanceled()
        {
            if (_paused)
            {
                return;
            }

            _requiresRepress = false;

            // Deferred to FixedUpdate on purpose: when the player is destroyed, PlayerInput cancels Grab before our
            // OnDisable runs, and at that point isActiveAndEnabled/activeInHierarchy are still true (verified in
            // Play Mode). Dropping here would report Released; deferring lets OnDisable drop with Forced first.
            if (_held != null && _releaseMode == ReleaseMode.Auto)
            {
                _releaseRequested = true;
            }
        }

        private void TryAcquire()
        {
            if (_settings == null || _holdPoint == null || _movement == null || Id == PlayerId.None)
            {
                return;
            }

            Vector3 origin = transform.position;
            // Line of sight starts at chest height (same height the drop uses) so low walls and fences block it.
            Vector3 eye = origin + Vector3.up * DropHeight;
            Vector3 facing = _movement.Facing;
            float minDot = Mathf.Cos(_settings.GrabHalfAngle * Mathf.Deg2Rad);

            int count = Physics.OverlapSphereNonAlloc(origin, _settings.GrabRange, _hits, _grabbableMask, QueryTriggerInteraction.Ignore);

            IGrabbable best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider hit = _hits[i];
                Rigidbody body = hit.attachedRigidbody;
                _hits[i] = null;
                if (body == null)
                {
                    continue;
                }

                Vector3 toTarget = body.position - origin;
                toTarget.y = 0f;
                float sqr = toTarget.sqrMagnitude;
                if (sqr >= bestSqr)
                {
                    continue;
                }

                if (sqr > Epsilon && Vector3.Dot(facing, toTarget / Mathf.Sqrt(sqr)) < minDot)
                {
                    continue;
                }

                if (!HasLineOfSight(eye, origin.y, hit.bounds))
                {
                    continue;
                }

                // Resolving a physics query result (<= 10 Hz while the button is held), equivalent to a physics event.
                if (!body.TryGetComponent(out IGrabbable grabbable) || !grabbable.CanBeGrabbed)
                {
                    continue;
                }

                best = grabbable;
                bestSqr = sqr;
            }

            if (best != null && best.TryGrab(Id, _holdPoint))
            {
                _held = best;
                _movement.SetCarryLoad(best.CarrySpeedMultiplier);
            }
        }

        /// <summary>
        /// No grabbing through fences, obstacles or walls (Environment). Pen barriers (ZoneBarrier) never block; an animal
        /// pressed against a wall is reachable because the line ends at its center, before the wall. An animal standing
        /// on an obstacle gets a second line from the hold point, which passes over the obstacle under it. Animals on the
        /// floor never get the retry, so a fence between player and animal always blocks.
        /// </summary>
        private bool HasLineOfSight(Vector3 eye, float feetHeight, Bounds target)
        {
            Vector3 center = target.center;
            if (!Physics.Linecast(eye, center, _environmentMask, QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            if (target.min.y - feetHeight < ElevatedTargetHeight)
            {
                return false;
            }

            return !Physics.Linecast(_holdPoint.position, center, _environmentMask, QueryTriggerInteraction.Ignore);
        }

        private void Drop(DropReason reason, Vector3 extraVelocity)
        {
            if (_held == null)
            {
                return;
            }

            IGrabbable held = _held;
            _held = null;
            _releaseRequested = false;

            if (IsDestroyed(held))
            {
                ClearCarryLoad();
                return;
            }

            // On a steal or a blast, throw along the push direction (away from the attacker), not where the victim faced.
            Vector3 dropDirection = _movement != null ? _movement.Facing : transform.forward;
            if (IsKnockedOut(reason))
            {
                Vector3 tackleDirection = new Vector3(extraVelocity.x, 0f, extraVelocity.z);
                if (tackleDirection.sqrMagnitude > Epsilon)
                {
                    dropDirection = tackleDirection;
                }
            }

            dropDirection.y = 0f;
            if (dropDirection.sqrMagnitude < Epsilon)
            {
                dropDirection = Vector3.forward;
            }

            dropDirection.Normalize();

            Vector3 root = transform.position;
            Vector3 handPosition = _holdPoint != null ? _holdPoint.position : root + Vector3.up * DropHeight;
            Vector3 velocity = new Vector3(0f, extraVelocity.y, 0f);
            Vector3 position;

            // Never push the animal into a wall, another player or another animal: try in front, behind, left, right.
            if (TryFindDropSpot(root, handPosition, dropDirection, out position, out Vector3 chosen))
            {
                if (chosen == dropDirection)
                {
                    // Only the preferred direction keeps the extra push (e.g. the tackle).
                    velocity = extraVelocity;
                }

                if (_settings != null)
                {
                    velocity += chosen * _settings.DropForwardSpeed;
                }
            }
            else
            {
                // Boxed in on all sides: last resort, release at the hand (it slides off the head if needed).
                position = handPosition;
            }

            if (_settings != null)
            {
                velocity += Vector3.up * _settings.DropUpSpeed;
            }

            held.Release(position, velocity, reason);
            ClearCarryLoad();
        }

        /// <summary>
        /// First free drop spot at drop distance and height around the player: <paramref name="preferred"/>, then behind,
        /// left and right. A spot is free when nothing overlaps it and no wall stands between the hand and it.
        /// </summary>
        private bool TryFindDropSpot(
            Vector3 root, Vector3 handPosition, Vector3 preferred, out Vector3 position, out Vector3 direction)
        {
            float distance = _settings != null ? _settings.DropDistance : 1f;
            Vector3 right = new Vector3(preferred.z, 0f, -preferred.x);

            for (int i = 0; i < 4; i++)
            {
                direction = i == 0 ? preferred : i == 1 ? -preferred : i == 2 ? -right : right;
                position = root + direction * distance;
                position.y = root.y + DropHeight;

                if (!IsDropSpotBlocked(position)
                    && !Physics.Linecast(handPosition, position, _environmentMask, QueryTriggerInteraction.Ignore))
                {
                    return true;
                }
            }

            position = Vector3.zero;
            direction = Vector3.zero;
            return false;
        }

        /// <summary>
        /// Launches the carried object from <paramref name="origin"/> with <paramref name="velocity"/>, both from
        /// <see cref="PlayerAim.TryGetLaunch"/> (already corrected for walls, same values the preview draws).
        /// </summary>
        private void Throw(Vector3 origin, Vector3 velocity)
        {
            IGrabbable held = _held;
            _held = null;
            _releaseRequested = false;

            if (IsDestroyed(held))
            {
                ClearCarryLoad();
                return;
            }

            held.Release(origin, velocity, DropReason.Thrown);
            ClearCarryLoad();
        }

        /// <summary>True if the drop spot overlaps the environment, another player or an animal (own colliders ignored).</summary>
        private bool IsDropSpotBlocked(Vector3 position)
        {
            int count = Physics.OverlapSphereNonAlloc(position, DropClearanceRadius, _hits, _dropBlockingMask, QueryTriggerInteraction.Ignore);
            bool blocked = false;
            for (int i = 0; i < count; i++)
            {
                Collider hit = _hits[i];
                _hits[i] = null;
                Rigidbody body = hit.attachedRigidbody;
                if (body != null && body.transform == transform)
                {
                    continue;
                }

                blocked = true;
            }

            return blocked;
        }

        private void ClearCarryLoad()
        {
            if (_movement != null)
            {
                _movement.ClearCarryLoad();
            }
        }

        private bool IsHeldDestroyed() => IsDestroyed(_held);

        private bool IsInResumeGrace() => Time.frameCount <= _resumeFrame + 1;

        // Editor-time default only; runtime never resolves layers by name for this mask.
        private void Reset()
        {
            _grabbableMask = LayerMask.GetMask(LayerNames.Animal, LayerNames.CapturedAnimal, LayerNames.Bomb);
        }

        /// <summary>Drops caused by a hit (tackle or blast): pushed along the hit and require re-pressing Grab.</summary>
        private static bool IsKnockedOut(DropReason reason) => reason == DropReason.Stolen || reason == DropReason.Stunned;

        // Interface references bypass Unity's overloaded null check.
        private static bool IsDestroyed(IGrabbable grabbable) => grabbable is Object unityObject && unityObject == null;

        private void OnValidate()
        {
            if (_settings == null)
            {
                Debug.LogWarning($"[{nameof(PlayerGrabber)}] Missing PlayerSettings on '{name}'.", this);
            }

            if (_input == null)
            {
                Debug.LogWarning($"[{nameof(PlayerGrabber)}] Missing input source on '{name}'.", this);
            }
            else if (!(_input is IPlayerCommandSource))
            {
                Debug.LogWarning($"[{nameof(PlayerGrabber)}] Input source '{_input.GetType().Name}' on '{name}' does not implement {nameof(IPlayerCommandSource)}.", this);
            }

            if (_identity == null)
            {
                TryGetComponent(out _identity);
            }

            if (_movement == null)
            {
                TryGetComponent(out _movement);
            }

            if (_holdPoint == null)
            {
                Debug.LogWarning($"[{nameof(PlayerGrabber)}] Missing hold point on '{name}'.", this);
            }

            if (_aim == null)
            {
                TryGetComponent(out _aim);
            }

            if (_grabbableMask.value == 0)
            {
                Debug.LogWarning($"[{nameof(PlayerGrabber)}] Grabbable mask on '{name}' is empty; nothing can be grabbed (use Animal + CapturedAnimal + Bomb).", this);
            }
        }
    }
}
