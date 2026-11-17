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
        // History: 1 = payload with CharacterIndex (1.5); 2 = character picked in the room, removed from the payload (2.3);
        // 3 = MatchResult.EndReason / forfeit winner replicated by NetworkMatchState (2.8).
        [SerializeField, Min(1), Tooltip("Connection protocol version; clients with a different one are rejected. Bump it whenever the connection payload or any replicated layout changes.")]
        private int _protocolVersion = 3;

        [SerializeField, Tooltip("Unity Gaming Services environment name.")]
        private string _environmentName = "dev";

        [SerializeField, Range(2, 3), Tooltip("Maximum connections per session (2 players, plus a spectator when allowed).")]
        private int _maxPlayers = 2;

        [SerializeField, Tooltip("Accept a third connection as a spectator (no avatar, no game input; watches the match) instead of answering 'Room full'. Needs Max Players 3.")]
        private bool _allowSpectator;

        [SerializeField, Min(0f), Tooltip("Seconds a disconnected player keeps its slot and pose while it reconnects.")]
        private float _reconnectGraceSeconds = 20f;

        [SerializeField, Range(0f, 2f), Tooltip("A host leaving on purpose first tells its clients and keeps their connections open this long (s) so the notice is delivered, then disconnects them (they record a host-left win instead of a lost connection). 0 = disconnect at once.")]
        private float _hostLeaveNoticeSeconds = 0.5f;

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

        [Header("Debug")]
        [SerializeField, Tooltip("Ownership labels over carried objects visible by default (the debug overlay can override it at runtime).")]
        private bool _showOwnershipLabels = true;

        [SerializeField, Tooltip("Logs every ownership change of carried objects (animals, bombs) on every instance.")]
        private bool _logOwnership = true;

        [SerializeField, Tooltip("Pool counters (animals and bombs: active / in pool / created) and receive-queue stats visible by default in the online debug view (the debug overlay can override it at runtime).")]
        private bool _showPoolStats = true;

        [SerializeField, Tooltip("Logs every network spawn / despawn of a pooled animal or bomb (first use or reused, pool counters). Off by default: noisy.")]
        private bool _logPoolLifecycle;

        [Header("Name tags")]
        [SerializeField, Tooltip("Floating name tag over every remote player avatar (never over the local one).")]
        private bool _showNameTags = true;

        [SerializeField, Tooltip("Debug: adds a 'client N' line (the avatar owner's NGO client id) under each name tag, to see the new id after a reconnection.")]
        private bool _nameTagShowsClientId;

        [Header("Cloud Save")]
        [SerializeField, Tooltip("Read the rival's public stats in the online room (Plan B #2: off, and the RivalStats view disabled).")]
        private bool _readRivalStats = true;

        [SerializeField, Min(1), Tooltip("Attempts per stats save before reporting the failure (later rounds or loads retry again).")]
        private int _statsSaveAttempts = 3;

        [SerializeField, Min(0.5f), Tooltip("Delay before the first stats save retry (s); multiplied by 3 on every further attempt. A rate limit's Retry-After wins when longer.")]
        private float _statsRetryDelaySeconds = 4f;

        [SerializeField, Tooltip("Logs every stats load, save, conflict and retry.")]
        private bool _logPersistence = true;

        [SerializeField, Tooltip("Editor / development builds only: every stats load and save fails as offline, without touching the network.")]
        private bool _simulateStatsFailure;

        public int ProtocolVersion => _protocolVersion;
        public string EnvironmentName => _environmentName;
        public int MaxPlayers => _maxPlayers;
        public bool AllowSpectator => _allowSpectator;
        public float ReconnectGraceSeconds => _reconnectGraceSeconds;
        public float HostLeaveNoticeSeconds => _hostLeaveNoticeSeconds;
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
        public bool ShowOwnershipLabels => _showOwnershipLabels;
        public bool LogOwnership => _logOwnership;
        public bool ShowPoolStats => _showPoolStats;
        public bool LogPoolLifecycle => _logPoolLifecycle;
        public bool ShowNameTags => _showNameTags;
        public bool NameTagShowsClientId => _nameTagShowsClientId;
        public bool ReadRivalStats => _readRivalStats;
        public int StatsSaveAttempts => _statsSaveAttempts;
        public float StatsRetryDelaySeconds => _statsRetryDelaySeconds;
        public bool LogPersistence => _logPersistence;

        /// <summary>Simulated offline stats store; always false outside the Editor and development builds.</summary>
        public bool SimulateStatsFailure
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return _simulateStatsFailure;
#else
                return false;
#endif
            }
        }

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
