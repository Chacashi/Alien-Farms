using UnityEngine;

namespace AlienFarms.Data
{
    /// <summary>Launch and trajectory-preview tuning for thrown objects.</summary>
    [CreateAssetMenu(fileName = "ThrowSettings", menuName = "Alien Farms/Throw Settings")]
    public sealed class ThrowSettings : ScriptableObject
    {
        [Header("Launch")]
        [SerializeField, Min(0f), Tooltip("Launch speed along the throw direction (m/s).")]
        private float _launchSpeed = 13f;

        [SerializeField, Range(0f, 85f), Tooltip("Elevation above the horizon when the camera is at the reference pitch (deg).")]
        private float _baseElevation = 30f;

        [SerializeField, Range(0f, 85f), Tooltip("Lowest elevation the camera pitch can produce (deg).")]
        private float _minElevation = 10f;

        [SerializeField, Range(0f, 85f), Tooltip("Highest elevation the camera pitch can produce (deg).")]
        private float _maxElevation = 55f;

        [SerializeField, Range(0f, 2f), Tooltip("Degrees of elevation gained per degree the camera looks up from the reference pitch.")]
        private float _pitchInfluence = 0.8f;

        [SerializeField, Range(-89f, 89f), Tooltip("Camera pitch that maps to the base elevation (deg, positive looks down).")]
        private float _referenceCameraPitch = 20f;

        [SerializeField, Range(0f, 1f), Tooltip("Fraction of the thrower's velocity added to the launch.")]
        private float _inheritVelocityFactor;

        [Header("Preview")]
        [SerializeField, Min(0.1f), Tooltip("Longest flight time drawn by the trajectory preview (s).")]
        private float _previewMaxTime = 2.5f;

        [SerializeField, Min(1), Tooltip("Physics steps simulated per preview point (and per collision check).")]
        private int _previewStepsPerPoint = 2;

        public float LaunchSpeed => _launchSpeed;
        public float BaseElevation => _baseElevation;
        public float MinElevation => _minElevation;
        public float MaxElevation => _maxElevation;
        public float PitchInfluence => _pitchInfluence;
        public float ReferenceCameraPitch => _referenceCameraPitch;
        public float InheritVelocityFactor => _inheritVelocityFactor;
        public float PreviewMaxTime => _previewMaxTime;
        public int PreviewStepsPerPoint => _previewStepsPerPoint;

        private void OnValidate()
        {
            if (_maxElevation < _minElevation)
            {
                _maxElevation = _minElevation;
            }

            _baseElevation = Mathf.Clamp(_baseElevation, _minElevation, _maxElevation);
        }
    }
}
