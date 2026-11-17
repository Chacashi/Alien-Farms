using AlienFarms.Networking.Session;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests
{
    /// <summary>Graceful host shutdown: notice time, disconnect with reason, then Shutdown in a later frame (B1).</summary>
    public sealed class HostShutdownSequenceTests
    {
        private const float Notice = 0.5f;

        private HostShutdownSequence _sequence;

        [SetUp]
        public void SetUp()
        {
            _sequence = new HostShutdownSequence();
        }

        [Test]
        public void ShouldDefer_OnlyHostWithNotifiedClientsAndNoticeTime()
        {
            Assert.That(HostShutdownSequence.ShouldDefer(true, 1, Notice), Is.True);
            Assert.That(HostShutdownSequence.ShouldDefer(false, 1, Notice), Is.False);
            Assert.That(HostShutdownSequence.ShouldDefer(true, 0, Notice), Is.False);
            Assert.That(HostShutdownSequence.ShouldDefer(true, 2, 0f), Is.False);
        }

        [Test]
        public void Idle_Waits()
        {
            Assert.That(_sequence.IsRunning, Is.False);
            Assert.That(_sequence.Step(0d, true, false, true), Is.EqualTo(HostShutdownStep.Wait));
        }

        [Test]
        public void Announcing_KeepsConnectionsOpenUntilTheNoticeTime()
        {
            _sequence.Begin(10d, Notice);

            Assert.That(_sequence.IsRunning, Is.True);
            Assert.That(_sequence.Step(10d, true, false, true), Is.EqualTo(HostShutdownStep.Wait));
            Assert.That(_sequence.Step(10.49d, true, false, true), Is.EqualTo(HostShutdownStep.Wait));
            Assert.That(_sequence.Step(10.5d, true, false, true), Is.EqualTo(HostShutdownStep.DisconnectClients));
        }

        [Test]
        public void FullSequence_DisconnectsThenShutsDownNextFrame_ThenCompletesWhenStopped()
        {
            _sequence.Begin(0d, Notice);

            Assert.That(_sequence.Step(0.5d, true, false, true), Is.EqualTo(HostShutdownStep.DisconnectClients));

            // Same frame as the disconnect never shuts down: NGO drops the clients on its late update first.
            Assert.That(_sequence.Step(0.51d, true, false, true), Is.EqualTo(HostShutdownStep.Wait));
            Assert.That(_sequence.Step(0.52d, true, false, false), Is.EqualTo(HostShutdownStep.Shutdown));
            Assert.That(_sequence.Step(0.53d, true, true, false), Is.EqualTo(HostShutdownStep.Wait));
            Assert.That(_sequence.Step(0.54d, false, false, false), Is.EqualTo(HostShutdownStep.Complete));
            Assert.That(_sequence.IsRunning, Is.False);
        }

        [Test]
        public void Announcing_AllClientsGone_DisconnectsEarly()
        {
            _sequence.Begin(0d, Notice);

            Assert.That(_sequence.Step(0.1d, true, false, false), Is.EqualTo(HostShutdownStep.DisconnectClients));
        }

        [Test]
        public void Disconnecting_ClientsNeverDrop_ShutsDownAfterTheFlushTimeout()
        {
            _sequence.Begin(0d, Notice);
            _sequence.Step(0.5d, true, false, true);

            Assert.That(_sequence.Step(0.5d + HostShutdownSequence.DisconnectFlushSeconds - 0.01d, true, false, true), Is.EqualTo(HostShutdownStep.Wait));
            Assert.That(_sequence.Step(0.5d + HostShutdownSequence.DisconnectFlushSeconds, true, false, true), Is.EqualTo(HostShutdownStep.Shutdown));
        }

        [Test]
        public void Stopping_NeverStops_CompletesAfterTheStopTimeout()
        {
            _sequence.Begin(0d, Notice);
            _sequence.Step(0.5d, true, false, true);
            _sequence.Step(0.6d, true, false, false);

            Assert.That(_sequence.Step(0.6d + HostShutdownSequence.StopTimeoutSeconds - 0.01d, true, true, false), Is.EqualTo(HostShutdownStep.Wait));
            Assert.That(_sequence.Step(0.6d + HostShutdownSequence.StopTimeoutSeconds, true, true, false), Is.EqualTo(HostShutdownStep.Complete));
        }

        [Test]
        public void Announcing_ShutDownElsewhere_NeverDisconnectsOrShutsDownAgain()
        {
            _sequence.Begin(0d, Notice);

            Assert.That(_sequence.Step(0.1d, true, true, true), Is.EqualTo(HostShutdownStep.Wait));
            Assert.That(_sequence.Step(0.2d, true, true, true), Is.EqualTo(HostShutdownStep.Wait));
            Assert.That(_sequence.Step(0.3d, false, false, false), Is.EqualTo(HostShutdownStep.Complete));
        }

        [Test]
        public void Disconnecting_ShutDownElsewhere_NeverAsksForShutdownAgain()
        {
            _sequence.Begin(0d, Notice);
            Assert.That(_sequence.Step(0.5d, true, false, true), Is.EqualTo(HostShutdownStep.DisconnectClients));

            Assert.That(_sequence.Step(0.51d, true, true, true), Is.EqualTo(HostShutdownStep.Wait));
            Assert.That(_sequence.Step(0.6d, true, true, false), Is.EqualTo(HostShutdownStep.Wait));
            Assert.That(_sequence.Step(0.7d, false, false, false), Is.EqualTo(HostShutdownStep.Complete));
        }

        [Test]
        public void Reset_StopsTheSequence()
        {
            _sequence.Begin(0d, Notice);
            _sequence.Reset();

            Assert.That(_sequence.IsRunning, Is.False);
            Assert.That(_sequence.Step(5d, true, false, true), Is.EqualTo(HostShutdownStep.Wait));
        }
    }
}
