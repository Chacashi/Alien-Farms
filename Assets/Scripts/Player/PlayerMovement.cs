using AlienFarms.Core;
using AlienFarms.Data;
using UnityEngine;

namespace AlienFarms.Player
{
    /// <summary>
    /// Camera-relative, Rigidbody-driven ground movement with walk/sprint speeds.
    /// The root never rotates; only <see cref="_visual"/> turns to face the move direction (or, while aiming, the
    /// camera's planar forward so the player strafes).
    /// Input comes from an <see cref="IPlayerCommandSource"/>. As a <see cref="SimulationRole.Replica"/> it does not move.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerMovement : MonoBehaviour, ISimulationRoleTarget
    {
        [Header("Config")]
        [SerializeField, Tooltip("Shared movement tuning.")]
        private PlayerSettings _settings;

        [Header("References")]
        // Name kept from when it was typed PlayerInputHandler, so existing prefab references stay wired.
        [SerializeField, Tooltip("Input source for this player: a component implementing IPlayerCommandSource (PlayerInputHandler).")]
        private MonoBehaviour _input;

        [SerializeField, Tooltip("Rigidbody on this root (rotation should be frozen).")]
        private Rigidbody _rb;

        [SerializeField, Tooltip("Transform whose yaw defines 'forward' for input (usually the camera rig).")]
        private Transform _cameraYawSource;

        [SerializeField, Tooltip("Child that holds the model and rotates towards the facing direction.")]
        private Transform _visual;

        private Vector3 _facing = Vector3.forward;
        private bool _isSprinting;
        private bool _isCarrying;
        private float _carryWalkMultiplier = 1f;
        private float _stunTimer;
        private bool _inputEnabled = true;
        private bool _isAiming;
        private IPlayerCommandSource _commands;
        private SimulationRole _role = SimulationRole.Authority;

        /// <summary>Last non-zero horizontal move direction, or the camera forward while aiming (normalized).</summary>
        public Vector3 Facing => _facing;

        public bool IsSprinting => _isSprinting;

        /// <summary>True while a knockback stun is active (input ignored).</summary>
        public bool IsStunned => _stunTimer > 0f;

        public bool IsCarrying => _isCarrying;

        /// <summary>False while the match freezes this player (no move/sprint input; knockback still applies).</summary>
        public bool IsInputEnabled => _inputEnabled;

        /// <summary>True while aiming a throw: faces the camera, strafes, slower walk, no sprint.</summary>
        public bool IsAiming => _isAiming;

        /// <summary>Current Rigidbody velocity (world space).</summary>
        public Vector3 Velocity => _rb != null ? _rb.linearVelocity : Vector3.zero;

        /// <summary>Seconds left of the current knockback stun (0 when not stunned).</summary>
        public float StunRemaining => _stunTimer > 0f ? _stunTimer : 0f;

        public SimulationRole Role => _role;

        /// <summary>Input this component reads (the serialized source unless one was injected).</summary>
        public IPlayerCommandSource CommandSource => _commands;

        private void Awake()
        {
            if (_rb == null)
            {
                _rb = GetComponent<Rigidbody>();
            }

            // An injected source (SetCommandSource before Awake) wins over the serialized one.
            if (_commands == null)
            {
                _commands = _input as IPlayerCommandSource;
            }

            if (_visual != null)
            {
                Vector3 visualForward = _visual.forward;
                visualForward.y = 0f;
                if (visualForward.sqrMagnitude > 0.0001f)
                {
                    _facing = visualForward.normalized;
                }
            }
        }

        private void FixedUpdate()
        {
            if (_settings == null || _commands == null || _role == SimulationRole.Replica)
            {
                return;
            }

            if (_stunTimer > 0f)
            {
                _stunTimer -= Time.fixedDeltaTime;
                _isSprinting = false;
                ApplyHorizontalTowards(Vector3.zero, _settings.StunDeceleration);
                return;
            }

            Vector2 move = _inputEnabled ? Vector2.ClampMagnitude(_commands.Move, 1f) : Vector2.zero;
            float magnitude = move.magnitude;

            GetPlanarBasis(out Vector3 forward, out Vector3 right);
            if (_isAiming)
            {
                // Strafe: keep facing where the camera looks, whatever the move direction.
                _facing = forward;
            }

            Vector3 direction = Vector3.zero;
            if (magnitude > 0.0001f)
            {
                direction = forward * move.y + right * move.x;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    direction.Normalize();
                    if (!_isAiming)
                    {
                        _facing = direction;
                    }
                }
            }

            // Carrying or aiming forbids sprinting; both slow the walk.
            _isSprinting = !_isCarrying && !_isAiming && _commands.IsSprintHeld && magnitude > 0.0001f;
            float speed = _isSprinting
                ? _settings.SprintSpeed
                : _settings.WalkSpeed
                  * (_isCarrying ? _carryWalkMultiplier : 1f)
                  * (_isAiming ? _settings.AimWalkMultiplier : 1f);
            Vector3 targetHorizontal = direction * (speed * magnitude);

            float rate = targetHorizontal.sqrMagnitude > 0.0001f ? _settings.Acceleration : _settings.Deceleration;
            ApplyHorizontalTowards(targetHorizontal, rate);
        }

        private void ApplyHorizontalTowards(Vector3 targetHorizontal, float rate)
        {
            Vector3 velocity = _rb.linearVelocity;
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            horizontal = Vector3.MoveTowards(horizontal, targetHorizontal, rate * Time.fixedDeltaTime);
            _rb.linearVelocity = new Vector3(horizontal.x, velocity.y, horizontal.z);
        }

        private void Update()
        {
            if (_visual == null || _settings == null)
            {
                return;
            }

            Quaternion target = Quaternion.LookRotation(_facing, Vector3.up);
            _visual.rotation = Quaternion.RotateTowards(_visual.rotation, target, _settings.RotationSpeed * Time.deltaTime);
        }

        /// <summary>Instantly moves the player and clears its velocity. Rotation is applied to the visual.</summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            // May be called from PlayerInputManager.onPlayerJoined before this Awake has run.
            if (_rb == null)
            {
                _rb = GetComponent<Rigidbody>();
            }

            _rb.position = position;
            transform.position = position;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _stunTimer = 0f;

            Vector3 forward = rotation * Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f)
            {
                _facing = forward.normalized;
            }

            if (_visual != null)
            {
                _visual.rotation = Quaternion.LookRotation(_facing, Vector3.up);
            }
        }

        /// <summary>Replaces the input source (e.g. a network-fed one). Null leaves the player without input.</summary>
        public void SetCommandSource(IPlayerCommandSource source)
        {
            _commands = source;
        }

        /// <summary>Replica: no input-driven movement in FixedUpdate (the position is applied from outside).</summary>
        public void SetSimulationRole(SimulationRole role)
        {
            _role = role;
            if (role == SimulationRole.Replica)
            {
                _isSprinting = false;
            }
        }

        /// <summary>Enables or ignores move/sprint input. While disabled the player decelerates to a stop.</summary>
        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled)
            {
                _isSprinting = false;
            }
        }

        /// <summary>Enters or leaves aim mode (driven by PlayerAim).</summary>
        public void SetAimMode(bool aiming)
        {
            _isAiming = aiming;
            if (aiming)
            {
                _isSprinting = false;
            }
        }

        /// <summary>While carrying: no sprint and walk speed scaled by <paramref name="walkMultiplier"/>.</summary>
        public void SetCarryLoad(float walkMultiplier)
        {
            _isCarrying = true;
            _carryWalkMultiplier = Mathf.Clamp01(walkMultiplier);
        }

        public void ClearCarryLoad()
        {
            _isCarrying = false;
            _carryWalkMultiplier = 1f;
        }

        /// <summary>
        /// Overwrites the horizontal velocity with <paramref name="knockbackVelocity"/> (Y is ignored) and ignores
        /// input for <paramref name="stunDuration"/> seconds while decelerating with StunDeceleration.
        /// </summary>
        public void ApplyKnockback(Vector3 knockbackVelocity, float stunDuration)
        {
            Vector3 velocity = _rb.linearVelocity;
            _rb.linearVelocity = new Vector3(knockbackVelocity.x, velocity.y, knockbackVelocity.z);
            _stunTimer = Mathf.Max(_stunTimer, stunDuration);
            _isSprinting = false;
        }

        private void GetPlanarBasis(out Vector3 forward, out Vector3 right)
        {
            Transform source = _cameraYawSource != null ? _cameraYawSource : transform;

            forward = source.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                // Looking straight up/down: fall back to the source's up projected on the ground.
                forward = source.up;
                forward.y = 0f;
            }

            forward.Normalize();
            right = new Vector3(forward.z, 0f, -forward.x);
        }

        private void OnValidate()
        {
            if (_rb == null)
            {
                TryGetComponent(out _rb);
            }

            if (_rb != null && _rb.interpolation == RigidbodyInterpolation.None)
            {
                Debug.LogWarning($"[{nameof(PlayerMovement)}] Rigidbody on '{name}' has no interpolation; the camera will jitter. Set it to Interpolate.", this);
            }

            if (_settings == null)
            {
                Debug.LogWarning($"[{nameof(PlayerMovement)}] Missing PlayerSettings on '{name}'.", this);
            }

            if (_input == null)
            {
                Debug.LogWarning($"[{nameof(PlayerMovement)}] Missing input source on '{name}'.", this);
            }
            else if (!(_input is IPlayerCommandSource))
            {
                Debug.LogWarning($"[{nameof(PlayerMovement)}] Input source '{_input.GetType().Name}' on '{name}' does not implement {nameof(IPlayerCommandSource)}.", this);
            }

            if (_cameraYawSource == null)
            {
                Debug.LogWarning($"[{nameof(PlayerMovement)}] Missing camera yaw source on '{name}'; root forward will be used.", this);
            }

            if (_visual == null)
            {
                Debug.LogWarning($"[{nameof(PlayerMovement)}] Missing visual transform on '{name}'.", this);
            }
        }
    }
}
