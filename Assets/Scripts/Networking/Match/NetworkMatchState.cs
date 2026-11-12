using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Match;
using AlienFarms.Zones;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Match
{
    /// <summary>
    /// Replicated match state, on the in-scene MatchNetwork object (inert in local mode: never spawned).
    /// <list type="bullet">
    /// <item>Server: follows the MatchManager through its GameEvents and writes phase, countdown / round end times
    /// (server time), the published score board and the obstacle layout.</item>
    /// <item>Pure clients: derive and raise the same GameEvents the server's match raises (phase, countdown ticks and
    /// GO, the 1 Hz round time, scores, and the match result from Phase == Results + score board), and apply the
    /// obstacle layout. The timer costs no messages: seconds are derived from <c>RoundEndTime</c>.</item>
    /// </list>
    /// Clients process the values in Update, after every variable of the tick has been applied, so a phase change and
    /// the scores it depends on are always seen together.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkMatchState : NetworkBehaviour
    {
        // A drift of the server's round timer from the replicated end time beyond this is re-sent (s).
        private const double RoundEndResyncThreshold = 0.1d;

        [Header("References")]
        [SerializeField, Tooltip("Match (runs on the server; its settings give clients the countdown, GO and round length).")]
        private MatchManager _match;

        [SerializeField, Tooltip("Obstacle layout of the Animal Zone (captured on the server, applied on clients).")]
        private ObstacleField _obstacles;

        private readonly NetworkVariable<MatchPhase> _phase = new NetworkVariable<MatchPhase>(
            MatchPhase.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<double> _countdownEndTime = new NetworkVariable<double>(
            0d, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<double> _roundEndTime = new NetworkVariable<double>(
            0d, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<ScoreBoard> _scoreBoard = new NetworkVariable<ScoreBoard>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<ObstacleLayoutState> _obstacleLayout = new NetworkVariable<ObstacleLayoutState>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private ObstaclePlacement[] _layoutBuffer;
        private bool _layoutCheckPending;
        private bool _warnedLayoutCapacity;

        // Pure client: what was last raised.
        private MatchPhase _appliedPhase = MatchPhase.None;
        private int _appliedScore1;
        private int _appliedScore2;
        private int _shownCountdown;
        private int _shownTime;
        private bool _resultPending;
        private bool _layoutDirty;

        public MatchPhase Phase => _phase.Value;

        public double RoundEndTime => _roundEndTime.Value;

        public ScoreBoard Scores => _scoreBoard.Value;

        private bool IsServerSpawned => IsSpawned && IsServer;

        private bool IsPureClientSpawned => IsSpawned && !IsServer;

        private MatchSettings Settings => _match != null ? _match.Settings : null;

        private double ServerNow => NetworkManager.ServerTime.Time;

        private void Awake()
        {
            int slots = _obstacles != null ? _obstacles.SlotCount : 0;
            _layoutBuffer = new ObstaclePlacement[slots];
        }

        private void OnEnable()
        {
            // Subscribed in every mode; handlers only act on a spawned server (clients raise these themselves).
            GameEvents.OnMatchPhaseChanged += HandleMatchPhaseChanged;
            GameEvents.OnScoreChanged += HandleScoreChanged;
            GameEvents.OnMatchTimeChanged += HandleMatchTimeChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnMatchPhaseChanged -= HandleMatchPhaseChanged;
            GameEvents.OnScoreChanged -= HandleScoreChanged;
            GameEvents.OnMatchTimeChanged -= HandleMatchTimeChanged;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                ServerWriteSnapshot();
                return;
            }

            _appliedPhase = MatchPhase.None;
            _appliedScore1 = int.MinValue;
            _appliedScore2 = int.MinValue;
            _shownCountdown = int.MinValue;
            _shownTime = int.MinValue;
            _resultPending = false;
            _layoutDirty = true;
            _obstacleLayout.OnValueChanged += HandleObstacleLayoutChanged;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer)
            {
                _obstacleLayout.OnValueChanged -= HandleObstacleLayoutChanged;
            }
        }

        private void Update()
        {
            if (IsPureClientSpawned)
            {
                PumpClient();
            }
        }

        private void LateUpdate()
        {
            // After the match's Update: a new round regenerated its layout in the same frame as the phase change.
            if (_layoutCheckPending && IsServerSpawned)
            {
                _layoutCheckPending = false;
                ServerPushLayout();
            }
        }

        #region Server

        /// <summary>Everything the match did before this object spawned (initial phase, scores, layout).</summary>
        private void ServerWriteSnapshot()
        {
            if (_match == null)
            {
                Debug.LogError($"[{nameof(NetworkMatchState)}] Missing MatchManager on '{name}'; the match is not replicated.", this);
                return;
            }

            MatchPhase phase = _match.CurrentPhase;
            double now = ServerNow;
            MatchSettings settings = Settings;
            _countdownEndTime.Value = phase == MatchPhase.Countdown && settings != null ? now + settings.CountdownSeconds : 0d;
            _roundEndTime.Value = now + _match.RemainingTime;

            // The HUD only shows scores published while playing (and the final ones on the results screen). The match's
            // score is frozen outside Playing, so in Results it is exactly the published result.
            bool published = phase == MatchPhase.Playing || phase == MatchPhase.Results;
            _scoreBoard.Value = published
                ? new ScoreBoard(_match.GetScore(PlayerId.Player1), _match.GetScore(PlayerId.Player2))
                : default;
            _phase.Value = phase;
            ServerPushLayout();
        }

        private void HandleMatchPhaseChanged(MatchPhase previous, MatchPhase current)
        {
            if (!IsServerSpawned)
            {
                return;
            }

            double now = ServerNow;
            if (current == MatchPhase.Countdown)
            {
                MatchSettings settings = Settings;
                _countdownEndTime.Value = now + (settings != null ? settings.CountdownSeconds : 0);
            }
            else if (current == MatchPhase.Playing && _match != null)
            {
                _roundEndTime.Value = now + _match.RemainingTime;
            }

            _phase.Value = current;
            _layoutCheckPending = true;
        }

        private void HandleScoreChanged(PlayerId playerId, int score)
        {
            if (IsServerSpawned)
            {
                _scoreBoard.Value = _scoreBoard.Value.With(playerId, score);
            }
        }

        /// <summary>1 Hz while playing: re-sends the end time only if the server's timer drifted from it.</summary>
        private void HandleMatchTimeChanged(int secondsLeft)
        {
            if (!IsServerSpawned || _match == null || _match.CurrentPhase != MatchPhase.Playing)
            {
                return;
            }

            double end = ServerNow + _match.RemainingTime;
            double drift = end - _roundEndTime.Value;
            if (drift > RoundEndResyncThreshold || drift < -RoundEndResyncThreshold)
            {
                _roundEndTime.Value = end;
            }
        }

        private void ServerPushLayout()
        {
            if (_obstacles == null || _layoutBuffer == null)
            {
                return;
            }

            int count = _obstacles.CaptureLayout(_layoutBuffer);
            if (count <= 0)
            {
                return;
            }

            if (count > ObstacleLayoutState.Capacity && !_warnedLayoutCapacity)
            {
                _warnedLayoutCapacity = true;
                Debug.LogError($"[{nameof(NetworkMatchState)}] {count} obstacle slots exceed the replicated capacity of {ObstacleLayoutState.Capacity}; extra slots stay empty on clients.", this);
            }

            ObstacleLayoutState layout = ObstacleLayoutState.FromPlacements(_layoutBuffer, count);
            if (!layout.Equals(_obstacleLayout.Value))
            {
                _obstacleLayout.Value = layout;
            }
        }

        #endregion

        #region Client

        private void HandleObstacleLayoutChanged(ObstacleLayoutState previous, ObstacleLayoutState current)
        {
            _layoutDirty = true;
        }

        /// <summary>Raises the match GameEvents in the server's order: GO, phase, scores, round time, countdown, result.</summary>
        private void PumpClient()
        {
            MatchPhase phase = _phase.Value;
            if (phase != _appliedPhase)
            {
                MatchPhase previous = _appliedPhase;
                _appliedPhase = phase;

                if (previous == MatchPhase.Countdown && phase == MatchPhase.Playing)
                {
                    MatchSettings settings = Settings;
                    GameEvents.RaiseCountdownFinished(settings != null ? settings.GoMessageDuration : 0f);
                }

                GameEvents.RaiseMatchPhaseChanged(previous, phase);
                _shownCountdown = int.MinValue;
                _shownTime = int.MinValue;
                _resultPending = phase == MatchPhase.Results;
            }

            ScoreBoard scores = _scoreBoard.Value;
            if (scores.Player1 != _appliedScore1)
            {
                _appliedScore1 = scores.Player1;
                GameEvents.RaiseScoreChanged(PlayerId.Player1, scores.Player1);
            }

            if (scores.Player2 != _appliedScore2)
            {
                _appliedScore2 = scores.Player2;
                GameEvents.RaiseScoreChanged(PlayerId.Player2, scores.Player2);
            }

            double now = ServerNow;
            MatchSettings matchSettings = Settings;
            int time = MatchClock.DisplaySeconds(phase, _roundEndTime.Value, now, matchSettings != null ? matchSettings.MatchDuration : 0f);
            if (time != _shownTime)
            {
                _shownTime = time;
                GameEvents.RaiseMatchTimeChanged(time);
            }

            if (phase == MatchPhase.Countdown)
            {
                int countdown = MatchClock.CountdownSecondsLeft(_countdownEndTime.Value, now);
                if (countdown != _shownCountdown)
                {
                    _shownCountdown = countdown;
                    GameEvents.RaiseCountdownTick(countdown);
                }
            }

            if (_resultPending)
            {
                _resultPending = false;
                GameEvents.RaiseMatchEnded(scores.ToResult());
            }

            if (_layoutDirty)
            {
                ApplyLayout();
            }
        }

        private void ApplyLayout()
        {
            if (_obstacles == null || _layoutBuffer == null)
            {
                _layoutDirty = false;
                return;
            }

            ObstacleLayoutState layout = _obstacleLayout.Value;
            if (layout.Count == 0)
            {
                // Nothing replicated yet.
                _layoutDirty = false;
                return;
            }

            for (int i = 0; i < _layoutBuffer.Length; i++)
            {
                _layoutBuffer[i].Clear();
            }

            layout.CopyTo(_layoutBuffer);

            // False until the field initialized (Awake): kept dirty and retried next frame.
            _layoutDirty = !_obstacles.ApplyLayout(_layoutBuffer);
        }

        #endregion

        private void OnValidate()
        {
            if (_match == null)
            {
                Debug.LogWarning($"[{nameof(NetworkMatchState)}] Missing MatchManager on '{name}'.", this);
            }

            if (_obstacles == null)
            {
                Debug.LogWarning($"[{nameof(NetworkMatchState)}] Missing ObstacleField on '{name}'; the obstacle layout is not replicated.", this);
            }
        }
    }
}
