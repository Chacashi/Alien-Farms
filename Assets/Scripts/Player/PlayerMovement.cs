using AlienFarms.Data;
using AlienFarms.Input;
using UnityEngine;

namespace AlienFarms.Player
{
    /// <summary>
    /// Camera-relative, Rigidbody-driven ground movement with walk/sprint speeds.
    /// The root never rotates; only <see cref="_visual"/> turns to face the move direction.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Shared movement tuning.")]
        private PlayerSettings _settings;

        [Header("References")]
        [SerializeField, Tooltip("Input source for this player.")]
        private PlayerInputHandler _input;

        [SerializeField, Tooltip("Rigidbody on this root (rotation should be frozen).")]
        private Rigidbody _rb;

        [SerializeField, Tooltip("Transform whose yaw defines 'forward' for input (usually the camera rig).")]
        private Transform _cameraYawSource;

        [SerializeField, Tooltip("Child that holds the model and rotates towards the facing direction.")]
        private Transform _visual;

        private Vector3 _facing = Vector3.forward;
        private bool _isSprinting;

        /// <summary>Last non-zero horizontal move direction (normalized).</summary>
        public Vector3 Facing => _facing;

        public bool IsSprinting => _isSprinting;

        private void Awake()
        {
            if (_rb == null)
            {
                _rb = GetComponent<Rigidbody>();
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
            if (_settings == null || _input == null)
            {
                return;
            }

            Vector2 move = Vector2.ClampMagnitude(_input.Move, 1f);
            float magnitude = move.magnitude;

            Vector3 direction = Vector3.zero;
            if (magnitude > 0.0001f)
            {
                GetPlanarBasis(out Vector3 forward, out Vector3 right);
                direction = forward * move.y + right * move.x;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    direction.Normalize();
                    _facing = direction;
                }
            }

            _isSprinting = _input.IsSprintHeld && magnitude > 0.0001f;
            float speed = _isSprinting ? _settings.SprintSpeed : _settings.WalkSpeed;
            Vector3 targetHorizontal = direction * (speed * magnitude);

            Vector3 velocity = _rb.linearVelocity;
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            float rate = targetHorizontal.sqrMagnitude > 0.0001f ? _settings.Acceleration : _settings.Deceleration;
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
            _rb.position = position;
            transform.position = position;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;

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

        /// <summary>Applies an instantaneous velocity change (e.g. knockback).</summary>
        public void AddImpulse(Vector3 impulse)
        {
            _rb.AddForce(impulse, ForceMode.Impulse);
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
                Debug.LogWarning($"[{nameof(PlayerMovement)}] Missing PlayerInputHandler on '{name}'.", this);
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
