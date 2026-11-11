using UnityEngine;

namespace AlienFarms.Data
{
    /// <summary>
    /// Tuning of the online mode (sessions, input, interpolation, prediction and lag compensation). Primitive fields only,
    /// so it lives in the runtime assembly and is read by the networking layer.
    /// </summary>
    [CreateAssetMenu(fileName = "NetworkSettings", menuName = "Alien Farms/Networking/Network Settings")]
    public sealed class NetworkSettings : ScriptableObject
    {
        [Header("Session")]
        [SerializeField, Min(1), Tooltip("Connection protocol version; clients with a different one are rejected.")]
        private int _protocolVersion = 1;

        [SerializeField, Tooltip("Unity Gaming Services environment name.")]
        private string _environmentName = "dev";

        [SerializeField, Range(2, 3), Tooltip("Maximum connections per session (2 players, plus a spectator when allowed).")]
        private int _maxPlayers = 2;

        [SerializeField, Tooltip("Accept a third connection as a spectator instead of answering 'Room full'.")]
        private bool _allowSpectator;

        [SerializeField, Min(0f), Tooltip("Seconds a disconnected player keeps its slot and pose while it reconnects.")]
        private float _reconnectGraceSeconds = 20f;

        [Header("Input")]
        [SerializeField, Range(1, 8), Tooltip("Commands sent per input packet (the latest plus previous ones, against loss).")]
        private int _inputRedundancy = 3;

        [SerializeField, Range(0, 8), Tooltip("Commands the server buffers per client before consuming one per physics step (jitter buffer).")]
        private int _inputBufferTarget = 2;

        [Header("Interpolation and prediction")]
        [SerializeField, Range(0f, 0.5f), Tooltip("Delay applied to remote snapshots before interpolating them (s).")]
        private float _interpolationDelay = 0.1f;

        [SerializeField, Min(0f), Tooltip("Position error above which the owner's prediction is reconciled (m).")]
        private float _reconcileThreshold = 0.05f;

        [SerializeField, Min(0f), Tooltip("Rate at which the visual catches up after a reconciliation (1/s, exponential).")]
        private float _visualSmoothingRate = 15f;

        [SerializeField, Tooltip("Client-side prediction enabled by default.")]
        private bool _predictionDefault = true;

        [Header("Lag compensation")]
        [SerializeField, Range(0f, 1f), Tooltip("Maximum time the server rewinds to validate a claim (s).")]
        private float _maxRewindSeconds = 0.3f;

        [SerializeField, Min(0f), Tooltip("Extra distance tolerated when validating a tackle against the rewound pose (m).")]
        private float _tackleRewindTolerance = 0.35f;

        [SerializeField, Range(0f, 90f), Tooltip("Maximum difference between the reported throw view and the server's view (deg).")]
        private float _throwViewTolerance = 15f;

        [SerializeField, Range(0.05f, 2f), Tooltip("Server: a remote player that released Grab while aiming is dropped (no throw) if its throw request does not arrive within this time (s).")]
        private float _throwRequestTimeout = 0.5f;

        [SerializeField, Tooltip("Lag compensation enabled by default.")]
        private bool _lagCompDefault = true;

        public int ProtocolVersion => _protocolVersion;
        public string EnvironmentName => _environmentName;
        public int MaxPlayers => _maxPlayers;
        public bool AllowSpectator => _allowSpectator;
        public float ReconnectGraceSeconds => _reconnectGraceSeconds;
        public int InputRedundancy => _inputRedundancy;
        public int InputBufferTarget => _inputBufferTarget;
        public float InterpolationDelay => _interpolationDelay;
        public float ReconcileThreshold => _reconcileThreshold;
        public float VisualSmoothingRate => _visualSmoothingRate;
        public bool PredictionDefault => _predictionDefault;
        public float MaxRewindSeconds => _maxRewindSeconds;
        public float TackleRewindTolerance => _tackleRewindTolerance;
        public float ThrowViewTolerance => _throwViewTolerance;
        public float ThrowRequestTimeout => _throwRequestTimeout;
        public bool LagCompDefault => _lagCompDefault;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_environmentName))
            {
                Debug.LogWarning($"[{nameof(NetworkSettings)}] Environment name on '{name}' is empty; Unity Gaming Services will use the default environment.", this);
            }

            if (_allowSpectator && _maxPlayers < 3)
            {
                Debug.LogWarning($"[{nameof(NetworkSettings)}] '{name}' allows a spectator but Max Players is {_maxPlayers}; set it to 3.", this);
            }
        }
    }
}
