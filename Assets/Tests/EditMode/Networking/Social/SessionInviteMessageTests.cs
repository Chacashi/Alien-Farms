using System.Text;
using AlienFarms.Networking.Social;
using Newtonsoft.Json;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests.Social
{
    public sealed class SessionInviteMessageTests
    {
        [Test]
        public void Newtonsoft_RoundTripsAllFieldsWithinWireLimit()
        {
            var message = new SessionInviteMessage
            {
                code = "BCDFGH",
                sessionId = "session-id",
                hostName = "Anfitrión",
                sentAtUnixMs = 1_700_000_000_000L
            };

            string json = JsonConvert.SerializeObject(message);
            var copy = JsonConvert.DeserializeObject<SessionInviteMessage>(json);

            Assert.That(Encoding.UTF8.GetByteCount(json), Is.LessThan(10_240));
            Assert.That(copy.type, Is.EqualTo("af.invite"));
            Assert.That(copy.version, Is.EqualTo(1));
            Assert.That(copy.code, Is.EqualTo(message.code));
            Assert.That(copy.sessionId, Is.EqualTo(message.sessionId));
            Assert.That(copy.hostName, Is.EqualTo(message.hostName));
            Assert.That(copy.sentAtUnixMs, Is.EqualTo(message.sentAtUnixMs));
        }
    }
}
