using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Input;
using UnityEngine;

namespace AlienFarms.Player
{
    /// <summary>
    /// Hold-to-carry grabbing: while Grab is held with empty hands, picks the closest <see cref="IGrabbable"/>
    /// in a cone in front of the player (also catching ones that walk into range). Releasing Grab drops it in front.
    /// Carries at most one object.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerGrabber : MonoBehaviour
    {
        private const float DropHeight = 0.6f;
        private const float DropClearanceRadius = 0.3f;
        private const float Epsilon = 0.0001f;

        [Header("Config")]
        [SerializeField, Tooltip("Shared grab tuning.")]
        private PlayerSettings _settings;

        [Header("References")]
        [SerializeField, Tooltip("Input source for this player.")]
        private PlayerInputHandler _input;

        [SerializeField, Tooltip("Identity of this player (holder id).")]
        private PlayerIdentity _identity;

        [SerializeField, Tooltip("Movement on this root (facing, carry load, stun).")]
        private PlayerMovement _movement;

        [SerializeField, Tooltip("Point the carried object follows (child of the visual, above the head).")]
        private Transform _holdPoint;

        private readonly Collider[] _hits = new Collider[8];
        private int _animalMask;
        private int _environmentMask;
        private int _dropBlockingMask;
        private IGrabbable _held;
        private float _nextRetryTime;
        private bool _requiresRepress;
        private bool _releaseRequested;

        public bool IsHolding => _held != null;
        public IGrabbable Held => _held;

        private PlayerId Id => _identity != null ? _identity.Id : PlayerId.None;

        private void Awake()
        {
            _animalMask = LayerMask.GetMask(LayerNames.Animal);
            _environmentMask = LayerMask.GetMask(LayerNames.Environment);
            _dropBlockingMask = LayerMask.GetMask(LayerNames.Environment, LayerNames.Player, LayerNames.Animal);
        }

        private void OnEnable()
        {
            _requiresRepress = false;
            _releaseRequested = false;

            if (_input != null)
            {
                _input.GrabStarted += HandleGrabStarted;
                _input.GrabCanceled += HandleGrabCanceled;
            }
        }

        private void OnDisable()
        {
            if (_input != null)
            {
                _input.GrabStarted -= HandleGrabStarted;
                _input.GrabCanceled -= HandleGrabCanceled;
            }

            // A player leaving (or being disabled) must not keep an animal frozen at its hold point.
            ForceDrop(DropReason.Forced, Vector3.zero);
        }

        private void FixedUpdate()
        {
            if (_held != null)
            {
                // Released externally (despawn, capture...): clear our side.
                if (IsHeldDestroyed() || _held.Holder != Id)
                {
                    _held = null;
                    ClearCarryLoad();
                }
                else if (_releaseRequested || (_input != null && !_input.IsGrabHeld))
                {
                    // Grab released (deferred from HandleGrabCanceled), or 'canceled' was missed (focus loss...).
                    Drop(DropReason.Released, Vector3.zero);
                }

                return;
            }

            if (_settings == null || _input == null || _requiresRepress || !_input.IsGrabHeld)
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
            Drop(reason, extraVelocity);

            // After a steal the button must be released and pressed again to grab.
            if (reason == DropReason.Stolen)
            {
                _requiresRepress = true;
            }
        }

        private void HandleGrabStarted()
        {
            if (_held != null || _requiresRepress || (_movement != null && _movement.IsStunned))
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
            _requiresRepress = false;

            // Deferred to FixedUpdate on purpose: when the player is destroyed, PlayerInput cancels Grab before our
            // OnDisable runs, and at that point isActiveAndEnabled/activeInHierarchy are still true (verified in
            // Play Mode). Dropping here would report Released; deferring lets OnDisable drop with Forced first.
            if (_held != null)
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
            Vector3 facing = _movement.Facing;
            float minDot = Mathf.Cos(_settings.GrabHalfAngle * Mathf.Deg2Rad);

            int count = Physics.OverlapSphereNonAlloc(origin, _settings.GrabRange, _hits, _animalMask, QueryTriggerInteraction.Ignore);

            IGrabbable best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Rigidbody body = _hits[i].attachedRigidbody;
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

            // On a steal, throw along the tackle direction (away from the attacker), not where the victim faced.
            Vector3 dropDirection = _movement != null ? _movement.Facing : transform.forward;
            if (reason == DropReason.Stolen)
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
            Vector3 position = root + dropDirection * (_settings != null ? _settings.DropDistance : 1f);
            position.y = root.y + DropHeight;

            Vector3 velocity = extraVelocity;
            if (_settings != null)
            {
                velocity += dropDirection * _settings.DropForwardSpeed + Vector3.up * _settings.DropUpSpeed;
            }

            // Never push the animal into a wall, another player or another animal: drop it in place instead.
            Vector3 handPosition = _holdPoint != null ? _holdPoint.position : root + Vector3.up * DropHeight;
            if (IsDropSpotBlocked(position)
                || Physics.Linecast(handPosition, position, _environmentMask, QueryTriggerInteraction.Ignore))
            {
                position = handPosition;
                velocity.x = 0f;
                velocity.z = 0f;
            }

            held.Release(position, velocity, reason);
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
                Debug.LogWarning($"[{nameof(PlayerGrabber)}] Missing PlayerInputHandler on '{name}'.", this);
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
        }
    }
}
