using AlienFarms.Networking.Session;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests
{
    /// <summary>A pure client's drop reason: NGO's reason, or host_disconnected after the host's leaving notice (B1).</summary>
    public sealed class DisconnectReasonsResolveTests
    {
        [Test]
        public void Resolve_RawTransportDropAfterNotice_IsHostDisconnected()
        {
            string reason = DisconnectReasons.Resolve("[ClosedByRemote] Connection was closed by remote endpoint", true);

            Assert.That(reason, Is.EqualTo(DisconnectReasons.HostDisconnected));
        }

        [Test]
        public void Resolve_RawTransportDropWithoutNotice_IsConnectionLost()
        {
            Assert.That(DisconnectReasons.Resolve("Transport timeout", false), Is.EqualTo(DisconnectReasons.ConnectionLost));
        }

        [TestCase(null)]
        [TestCase("")]
        public void Resolve_NoReasonAfterNotice_IsHostDisconnected(string raw)
        {
            Assert.That(DisconnectReasons.Resolve(raw, true), Is.EqualTo(DisconnectReasons.HostDisconnected));
        }

        [Test]
        public void Resolve_HostReasonArrived_IsHostDisconnectedEitherWay()
        {
            Assert.That(DisconnectReasons.Resolve(DisconnectReasons.HostDisconnected, false), Is.EqualTo(DisconnectReasons.HostDisconnected));
            Assert.That(DisconnectReasons.Resolve(DisconnectReasons.HostDisconnected, true), Is.EqualTo(DisconnectReasons.HostDisconnected));
        }

        [TestCase(DisconnectReasons.ReconnectDenied)]
        [TestCase(DisconnectReasons.RoomFull)]
        [TestCase(DisconnectReasons.ProtocolMismatch)]
        public void Resolve_OtherGameReason_IsKeptEvenAfterNotice(string reason)
        {
            Assert.That(DisconnectReasons.Resolve(reason, true), Is.EqualTo(reason));
        }
    }
}
