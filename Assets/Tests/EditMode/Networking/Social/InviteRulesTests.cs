using AlienFarms.Networking.Social;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests.Social
{
    public sealed class InviteRulesTests
    {
        [Test]
        public void Validate_NormalizesCodeAndAcceptsBeforeExpiration()
        {
            var result = InviteRules.Validate(CreateMessage(), "local", "host", 1_999, 1_000, out string code);
            Assert.That(result, Is.EqualTo(InviteRules.Result.Valid));
            Assert.That(code, Is.EqualTo("BCDFGH"));
        }

        [TestCase("other", 1, "BCDFGH", "host", 1_000L, InviteRules.Result.Malformed)]
        [TestCase("af.invite", 2, "BCDFGH", "host", 1_000L, InviteRules.Result.WrongVersion)]
        [TestCase("af.invite", 1, "AAAAAA", "host", 1_000L, InviteRules.Result.Malformed)]
        [TestCase("af.invite", 1, "BCDFGH", "local", 1_000L, InviteRules.Result.FromSelf)]
        [TestCase("af.invite", 1, "BCDFGH", "host", 2_000L, InviteRules.Result.Expired)]
        public void Validate_RejectsInvalidOffers(string type, int version, string code, string sender,
            long now, InviteRules.Result expected)
        {
            SessionInviteMessage message = CreateMessage();
            message.type = type;
            message.version = version;
            message.code = code;

            Assert.That(InviteRules.Validate(message, "local", sender, now, 1_000, out string normalized),
                Is.EqualTo(expected));
            Assert.That(normalized, Is.Null);
        }

        [Test]
        public void Validate_NullPayloadIsMalformed()
        {
            Assert.That(InviteRules.Validate(null, "local", "host", 1_000, 1_000),
                Is.EqualTo(InviteRules.Result.Malformed));
        }

        private static SessionInviteMessage CreateMessage() => new SessionInviteMessage
        {
            code = " bcdfgh ",
            sessionId = "session",
            hostName = "Host",
            sentAtUnixMs = 1_000
        };
    }
}
