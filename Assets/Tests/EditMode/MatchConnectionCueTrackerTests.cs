using AlienFarms.Audio;
using AlienFarms.Core;
using AlienFarms.Data;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class MatchConnectionCueTrackerTests
    {
        private MatchConnectionCueTracker _tracker;
        private UiSfxCue[] _output;

        [SetUp]
        public void SetUp()
        {
            _tracker = new MatchConnectionCueTracker();
            _output = new UiSfxCue[2];
            _tracker.SetPhase(MatchPhase.Playing);
        }

        [TestCase(PlayerConnectionStatus.Connected)]
        [TestCase(PlayerConnectionStatus.Reconnecting)]
        [TestCase(PlayerConnectionStatus.Left)]
        [TestCase(PlayerConnectionStatus.TimedOut)]
        public void FirstStatusForEachSeat_IsSilent(PlayerConnectionStatus status)
        {
            Assert.That(Apply(PlayerId.Player1, status), Is.Zero);
            Assert.That(Apply(PlayerId.Player2, status), Is.Zero);
        }

        [TestCase(MatchPhase.Playing, UiSfxCue.MatchSuspended)]
        [TestCase(MatchPhase.WaitingForPlayers, UiSfxCue.ConnectionLost)]
        public void SeatDrop_UsesCurrentPhase(MatchPhase phase, UiSfxCue expected)
        {
            _tracker.SetPhase(phase);
            Apply(PlayerId.Player1, PlayerConnectionStatus.Connected);

            Assert.That(Apply(PlayerId.Player1, PlayerConnectionStatus.Reconnecting), Is.EqualTo(1));
            Assert.That(_output[0], Is.EqualTo(expected));
            Assert.That(Apply(PlayerId.Player1, PlayerConnectionStatus.Reconnecting), Is.Zero);
        }

        [TestCase(MatchPhase.Playing, 2)]
        [TestCase(MatchPhase.WaitingForPlayers, 1)]
        public void ReconnectAfterSilentBaseline_ResumesOnlyIfSuspended(MatchPhase phase, int expectedCount)
        {
            _tracker.SetPhase(phase);
            Apply(PlayerId.Player1, PlayerConnectionStatus.Reconnecting);

            Assert.That(Apply(PlayerId.Player1, PlayerConnectionStatus.Connected), Is.EqualTo(expectedCount));
            Assert.That(_output[0], Is.EqualTo(UiSfxCue.PlayerReconnected));
            if (expectedCount == 2)
            {
                Assert.That(_output[1], Is.EqualTo(UiSfxCue.MatchResumed));
            }
        }

        [Test]
        public void TwoReconnectingSeats_ResumeOnlyAfterLastReconnect()
        {
            Apply(PlayerId.Player1, PlayerConnectionStatus.Connected);
            Apply(PlayerId.Player2, PlayerConnectionStatus.Connected);
            Apply(PlayerId.Player1, PlayerConnectionStatus.Reconnecting);

            Assert.That(Apply(PlayerId.Player2, PlayerConnectionStatus.Reconnecting), Is.EqualTo(1));
            Assert.That(_output[0], Is.EqualTo(UiSfxCue.ConnectionLost));
            Assert.That(Apply(PlayerId.Player1, PlayerConnectionStatus.Connected), Is.EqualTo(1));
            Assert.That(_output[0], Is.EqualTo(UiSfxCue.PlayerReconnected));
            Assert.That(Apply(PlayerId.Player2, PlayerConnectionStatus.Connected), Is.EqualTo(2));
            Assert.That(_output[0], Is.EqualTo(UiSfxCue.PlayerReconnected));
            Assert.That(_output[1], Is.EqualTo(UiSfxCue.MatchResumed));
        }

        [TestCase(PlayerConnectionStatus.Left)]
        [TestCase(PlayerConnectionStatus.TimedOut)]
        public void ReleasedSeat_PlaysLossWithoutResume_ThenJoinOnReturn(PlayerConnectionStatus released)
        {
            Apply(PlayerId.Player1, PlayerConnectionStatus.Reconnecting);

            Assert.That(Apply(PlayerId.Player1, released), Is.EqualTo(1));
            Assert.That(_output[0], Is.EqualTo(UiSfxCue.ConnectionLost));
            Assert.That(Apply(PlayerId.Player1, PlayerConnectionStatus.Connected), Is.EqualTo(1));
            Assert.That(_output[0], Is.EqualTo(UiSfxCue.PlayerJoined));
            Assert.That(Apply(PlayerId.Player1, PlayerConnectionStatus.Reconnecting), Is.EqualTo(1));
            Assert.That(_output[0], Is.EqualTo(UiSfxCue.MatchSuspended));
        }

        [Test]
        public void Reset_ClearsSeatBaselinePhaseAndSuspension()
        {
            Apply(PlayerId.Player1, PlayerConnectionStatus.Reconnecting);

            _tracker.Reset();

            Assert.That(Apply(PlayerId.Player1, PlayerConnectionStatus.Connected), Is.Zero);
            Assert.That(Apply(PlayerId.Player1, PlayerConnectionStatus.Reconnecting), Is.EqualTo(1));
            Assert.That(_output[0], Is.EqualTo(UiSfxCue.ConnectionLost));
            Assert.That(Apply(PlayerId.Player1, PlayerConnectionStatus.Connected), Is.EqualTo(1));
            Assert.That(_output[0], Is.EqualTo(UiSfxCue.PlayerReconnected));
        }

        private int Apply(PlayerId id, PlayerConnectionStatus status) => _tracker.Apply(id, status, _output);
    }
}
