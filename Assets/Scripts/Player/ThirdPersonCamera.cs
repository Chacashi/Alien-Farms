using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Input;
using UnityEngine;

namespace AlienFarms.Player
{
    /// <summary>
    /// Orbit camera rig driven by this player's Look input. Lives on the CameraRig (child of the
    /// non-rotating player root); the camera is a child placed at local (0, 0, -distance).
    /// While aiming, the distance blends to the aim distance and look sensitivity is reduced.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        private const int MaxHits = 8;
        private const float MinCameraDistance = 0.1f;

        [Header("Config")]
        [SerializeField, Tooltip("Shared camera tuning.")]
        private PlayerSettings _settings;

        [Header("References")]
        [SerializeField, Tooltip("Input source for this player.")]
        private PlayerInputHandler _input;

        [SerializeField, Tooltip("Point the rig orbits around (e.g. a pivot at head height).")]
        private Transform _target;

        [SerializeField, Tooltip("Camera transform, child of this rig.")]
        private Transform _cameraTransform;

        [SerializeField, Tooltip("Layers that block the camera (Ground, Environment). Never include Player or Animal.")]
        private LayerMask _collisionMask;

        private readonly RaycastHit[] _hits = new RaycastHit[MaxHits];
        private float _yaw;
        private float _pitch;
        private bool _yawSnapped;
        private bool _isAiming;
        private float _currentDistance;

        /// <summary>Current orbit pitch (deg, positive looks down).</summary>
        public float Pitch => _pitch;

        /// <summary>Current orbit yaw (deg).</summary>
        public float Yaw => _yaw;

        /// <summary>Horizontal unit vector the camera looks along.</summary>
        public Vector3 PlanarForward => Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;

        public bool IsAiming => _isAiming;

        private void Awake()
        {
            // PlayerInputManager may call SnapYaw (via onPlayerJoined) before this Awake runs; keep that value.
            if (!_yawSnapped)
            {
                _yaw = transform.eulerAngles.y;
            }

            _pitch = _settings != null ? _settings.DefaultPitch : 0f;
            _currentDistance = _settings != null ? _settings.Distance : 0f;
        }

        private void LateUpdate()
        {
            if (_settings == null || _input == null || _target == null)
            {
                return;
            }

            Vector2 look = _input.Look;
            Vector2 delta = _input.IsLookPointerDelta
                ? look * _settings.MouseSensitivity
                : Vector2.Scale(look, _settings.StickSensitivity) * Time.deltaTime;

            if (_settings.InvertY)
            {
                delta.y = -delta.y;
            }

            if (_isAiming)
            {
                delta *= _settings.AimLookSensitivityMultiplier;
            }

            // Exponential blend: frame-rate independent and never overshoots.
            float targetDistance = _isAiming ? _settings.AimCameraDistance : _settings.Distance;
            float blend = 1f - Mathf.Exp(-_settings.AimCameraBlendSpeed * Time.deltaTime);
            _currentDistance = Mathf.Lerp(_currentDistance, targetDistance, blend);

            _yaw = Mathf.Repeat(_yaw + delta.x, 360f);
            _pitch = Mathf.Clamp(_pitch - delta.y, _settings.MinPitch, _settings.MaxPitch);

            Vector3 pivot = _target.position;
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            transform.SetPositionAndRotation(pivot, rotation);

            if (_cameraTransform != null)
            {
                float distance = ResolveDistance(pivot, rotation * Vector3.back, _currentDistance);
                _cameraTransform.localPosition = new Vector3(0f, 0f, -distance);
                _cameraTransform.localRotation = Quaternion.identity;
            }
        }

        /// <summary>Enters or leaves aim mode (driven by PlayerAim).</summary>
        public void SetAimMode(bool aiming)
        {
            _isAiming = aiming;
        }

        /// <summary>Sets the orbit yaw instantly (e.g. after a respawn).</summary>
        public void SnapYaw(float yaw)
        {
            _yaw = Mathf.Repeat(yaw, 360f);
            _yawSnapped = true;

            // Apply now so movement (which reads this rig's yaw) is correct before the next LateUpdate.
            Vector3 euler = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(euler.x, _yaw, 0f);
        }

        private float ResolveDistance(Vector3 origin, Vector3 direction, float desired)
        {
            int count = Physics.SphereCastNonAlloc(
                origin, _settings.CollisionRadius, direction, _hits, desired, _collisionMask, QueryTriggerInteraction.Ignore);

            float closest = desired;
            for (int i = 0; i < count; i++)
            {
                // Colliders overlapping the sphere at the origin report distance 0; ignore them.
                float hitDistance = _hits[i].distance;
                if (hitDistance > 0f && hitDistance < closest)
                {
                    closest = hitDistance;
                }
            }

            return Mathf.Max(closest, MinCameraDistance);
        }

        // Editor-time default only; runtime never resolves layers by name.
        private void Reset()
        {
            _collisionMask = LayerMask.GetMask(LayerNames.Ground, LayerNames.Environment);
        }

        private void OnValidate()
        {
            int forbidden = LayerMask.GetMask(LayerNames.Player, LayerNames.Animal);
            if ((_collisionMask.value & forbidden) != 0)
            {
                Debug.LogWarning($"[{nameof(ThirdPersonCamera)}] Collision mask on '{name}' includes the Player or Animal layer; the camera will collide with characters.", this);
            }

            if (_collisionMask.value == 0)
            {
                Debug.LogWarning($"[{nameof(ThirdPersonCamera)}] Collision mask on '{name}' is empty; the camera will clip through geometry.", this);
            }

            if (_settings == null)
            {
                Debug.LogWarning($"[{nameof(ThirdPersonCamera)}] Missing PlayerSettings on '{name}'.", this);
            }

            if (_input == null)
            {
                Debug.LogWarning($"[{nameof(ThirdPersonCamera)}] Missing PlayerInputHandler on '{name}'.", this);
            }

            if (_target == null)
            {
                Debug.LogWarning($"[{nameof(ThirdPersonCamera)}] Missing target on '{name}'.", this);
            }

            if (_cameraTransform == null)
            {
                Debug.LogWarning($"[{nameof(ThirdPersonCamera)}] Missing camera transform on '{name}'.", this);
            }
        }
    }
}
