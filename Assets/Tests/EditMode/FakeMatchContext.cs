using System.Collections.Generic;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Match;

namespace AlienFarms.Tests
{
    /// <summary>
    /// Test double of <see cref="IMatchContext"/>: only records side effects. Transitions are delegated to the real
    /// <see cref="MatchStateMachine"/>, exactly like MatchManager does.
    /// </summary>
    public sealed class FakeMatchContext : IMatchContext
    {
        public FakeMatchContext(MatchSettings settings)
        {
            Settings = settings;
            Score = new MatchScore();
            Timer = new MatchTimer(settings.MatchDuration);
            Machine = new MatchStateMachine(this);
        }

        public MatchStateMachine Machine { get; }
        public MatchSettings Settings { get; }
        public MatchTimer Timer { get; }
        public MatchScore Score { get; }

        public int JoinedPlayerCount { get; set; }

        public MatchPhase Phase => Machine.CurrentPhase;
        public bool ControlsEnabled { get; private set; } = true;
        public int ResetRoundCount { get; private set; }
        public int ResetRoundWithRespawnCount { get; private set; }

        /// <summary>Ordered log of side effects (shared with test event handlers to assert ordering).</summary>
        public List<string> CallLog { get; } = new List<string>();

        public void ChangeState(MatchPhase next) => Machine.ChangeState(next);

        public void ResetRound(bool respawnPlayers)
        {
            CallLog.Add(respawnPlayers ? "ResetRound(true)" : "ResetRound(false)");
            ResetRoundCount++;
            if (respawnPlayers)
            {
                ResetRoundWithRespawnCount++;
            }

            Score.Reset();
            Timer.Reset(Settings.MatchDuration);
        }

        public void SetPlayerControlsEnabled(bool enabled)
        {
            CallLog.Add(enabled ? "Controls(true)" : "Controls(false)");
            ControlsEnabled = enabled;
        }

        public void Tick(float deltaTime) => Machine.Tick(deltaTime);

        public void Confirm(PlayerId playerId) => Machine.OnConfirmPressed(playerId);

        /// <summary>Ticks in fixed steps (like frames) for <paramref name="seconds"/>.</summary>
        public void Advance(float seconds, float step = 0.1f)
        {
            float elapsed = 0f;
            while (elapsed < seconds - 0.00001f)
            {
                float dt = seconds - elapsed < step ? seconds - elapsed : step;
                Tick(dt);
                elapsed += dt;
            }
        }
    }
}
