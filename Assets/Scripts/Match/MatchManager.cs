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
    /// The score (sum of each captured animal's points) mirrors zone occupancy from <see cref="GameEvents.OnAnimalCaptured"/> / <see cref="GameEvents.OnAnimalLeftZone"/>;
    /// it is tracked in every phase but only published while playing.
    /// The players come from an <see cref="IPlayerRoster"/>: the serialized coordinator unless another one is bound
    /// with <see cref="Bind"/> before Start.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchManager : MonoBehaviour, IMatchContext
    {
        [Header("Config")]
        [SerializeField, Tooltip("Round timing.")]
        private MatchSettings _settings;

        [Header("References")]
        [SerializeField, Tooltip("Local join coordinator (player registry, respawn, freeze); default roster unless another one is bound.")]
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
        private IPlayerRoster _roster;
        private bool _suspended;
        private bool _started;

        public MatchPhase CurrentPhase => _machine != null ? _machine.CurrentPhase : MatchPhase.None;

        /// <summary>Seconds left in the round (0 before Awake).</summary>
        public float RemainingTime => _timer != null ? _timer.Remaining : 0f;

        /// <summary>True while the state machine is not ticked (see <see cref="SetSuspended"/>).</summary>
        public bool IsSuspended => _suspended;

        /// <summary>True when <see cref="RequestRematch"/> would start a new round now.</summary>
        public bool IsRematchReady => _machine != null && _machine.IsRematchReady;

        MatchSettings IMatchContext.Settings => _settings;

        int IMatchContext.JoinedPlayerCount => _roster != null ? _roster.JoinedCount : 0;

        MatchTimer IMatchContext.Timer => _timer;

        MatchScore IMatchContext.Score => _score;

        private void Awake()
        {
            if (_settings == null || _spawner == null)
            {
                Debug.LogError($"[{nameof(MatchManager)}] Missing settings or spawner on '{name}'; match disabled.", this);
                enabled = false;
                return;
            }

            // A roster bound before Awake wins. Unity null check before boxing to the interface.
            if (_roster == null && _players != null)
            {
                _roster = _players;
            }

            _score = new MatchScore();
            _timer = new MatchTimer(_settings.MatchDuration);
            _machine = new MatchStateMachine(this);
        }

        /// <summary>Replaces the player roster (e.g. the online one). Must be called before Start.</summary>
        public void Bind(IPlayerRoster roster)
        {
            if (_started)
            {
                Debug.LogWarning($"[{nameof(MatchManager)}] Roster bound on '{name}' after the match started; bind it before Start.", this);
            }

            _roster = roster;
        }

        /// <summary>While suspended the state machine is not ticked (timer and countdown frozen); events still update the score.</summary>
        public void SetSuspended(bool suspended)
        {
            _suspended = suspended;
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
            _started = true;

            // Checked here, not in Awake, so a roster can be bound between both.
            if (_roster == null)
            {
                Debug.LogError($"[{nameof(MatchManager)}] No player roster on '{name}' (join coordinator missing and none bound); match disabled.", this);
                enabled = false;
                return;
            }

            // No ResetRound here: the spawner populates itself in its own Start (avoids a double spawn).
            ChangeState(MatchPhase.WaitingForPlayers);

            GameEvents.RaiseScoreChanged(PlayerId.Player1, 0);
            GameEvents.RaiseScoreChanged(PlayerId.Player2, 0);
            GameEvents.RaiseMatchTimeChanged(_timer.DisplaySeconds);
        }

        private void Update()
        {
            if (_suspended)
            {
                return;
            }

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

        /// <summary>Animals of <paramref name="type"/> inside <paramref name="playerId"/>'s pen this round.</summary>
        public int GetPenCount(PlayerId playerId, AnimalData type) => _score != null ? _score.GetCountByType(playerId, type) : 0;

        /// <summary>Asks the Results state for a rematch; it honors <see cref="MatchSettings.ResultsInputDelay"/> and needs both players.</summary>
        public void RequestRematch()
        {
            _machine?.OnConfirmPressed(PlayerId.None);
        }

        public void ResetRound(bool respawnPlayers)
        {
            if (respawnPlayers)
            {
                _roster.RespawnAll();

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
            _roster?.SetControlsEnabled(enabled);
        }

        private void HandleAnimalCaptured(PlayerId zoneOwner, Animal animal)
        {
            if (_score == null || animal == null || !CaptureRules.CountsForZone(animal.State, zoneOwner))
            {
                return;
            }

            if (_score.TryAdd(zoneOwner, animal, animal.Points, animal.Data, out PlayerId movedFrom))
            {
                PublishScore(zoneOwner);
                PublishPenInventory(zoneOwner, animal.Data);
                if (movedFrom != PlayerId.None)
                {
                    PublishScore(movedFrom);
                    PublishPenInventory(movedFrom, animal.Data);
                }
            }
        }

        private void HandleAnimalLeftZone(PlayerId zoneOwner, Animal animal)
        {
            if (_score != null && _score.TryRemove(zoneOwner, animal))
            {
                PublishScore(zoneOwner);
                PublishPenInventory(zoneOwner, animal != null ? animal.Data : null);
            }
        }

        /// <summary>
        /// Pen contents mirror zone occupancy in every phase (unlike the score). The type index is the animal's position
        /// in the spawn settings' entries (<see cref="AnimalSpawner.TryGetTypeIndex"/>).
        /// </summary>
        private void PublishPenInventory(PlayerId playerId, AnimalData type)
        {
            if (type != null && _spawner.TryGetTypeIndex(type, out int typeIndex))
            {
                GameEvents.RaisePenInventoryChanged(playerId, typeIndex, _score.GetCountByType(playerId, type));
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
