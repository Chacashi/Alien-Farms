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
            _context.Score.Add(PlayerId.Player2, 2);

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
            _context.Score.Add(PlayerId.Player1, 3);
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
