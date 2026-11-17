using System;
using System.Collections.Generic;
using AlienFarms.Animals;
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

        public bool ForfeitOnPlayerMissing { get; set; }

        public PlayerId ForfeitWinner { get; private set; }

        /// <summary>With one player joined, the one that is not present (default Player 2).</summary>
        public PlayerId MissingPlayer { get; set; } = PlayerId.Player2;

        /// <summary>Both present with 2 joined, nobody with 0, everyone but <see cref="MissingPlayer"/> with 1.</summary>
        public bool IsPlayerPresent(PlayerId id)
        {
            if (id != PlayerId.Player1 && id != PlayerId.Player2)
            {
                return false;
            }

            if (JoinedPlayerCount >= 2)
            {
                return true;
            }

            return JoinedPlayerCount == 1 && id != MissingPlayer;
        }

        /// <summary>Same steps as MatchManager.EndByForfeit.</summary>
        public void EndByForfeit(PlayerId winner)
        {
            if (Phase != MatchPhase.Playing || (winner != PlayerId.Player1 && winner != PlayerId.Player2))
            {
                return;
            }

            CallLog.Add("EndByForfeit(" + winner + ")");
            ForfeitWinner = winner;
            ChangeState(MatchPhase.Results);
        }

        public MatchPhase Phase => Machine.CurrentPhase;
        public bool ControlsEnabled { get; private set; } = true;
        public int ResetRoundCount { get; private set; }
        public int ResetRoundWithRespawnCount { get; private set; }

        /// <summary>While true <see cref="Tick"/> does nothing, like MatchManager.Update while suspended.</summary>
        public bool Suspended { get; private set; }

        /// <summary>Ordered log of side effects (shared with test event handlers to assert ordering).</summary>
        public List<string> CallLog { get; } = new List<string>();

        public void ChangeState(MatchPhase next) => Machine.ChangeState(next);

        public void ResetRound(bool respawnPlayers)
        {
            CallLog.Add(respawnPlayers ? "ResetRound(true)" : "ResetRound(false)");
            ForfeitWinner = PlayerId.None;
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
            ControlsChanged?.Invoke(enabled);
        }

        /// <summary>Invoked after every SetPlayerControlsEnabled (e.g. to land the animal a forced drop releases).</summary>
        public Action<bool> ControlsChanged { get; set; }

        /// <summary>A captured animal landing in <paramref name="zoneOwner"/>'s zone, through the same call MatchManager makes.</summary>
        public bool Capture(PlayerId zoneOwner, object animal, int points = 1)
        {
            return Score.TryCapture(Phase, AnimalState.Captured, zoneOwner, animal, points, null, out _);
        }

        /// <summary>An animal leaving <paramref name="zoneOwner"/>'s zone, through the same call MatchManager makes.</summary>
        public bool LeaveZone(PlayerId zoneOwner, object animal)
        {
            return Score.TryRelease(Phase, zoneOwner, animal);
        }

        public void Tick(float deltaTime)
        {
            if (!Suspended)
            {
                Machine.Tick(deltaTime);
            }
        }

        /// <summary>Same steps as MatchManager.SetSuspended (idempotent, MatchSuspendRules on both edges).</summary>
        public void SetSuspended(bool suspended)
        {
            if (Suspended == suspended)
            {
                return;
            }

            Suspended = suspended;
            if (suspended)
            {
                if (MatchSuspendRules.SuspendsIn(Phase))
                {
                    SetPlayerControlsEnabled(false);
                }

                return;
            }

            switch (MatchSuspendRules.OnResume(Phase, JoinedPlayerCount >= 2))
            {
                case MatchResumeAction.EnableControls:
                    SetPlayerControlsEnabled(true);
                    break;
                case MatchResumeAction.RestartCountdown:
                    // MatchManager.RestartRound outside WaitingForPlayers.
                    ChangeState(MatchPhase.Countdown);
                    break;
            }
        }

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
