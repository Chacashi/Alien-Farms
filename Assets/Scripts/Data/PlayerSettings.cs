using UnityEngine;

namespace AlienFarms.Data
{
    /// <summary>Tunable movement and camera values shared by player characters.</summary>
    [CreateAssetMenu(fileName = "PlayerSettings", menuName = "Alien Farms/Player Settings")]
    public sealed class PlayerSettings : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField, Min(0f), Tooltip("Ground speed while walking (m/s).")]
        private float _walkSpeed = 5f;

        [SerializeField, Min(0f), Tooltip("Ground speed while sprinting (m/s).")]
        private float _sprintSpeed = 8.5f;

        [SerializeField, Min(0f), Tooltip("Horizontal acceleration towards the target speed (m/s^2).")]
        private float _acceleration = 40f;

        [SerializeField, Min(0f), Tooltip("Horizontal deceleration when input is released (m/s^2).")]
        private float _deceleration = 50f;

        [SerializeField, Min(0f), Tooltip("How fast the visual turns towards the move direction (deg/s).")]
        private float _rotationSpeed = 720f;

        [Header("Camera")]
        [SerializeField, Min(0f), Tooltip("Degrees per pixel of mouse delta (not scaled by deltaTime).")]
        private float _mouseSensitivity = 0.12f;

        [SerializeField, Tooltip("Gamepad stick sensitivity in deg/s (x = yaw, y = pitch).")]
        private Vector2 _stickSensitivity = new Vector2(200f, 120f);

        [SerializeField, Tooltip("Invert vertical look.")]
        private bool _invertY;

        [SerializeField, Range(-89f, 89f), Tooltip("Lowest pitch angle (deg).")]
        private float _minPitch = -30f;

        [SerializeField, Range(-89f, 89f), Tooltip("Highest pitch angle (deg).")]
        private float _maxPitch = 70f;

        [SerializeField, Range(-89f, 89f), Tooltip("Pitch used when the camera initializes (deg).")]
        private float _defaultPitch = 20f;

        [SerializeField, Min(0.5f), Tooltip("Desired distance from the camera to its target (m).")]
        private float _distance = 6f;

        [SerializeField, Min(0f), Tooltip("Sphere radius used to keep the camera out of geometry (m).")]
        private float _collisionRadius = 0.25f;

        public float WalkSpeed => _walkSpeed;
        public float SprintSpeed => _sprintSpeed;
        public float Acceleration => _acceleration;
        public float Deceleration => _deceleration;
        public float RotationSpeed => _rotationSpeed;
        public float MouseSensitivity => _mouseSensitivity;
        public Vector2 StickSensitivity => _stickSensitivity;
        public bool InvertY => _invertY;
        public float MinPitch => _minPitch;
        public float MaxPitch => _maxPitch;
        public float DefaultPitch => _defaultPitch;
        public float Distance => _distance;
        public float CollisionRadius => _collisionRadius;

        private void OnValidate()
        {
            if (_sprintSpeed < _walkSpeed)
            {
                _sprintSpeed = _walkSpeed;
            }

            if (_maxPitch < _minPitch)
            {
                _maxPitch = _minPitch;
            }

            _defaultPitch = Mathf.Clamp(_defaultPitch, _minPitch, _maxPitch);
        }
    }
}
