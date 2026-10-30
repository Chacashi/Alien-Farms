using UnityEngine;
using UnityEngine.Serialization;

namespace AlienFarms.Data
{
    /// <summary>Tunable movement, camera, grab, aim and tackle values shared by player characters.</summary>
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

        [Header("Grab")]
        [SerializeField, Min(0f), Tooltip("Reach of the grab query from the player root (m).")]
        private float _grabRange = 1.6f;

        [SerializeField, Range(0f, 180f), Tooltip("Half angle of the grab cone around the facing direction (deg).")]
        private float _grabHalfAngle = 60f;

        [SerializeField, Min(0.02f), Tooltip("While Grab is held with empty hands, seconds between grab attempts.")]
        private float _grabRetryInterval = 0.1f;

        [SerializeField, Min(0f), Tooltip("Distance in front of the player where a released animal is placed (m).")]
        private float _dropDistance = 1.2f;

        [SerializeField, Min(0f), Tooltip("Forward speed given to a released animal (m/s).")]
        private float _dropForwardSpeed = 2f;

        [SerializeField, Min(0f), Tooltip("Upward speed given to a released animal (m/s).")]
        private float _dropUpSpeed = 2f;

        [Header("Aim")]
        [SerializeField, Range(0.1f, 1f), Tooltip("Walk speed multiplier while aiming a throw (stacks with the carry multiplier).")]
        private float _aimWalkMultiplier = 0.55f;

        [SerializeField, Min(0.5f), Tooltip("Camera distance to its target while aiming (m).")]
        private float _aimCameraDistance = 4.5f;

        [SerializeField, Min(0.01f), Tooltip("How fast the camera distance blends in and out of aim (1/s, exponential).")]
        private float _aimCameraBlendSpeed = 10f;

        [SerializeField, Range(0.1f, 1f), Tooltip("Look sensitivity multiplier while aiming.")]
        private float _aimLookSensitivityMultiplier = 0.7f;

        [Header("Tackle")]
        [SerializeField, Min(0f), FormerlySerializedAs("_tackleMinRelativeSpeed")]
        [Tooltip("Minimum closing speed (relative velocity along the line between both players) to steal (m/s).")]
        private float _tackleMinClosingSpeed = 2.5f;

        [SerializeField, Range(-1f, 1f), Tooltip("Min dot between the attacker's facing and the direction to the victim.")]
        private float _tackleFacingDot = 0.5f;

        [SerializeField, Min(0f), Tooltip("Horizontal speed applied to the tackled player (m/s).")]
        private float _tackleKnockbackSpeed = 6f;

        [SerializeField, Min(0f), Tooltip("Time the tackled player ignores input (s).")]
        private float _tackleStunDuration = 0.35f;

        [SerializeField, Min(0f), Tooltip("Horizontal deceleration while stunned (m/s^2).")]
        private float _stunDeceleration = 15f;

        [SerializeField, Min(0f), Tooltip("After being tackled, the player cannot be tackled again for this long (s).")]
        private float _tackleImmunity = 1f;

        [SerializeField, Min(0f), Tooltip("Extra speed given to a stolen animal in the tackle direction (m/s).")]
        private float _stealDropSpeed = 3f;

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
        public float GrabRange => _grabRange;
        public float GrabHalfAngle => _grabHalfAngle;
        public float GrabRetryInterval => _grabRetryInterval;
        public float DropDistance => _dropDistance;
        public float DropForwardSpeed => _dropForwardSpeed;
        public float DropUpSpeed => _dropUpSpeed;
        public float AimWalkMultiplier => _aimWalkMultiplier;
        public float AimCameraDistance => _aimCameraDistance;
        public float AimCameraBlendSpeed => _aimCameraBlendSpeed;
        public float AimLookSensitivityMultiplier => _aimLookSensitivityMultiplier;
        public float TackleMinClosingSpeed => _tackleMinClosingSpeed;
        public float TackleFacingDot => _tackleFacingDot;
        public float TackleKnockbackSpeed => _tackleKnockbackSpeed;
        public float TackleStunDuration => _tackleStunDuration;
        public float StunDeceleration => _stunDeceleration;
        public float TackleImmunity => _tackleImmunity;
        public float StealDropSpeed => _stealDropSpeed;

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
