using System.Collections.Generic;
using AlienFarms.Core;
using AlienFarms.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AlienFarms.Tests
{
    public sealed class MatchStateMachineTests
    {
        private const float MatchDuration = 300f;
        private const int CountdownSeconds = 3;
        private const float ResultsInputDelay = 1.5f;

        private MatchSettings _settings;
        private FakeMatchContext _context;
        private readonly List<int> _countdownTicks = new List<int>();
        private readonly List<MatchResult> _results = new List<MatchResult>();
        private readonly List<float> _countdownFinished = new List<float>();

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<MatchSettings>();
            var serialized = new SerializedObject(_settings);
            serialized.FindProperty("_matchDuration").floatValue = MatchDuration;
            serialized.FindProperty("_countdownSeconds").intValue = CountdownSeconds;
            serialized.FindProperty("_goMessageDuration").floatValue = 1f;
            serialized.FindProperty("_resultsInputDelay").floatValue = ResultsInputDelay;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            _context = new FakeMatchContext(_settings);
            _countdownTicks.Clear();
            _results.Clear();
            _countdownFinished.Clear();
            GameEvents.OnCountdownTick += HandleCountdownTick;
            GameEvents.OnMatchEnded += HandleMatchEnded;
            GameEvents.OnMatchPhaseChanged += HandleMatchPhaseChanged;
            GameEvents.OnCountdownFinished += HandleCountdownFinished;

            _context.ChangeState(MatchPhase.WaitingForPlayers);
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.OnCountdownTick -= HandleCountdownTick;
            GameEvents.OnMatchEnded -= HandleMatchEnded;
            GameEvents.OnMatchPhaseChanged -= HandleMatchPhaseChanged;
            GameEvents.OnCountdownFinished -= HandleCountdownFinished;
            Object.DestroyImmediate(_settings);
        }

        [Test]
        public void Waiting_StaysWithOnePlayer_AndKeepsControls()
        {
            _context.JoinedPlayerCount = 1;
            _context.Advance(5f);

            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.WaitingForPlayers));
            Assert.That(_context.ControlsEnabled, Is.True);
            Assert.That(_context.ResetRoundCount, Is.EqualTo(0));
        }

        [Test]
        public void FirstWaiting_DoesNotResetRound_ButReturnToWaitingDoes()
        {
            // SetUp already entered the first Waiting (from None).
            Assert.That(_context.ResetRoundCount, Is.EqualTo(0));
            Assert.That(_context.CallLog, Does.Not.Contain("ResetRound(false)"));

            EnterCountdown();
            _context.CallLog.Clear();
            _context.JoinedPlayerCount = 1;
            _context.Tick(0.016f);

            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.WaitingForPlayers));
            Assert.That(_context.CallLog, Is.EqualTo(new[]
            {
                "Phase(Countdown->WaitingForPlayers)", "ResetRound(false)", "Controls(true)"
            }));
        }

        [Test]
        public void EnterResults_PhaseEventPrecedesFreezingControls()
        {
            EnterPlaying();
            _context.CallLog.Clear();

            _context.Advance(MatchDuration + 0.5f, 0.5f);

            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.Results));
            int phaseIndex = _context.CallLog.IndexOf("Phase(Playing->Results)");
            int freezeIndex = _context.CallLog.IndexOf("Controls(false)");
            Assert.That(phaseIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(freezeIndex, Is.GreaterThan(phaseIndex));
        }

        [Test]
        public void Countdown_Finishing_PublishesGoMessageDuration()
        {
            EnterPlaying();

            Assert.That(_countdownFinished, Is.EqualTo(new[] { 1f }));
        }

        [Test]
        public void Waiting_WithTwoPlayers_GoesToCountdown()
        {
            _context.JoinedPlayerCount = 2;
            _context.Tick(0.016f);

            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.Countdown));
        }

        [Test]
        public void Countdown_ResetsRoundWithRespawn_FreezesPlayers_AndTicks()
        {
            EnterCountdown();

            Assert.That(_context.ResetRoundWithRespawnCount, Is.EqualTo(1));
            Assert.That(_context.ControlsEnabled, Is.False);
            Assert.That(_countdownTicks, Is.EqualTo(new[] { 3 }));

            _context.Advance(2.5f);

            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.Countdown));
            Assert.That(_countdownTicks, Is.EqualTo(new[] { 3, 2, 1 }));
        }

        [Test]
        public void Countdown_AfterThreeSeconds_GoesToPlaying_WithControls()
        {
            EnterCountdown();
            _context.Advance(CountdownSeconds + 0.05f);

            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.Playing));
            Assert.That(_context.ControlsEnabled, Is.True);
        }

        [Test]
        public void Playing_AfterMatchDuration_GoesToResults_AndPublishesResult()
        {
            EnterPlaying();
            TestScores.AddAnimals(_context.Score, PlayerId.Player2, 2);

            _context.Advance(MatchDuration - 1f, 0.5f);
            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.Playing));

            _context.Advance(1.5f, 0.5f);

            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.Results));
            Assert.That(_context.ControlsEnabled, Is.False);
            Assert.That(_results.Count, Is.EqualTo(1));
            Assert.That(_results[0].Winner, Is.EqualTo(PlayerId.Player2));
        }

        [Test]
        public void Countdown_PlayerLeaves_ReturnsToWaiting()
        {
            EnterCountdown();
            AssertReturnsToWaitingWhenPlayerLeaves();
        }

        [Test]
        public void Playing_PlayerLeaves_ReturnsToWaiting_AndAbortsRound()
        {
            EnterPlaying();
            TestScores.AddAnimals(_context.Score, PlayerId.Player1, 3);
            _context.Advance(10f);

            AssertReturnsToWaitingWhenPlayerLeaves();

            Assert.That(_context.Score.Get(PlayerId.Player1), Is.EqualTo(0));
            Assert.That(_context.Timer.Remaining, Is.EqualTo(MatchDuration));
        }

        [Test]
        public void Results_PlayerLeaves_ReturnsToWaiting()
        {
            EnterResults();
            AssertReturnsToWaitingWhenPlayerLeaves();
        }

        [Test]
        public void Results_ConfirmBeforeDelay_IsIgnored()
        {
            EnterResults();
            _context.Advance(ResultsInputDelay * 0.5f);

            _context.Confirm(PlayerId.Player1);

            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.Results));
        }

        [Test]
        public void Results_ConfirmWithMissingPlayer_IsIgnored()
        {
            EnterResults();
            _context.Advance(ResultsInputDelay + 0.1f);
            _context.JoinedPlayerCount = 1;
            int resetsBefore = _context.ResetRoundWithRespawnCount;

            _context.Confirm(PlayerId.Player1);

            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.Results));
            Assert.That(_context.ResetRoundWithRespawnCount, Is.EqualTo(resetsBefore));
        }

        [Test]
        public void Results_ConfirmAfterDelay_RestartsCountdown()
        {
            EnterResults();
            int resetsBefore = _context.ResetRoundWithRespawnCount;
            _context.Advance(ResultsInputDelay + 0.1f);

            _context.Confirm(PlayerId.Player2);

            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.Countdown));
            Assert.That(_context.ResetRoundWithRespawnCount, Is.EqualTo(resetsBefore + 1));
        }

        [Test]
        public void Playing_ChangeStateToCountdown_RestartsRoundWithRespawn()
        {
            // Covers the state-machine path used by MatchManager.RestartRound: Countdown.Enter resets the round itself.
            EnterPlaying();
            TestScores.AddAnimals(_context.Score, PlayerId.Player1, 2);
            _context.Advance(10f);
            int resetsBefore = _context.ResetRoundWithRespawnCount;
            _context.CallLog.Clear();

            _context.ChangeState(MatchPhase.Countdown);

            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.Countdown));
            Assert.That(_context.ResetRoundWithRespawnCount, Is.EqualTo(resetsBefore + 1));
            Assert.That(_context.ControlsEnabled, Is.False);
            Assert.That(_context.Score.Get(PlayerId.Player1), Is.EqualTo(0));
            Assert.That(_context.Timer.Remaining, Is.EqualTo(MatchDuration));
            Assert.That(_context.CallLog, Is.EqualTo(new[]
            {
                "Phase(Playing->Countdown)", "Controls(false)", "ResetRound(true)"
            }));
        }

        [Test]
        public void Countdown_ChangeStateToCountdown_RestartsCountdownWithRespawn()
        {
            EnterCountdown();
            _context.Advance(2f);
            int resetsBefore = _context.ResetRoundWithRespawnCount;
            _countdownTicks.Clear();
            _context.CallLog.Clear();

            _context.ChangeState(MatchPhase.Countdown);

            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.Countdown));
            Assert.That(_context.ResetRoundWithRespawnCount, Is.EqualTo(resetsBefore + 1));
            Assert.That(_countdownTicks, Is.EqualTo(new[] { CountdownSeconds }));
            Assert.That(_context.CallLog, Is.EqualTo(new[]
            {
                "Phase(Countdown->Countdown)", "Controls(false)", "ResetRound(true)"
            }));

            // The countdown starts over: still counting just before the full duration.
            _context.Advance(CountdownSeconds - 0.1f);
            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.Countdown));
        }

        [Test]
        public void IsRematchReady_OnlyInResults_AfterDelay_WithBothPlayers()
        {
            Assert.That(_context.Machine.IsRematchReady, Is.False);

            EnterPlaying();
            Assert.That(_context.Machine.IsRematchReady, Is.False);

            _context.Advance(MatchDuration + 0.5f, 0.5f);
            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.Results));
            Assert.That(_context.Machine.IsRematchReady, Is.False);

            _context.Advance(ResultsInputDelay + 0.1f);
            Assert.That(_context.Machine.IsRematchReady, Is.True);

            _context.JoinedPlayerCount = 1;
            Assert.That(_context.Machine.IsRematchReady, Is.False);
        }

        [Test]
        public void Results_LateLanding_DoesNotChangeTheFinalScore()
        {
            EnterPlaying();
            Assert.That(_context.Capture(PlayerId.Player1, new object()), Is.True);
            Assert.That(_context.Capture(PlayerId.Player1, new object()), Is.True);
            Assert.That(_context.Capture(PlayerId.Player2, new object()), Is.True);

            _context.Advance(MatchDuration + 0.5f, 0.5f);
            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.Results));

            // An animal thrown in the last second lands in P1's pen after the round ended.
            Assert.That(_context.Capture(PlayerId.Player1, new object()), Is.False);

            Assert.That(_results.Count, Is.EqualTo(1));
            Assert.That(_results[0].GetScore(PlayerId.Player1), Is.EqualTo(2));
            Assert.That(_context.Score.Get(PlayerId.Player1), Is.EqualTo(_results[0].GetScore(PlayerId.Player1)));
            Assert.That(_context.Score.Get(PlayerId.Player2), Is.EqualTo(_results[0].GetScore(PlayerId.Player2)));
        }

        [Test]
        public void Results_DropForcedByTheFreeze_IsNotCounted_EvenIfItLandsBeforeTheResult()
        {
            EnterPlaying();
            _context.Capture(PlayerId.Player1, new object());

            // Worst case: the animal released by freezing the players is captured inside Results.Enter, before the
            // result is evaluated.
            object dropped = new object();
            _context.ControlsChanged = enabled =>
            {
                if (!enabled)
                {
                    _context.Capture(PlayerId.Player1, dropped);
                }
            };

            _context.Advance(MatchDuration + 0.5f, 0.5f);

            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.Results));
            Assert.That(_results[0].GetScore(PlayerId.Player1), Is.EqualTo(1));
            Assert.That(_context.Score.Get(PlayerId.Player1), Is.EqualTo(1));
        }

        [Test]
        public void Results_AnimalLeavingAPen_DoesNotChangeTheFinalScore()
        {
            EnterPlaying();
            object animal = new object();
            _context.Capture(PlayerId.Player2, animal);
            _context.Advance(MatchDuration + 0.5f, 0.5f);

            Assert.That(_context.LeaveZone(PlayerId.Player2, animal), Is.False);
            Assert.That(_context.Score.Get(PlayerId.Player2), Is.EqualTo(_results[0].GetScore(PlayerId.Player2)));
            Assert.That(_context.Score.Get(PlayerId.Player2), Is.EqualTo(1));
        }

        [Test]
        public void Playing_CaptureOnTheLastFrame_IsInTheResult()
        {
            EnterPlaying();
            _context.Advance(MatchDuration - 0.25f, 0.5f);
            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.Playing));

            Assert.That(_context.Capture(PlayerId.Player2, new object()), Is.True);
            _context.Advance(0.5f, 0.5f);

            Assert.That(_results[0].GetScore(PlayerId.Player2), Is.EqualTo(1));
        }

        [Test]
        public void Rematch_ScoresAgainAfterTheReset()
        {
            EnterResults();
            Assert.That(_context.Capture(PlayerId.Player1, new object()), Is.False);

            _context.Advance(ResultsInputDelay + 0.1f);
            _context.Confirm(PlayerId.Player1);
            Assert.That(_context.Capture(PlayerId.Player1, new object()), Is.False, "countdown does not score");

            _context.Advance(CountdownSeconds + 0.05f);
            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.Playing));
            Assert.That(_context.Capture(PlayerId.Player1, new object()), Is.True);
            Assert.That(_context.Score.Get(PlayerId.Player1), Is.EqualTo(1));
        }

        // ---------------------------------------------------------------- Suspension (online reconnection grace)

        [Test]
        public void Playing_Suspended_FreezesControlsAndTimer_AndDoesNotAbortWithAPlayerMissing()
        {
            EnterPlaying();
            _context.Advance(10f);
            float remaining = _context.Timer.Remaining;
            _context.CallLog.Clear();

            _context.SetSuspended(true);
            _context.JoinedPlayerCount = 1;
            _context.Advance(30f);

            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.Playing));
            Assert.That(_context.Timer.Remaining, Is.EqualTo(remaining));
            Assert.That(_context.ControlsEnabled, Is.False);
            Assert.That(_context.CallLog, Is.EqualTo(new[] { "Controls(false)" }));
        }

        [Test]
        public void SetSuspended_IsIdempotent()
        {
            EnterPlaying();
            _context.CallLog.Clear();

            _context.SetSuspended(true);
            _context.SetSuspended(true);
            _context.SetSuspended(false);
            _context.SetSuspended(false);

            Assert.That(_context.CallLog, Is.EqualTo(new[] { "Controls(false)", "Controls(true)" }));
        }

        [Test]
        public void Playing_ResumedWithBothPlayers_GivesControlsBack_AndTheTimerContinues()
        {
            EnterPlaying();
            _context.Advance(10f);
            _context.SetSuspended(true);
            _context.JoinedPlayerCount = 1;
            _context.Advance(5f);
            float remaining = _context.Timer.Remaining;

            _context.JoinedPlayerCount = 2;
            _context.SetSuspended(false);
            _context.Advance(1f);

            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.Playing));
            Assert.That(_context.ControlsEnabled, Is.True);
            Assert.That(_context.Timer.Remaining, Is.EqualTo(remaining - 1f).Within(0.001f));
        }

        [Test]
        public void Playing_ResumedWithAPlayerMissing_AbortsToWaitingOnTheNextTick()
        {
            EnterPlaying();
            TestScores.AddAnimals(_context.Score, PlayerId.Player1, 2);
            _context.SetSuspended(true);
            _context.JoinedPlayerCount = 1;
            _context.CallLog.Clear();

            _context.SetSuspended(false);

            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.Playing), "Nothing happens on resume itself.");
            Assert.That(_context.CallLog, Is.Empty);

            _context.Tick(0.016f);

            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.WaitingForPlayers));
            Assert.That(_context.ControlsEnabled, Is.True);
            Assert.That(_context.Score.Get(PlayerId.Player1), Is.EqualTo(0));
        }

        [Test]
        public void Countdown_ResumedWithBothPlayers_RestartsTheCountdown()
        {
            EnterCountdown();
            _context.Advance(2f);
            _context.SetSuspended(true);
            int resetsBefore = _context.ResetRoundWithRespawnCount;
            _countdownTicks.Clear();

            _context.SetSuspended(false);

            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.Countdown));
            Assert.That(_context.ResetRoundWithRespawnCount, Is.EqualTo(resetsBefore + 1));
            Assert.That(_countdownTicks, Is.EqualTo(new[] { CountdownSeconds }));
            _context.Advance(CountdownSeconds - 0.1f);
            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.Countdown));
        }

        [Test]
        public void Countdown_SuspendedWithAPlayerMissing_DoesNotAbort_ThenAbortsAfterResume()
        {
            EnterCountdown();
            _context.SetSuspended(true);
            _context.JoinedPlayerCount = 1;
            _context.Advance(CountdownSeconds + 1f);

            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.Countdown));

            _context.SetSuspended(false);
            _context.Tick(0.016f);

            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.WaitingForPlayers));
        }

        [Test]
        public void Results_ResumedWithBothPlayers_StaysInResultsWithoutControls()
        {
            EnterResults();
            _context.SetSuspended(true);
            _context.JoinedPlayerCount = 1;
            _context.Advance(5f);
            _context.JoinedPlayerCount = 2;
            _context.CallLog.Clear();

            _context.SetSuspended(false);
            _context.Advance(ResultsInputDelay + 0.1f);

            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.Results));
            Assert.That(_context.ControlsEnabled, Is.False);
            Assert.That(_context.CallLog, Is.Empty);
            Assert.That(_context.Machine.IsRematchReady, Is.True);
        }

        [Test]
        public void Waiting_Suspended_KeepsControls()
        {
            _context.JoinedPlayerCount = 1;
            _context.CallLog.Clear();

            _context.SetSuspended(true);
            _context.SetSuspended(false);

            Assert.That(_context.ControlsEnabled, Is.True);
            Assert.That(_context.CallLog, Is.Empty);
        }

        private void EnterCountdown()
        {
            _context.JoinedPlayerCount = 2;
            _context.Tick(0.016f);
            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.Countdown));
        }

        private void EnterPlaying()
        {
            EnterCountdown();
            _context.Advance(CountdownSeconds + 0.05f);
            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.Playing));
        }

        private void EnterResults()
        {
            EnterPlaying();
            _context.Advance(MatchDuration + 0.5f, 0.5f);
            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.Results));
        }

        private void AssertReturnsToWaitingWhenPlayerLeaves()
        {
            int resetsBefore = _context.ResetRoundCount;
            _context.JoinedPlayerCount = 1;
            _context.Tick(0.016f);

            Assert.That(_context.Phase, Is.EqualTo(MatchPhase.WaitingForPlayers));
            Assert.That(_context.ControlsEnabled, Is.True);
            Assert.That(_context.ResetRoundCount, Is.EqualTo(resetsBefore + 1));
        }

        private void HandleCountdownTick(int secondsLeft) => _countdownTicks.Add(secondsLeft);

        private void HandleMatchEnded(MatchResult result) => _results.Add(result);

        private void HandleMatchPhaseChanged(MatchPhase previous, MatchPhase current) =>
            _context.CallLog.Add($"Phase({previous}->{current})");

        private void HandleCountdownFinished(float goMessageDuration) => _countdownFinished.Add(goMessageDuration);
    }
}
