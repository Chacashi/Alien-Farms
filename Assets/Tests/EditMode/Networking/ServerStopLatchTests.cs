using AlienFarms.Networking.Session;
using NUnit.Framework;
using Unity.Netcode;

namespace AlienFarms.Networking.Tests
{
    /// <summary>The host's "leaving" state lasts one run and never refuses the host itself (B4).</summary>
    public sealed class ServerStopLatchTests
    {
        [Test]
        public void New_IsNotRequested()
        {
            Assert.That(new ServerStopLatch().IsRequested, Is.False);
        }

        [Test]
        public void Request_OnlyOnTheServer_Once()
        {
            var latch = new ServerStopLatch();

            Assert.That(latch.Request(false), Is.False);
            Assert.That(latch.IsRequested, Is.False);
            Assert.That(latch.Request(true), Is.True);
            Assert.That(latch.Request(true), Is.False);
            Assert.That(latch.IsRequested, Is.True);
        }

        [Test]
        public void ServerStopped_EndsTheStop_SoTheReusedRootCanHostAgain()
        {
            var latch = new ServerStopLatch();
            latch.Request(true);

            latch.OnServerStopped();

            Assert.That(latch.IsRequested, Is.False);
            Assert.That(ServerStopLatch.ShouldRejectJoin(NetworkManager.ServerClientId, latch.IsRequested), Is.False);
            Assert.That(ServerStopLatch.ShouldRejectJoin(1UL, latch.IsRequested), Is.False);
        }

        [Test]
        public void NewSession_ResetsALeftoverStop()
        {
            var latch = new ServerStopLatch();
            latch.Request(true);

            latch.Reset();

            Assert.That(latch.IsRequested, Is.False);
        }

        [Test]
        public void ShouldRejectJoin_WhileStopping_RefusesClientsButNeverTheHost()
        {
            Assert.That(ServerStopLatch.ShouldRejectJoin(NetworkManager.ServerClientId, true), Is.False);
            Assert.That(ServerStopLatch.ShouldRejectJoin(1UL, true), Is.True);
            Assert.That(ServerStopLatch.ShouldRejectJoin(2UL, false), Is.False);
        }
    }
}
