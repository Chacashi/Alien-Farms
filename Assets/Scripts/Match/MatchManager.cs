using AlienFarms.Animals;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Items;
using AlienFarms.Player;
using AlienFarms.Zones;
using UnityEngine;

namespace AlienFarms.Match
{
    /// <summary>
    /// Scene composition root of the match: owns the phase state machine, score and timer, and bridges them to the
    /// players (via <see cref="PlayerJoinCoordinator"/>) and the animals (via <see cref="AnimalSpawner"/>).
    /// The score mirrors zone occupancy from <see cref="GameEvents.OnAnimalCaptured"/> / <see cref="GameEvents.OnAnimalLeftZone"/>;
    /// it is tracked in every phase but only published while playing.
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

        [SerializeField, Tooltip("Random obstacle layout of the Animal Zone (rolled on each new round).")]
        private ObstacleField _obstacles;

        [SerializeField, Tooltip("Optional: stun bomb spawner (bombs cleared between rounds).")]
        private BombSpawner _bombs;

        private MatchStateMachine _machine;
        private MatchScore _score;
        private MatchTimer _timer;

        public MatchPhase CurrentPhase => _machine != null ? _machine.CurrentPhase : MatchPhase.None;

        /// <summary>True when <see cref="RequestRematch"/> would start a new round now.</summary>
        public bool IsRematchReady => _machine != null && _machine.IsRematchReady;

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
            GameEvents.OnAnimalLeftZone += HandleAnimalLeftZone;
        }

        private void OnDisable()
        {
            GameEvents.OnAnimalCaptured -= HandleAnimalCaptured;
            GameEvents.OnAnimalLeftZone -= HandleAnimalLeftZone;
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

        /// <summary>
        /// Restarts the current round from the countdown (Countdown.Enter resets the round with respawn). While waiting
        /// for players there is no round to restart, so the arena is just reset (respawn, new layout, animals).
        /// </summary>
        public void RestartRound()
        {
            if (_machine == null)
            {
                return;
            }

            if (CurrentPhase == MatchPhase.WaitingForPlayers)
            {
                ResetRound(true);
                return;
            }

            ChangeState(MatchPhase.Countdown);
        }

        /// <summary>Asks the Results state for a rematch; it honors <see cref="MatchSettings.ResultsInputDelay"/> and needs both players.</summary>
        public void RequestRematch()
        {
            _machine?.OnConfirmPressed(PlayerId.None);
        }

        public void ResetRound(bool respawnPlayers)
        {
            if (respawnPlayers)
            {
                _players.RespawnAll();

                // Must run after the respawn (slots with a player stay empty) and before the repopulation
                // (the spawner avoids active obstacles). Direct call, not an event, because the order matters.
                if (_obstacles != null)
                {
                    _obstacles.Regenerate();
                }
            }

            // Despawning captured animals raises OnAnimalLeftZone; the reset below clears whatever is left anyway.
            _spawner.ResetPopulation();

            if (_bombs != null)
            {
                _bombs.ResetBombs();
            }

            _score.Reset();
            // Published regardless of phase: the HUD must show 0-0 for the new round.
            GameEvents.RaiseScoreChanged(PlayerId.Player1, 0);
            GameEvents.RaiseScoreChanged(PlayerId.Player2, 0);

            _timer.Reset(_settings.MatchDuration);
            GameEvents.RaiseMatchTimeChanged(_timer.DisplaySeconds);
        }

        public void SetPlayerControlsEnabled(bool enabled)
        {
            _players.SetControlsEnabled(enabled);
        }

        private void HandleAnimalCaptured(PlayerId zoneOwner, Animal animal)
        {
            if (_score == null || animal == null || !CaptureRules.CountsForZone(animal.State, zoneOwner))
            {
                return;
            }

            if (_score.TryAdd(zoneOwner, animal, out PlayerId movedFrom))
            {
                PublishScore(zoneOwner);
                if (movedFrom != PlayerId.None)
                {
                    PublishScore(movedFrom);
                }
            }
        }

        private void HandleAnimalLeftZone(PlayerId zoneOwner, Animal animal)
        {
            if (_score != null && _score.TryRemove(zoneOwner, animal))
            {
                PublishScore(zoneOwner);
            }
        }

        private void PublishScore(PlayerId playerId)
        {
            if (CaptureRules.PublishesScore(CurrentPhase))
            {
                GameEvents.RaiseScoreChanged(playerId, _score.Get(playerId));
            }
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

            if (_obstacles == null)
            {
                Debug.LogWarning($"[{nameof(MatchManager)}] No ObstacleField on '{name}'; the obstacle layout never changes between rounds.", this);
            }

            if (_bombs == null)
            {
                Debug.LogWarning($"[{nameof(MatchManager)}] No BombSpawner on '{name}'; bombs are not cleared between rounds.", this);
            }
        }
    }
}
