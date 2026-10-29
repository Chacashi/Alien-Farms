using AlienFarms.Animals;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Player;
using UnityEngine;

namespace AlienFarms.Match
{
    /// <summary>
    /// Scene composition root of the match: owns the phase state machine, score and timer, and bridges them to the
    /// players (via <see cref="PlayerJoinCoordinator"/>) and the animals (via <see cref="AnimalSpawner"/>).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchManager : MonoBehaviour, IMatchContext
    {
        [Header("Config")]
        [SerializeField, Tooltip("Round timing.")]
        private MatchSettings _settings;

        [Header("References")]
        [SerializeField, Tooltip("Join coordinator (player registry, respawn, freeze).")]
        private PlayerJoinCoordinator _players;

        [SerializeField, Tooltip("Animal spawner (population reset between rounds).")]
        private AnimalSpawner _spawner;

        private MatchStateMachine _machine;
        private MatchScore _score;
        private MatchTimer _timer;

        public MatchPhase CurrentPhase => _machine != null ? _machine.CurrentPhase : MatchPhase.None;

        MatchSettings IMatchContext.Settings => _settings;

        int IMatchContext.JoinedPlayerCount => _players != null ? _players.JoinedCount : 0;

        MatchTimer IMatchContext.Timer => _timer;

        MatchScore IMatchContext.Score => _score;

        private void Awake()
        {
            if (_settings == null || _players == null || _spawner == null)
            {
                Debug.LogError($"[{nameof(MatchManager)}] Missing settings, join coordinator or spawner on '{name}'; match disabled.", this);
                enabled = false;
                return;
            }

            _score = new MatchScore();
            _timer = new MatchTimer(_settings.MatchDuration);
            _machine = new MatchStateMachine(this);
        }

        private void OnEnable()
        {
            GameEvents.OnAnimalCaptured += HandleAnimalCaptured;
            GameEvents.OnConfirmPressed += HandleConfirmPressed;
        }

        private void OnDisable()
        {
            GameEvents.OnAnimalCaptured -= HandleAnimalCaptured;
            GameEvents.OnConfirmPressed -= HandleConfirmPressed;
        }

        private void Start()
        {
            // No ResetRound here: the spawner populates itself in its own Start (avoids a double spawn).
            ChangeState(MatchPhase.WaitingForPlayers);

            GameEvents.RaiseScoreChanged(PlayerId.Player1, 0);
            GameEvents.RaiseScoreChanged(PlayerId.Player2, 0);
            GameEvents.RaiseMatchTimeChanged(_timer.DisplaySeconds);
        }

        private void Update()
        {
            _machine.Tick(Time.deltaTime);
        }

        /// <summary>Transition rules (event order, abort reset) live in <see cref="MatchStateMachine.ChangeState"/>.</summary>
        public void ChangeState(MatchPhase next)
        {
            if (_machine == null || !_machine.ChangeState(next))
            {
                Debug.LogError($"[{nameof(MatchManager)}] No state for phase {next}.", this);
            }
        }

        public void ResetRound(bool respawnPlayers)
        {
            if (respawnPlayers)
            {
                _players.RespawnAll();
            }

            _spawner.ResetPopulation();

            _score.Reset();
            GameEvents.RaiseScoreChanged(PlayerId.Player1, 0);
            GameEvents.RaiseScoreChanged(PlayerId.Player2, 0);

            _timer.Reset(_settings.MatchDuration);
            GameEvents.RaiseMatchTimeChanged(_timer.DisplaySeconds);
        }

        public void SetPlayerControlsEnabled(bool enabled)
        {
            _players.SetControlsEnabled(enabled);
        }

        private void HandleAnimalCaptured(PlayerId playerId, Animal animal)
        {
            if (playerId == PlayerId.None)
            {
                return;
            }

            int score = _score.Add(playerId);
            GameEvents.RaiseScoreChanged(playerId, score);
        }

        private void HandleConfirmPressed(PlayerId playerId)
        {
            _machine?.OnConfirmPressed(playerId);
        }

        private void OnValidate()
        {
            if (_settings == null)
            {
                Debug.LogWarning($"[{nameof(MatchManager)}] Missing MatchSettings on '{name}'.", this);
            }

            if (_players == null)
            {
                Debug.LogWarning($"[{nameof(MatchManager)}] Missing PlayerJoinCoordinator on '{name}'.", this);
            }

            if (_spawner == null)
            {
                Debug.LogWarning($"[{nameof(MatchManager)}] Missing AnimalSpawner on '{name}'.", this);
            }
        }
    }
}
