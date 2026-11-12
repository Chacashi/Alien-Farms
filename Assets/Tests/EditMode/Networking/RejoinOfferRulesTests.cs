using AlienFarms.Core;
using AlienFarms.Networking.Session;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests
{
    /// <summary>Client rejoin offer: armed after a lost connection, lasts the grace, survives transient failures only.</summary>
    public sealed class RejoinOfferRulesTests
    {
        [Test]
        public void ShouldOffer_PureClientWithLostConnection_InKnownSession()
        {
            Assert.That(RejoinOfferRules.ShouldOffer(false, OnlineSessionErrorKeys.ConnectionLost, "session-id", "ABC123"), Is.True);
        }

        [Test]
        public void ShouldOffer_NormalizedRawTransportDrop_Arms()
        {
            string key = OnlineSessionErrorMapper.FromDisconnectReason(DisconnectReasons.Normalize("Transport timeout"));

            Assert.That(RejoinOfferRules.ShouldOffer(false, key, "session-id", "ABC123"), Is.True);
        }

        [TestCase(OnlineSessionErrorKeys.HostLeft)]
        [TestCase(OnlineSessionErrorKeys.ReconnectDenied)]
        [TestCase(OnlineSessionErrorKeys.SessionFull)]
        [TestCase(null)]
        public void ShouldOffer_OtherReasons_DoNotArm(string key)
        {
            Assert.That(RejoinOfferRules.ShouldOffer(false, key, "session-id", "ABC123"), Is.False);
        }

        [Test]
        public void ShouldOffer_HostOrUnknownSession_DoNotArm()
        {
            Assert.That(RejoinOfferRules.ShouldOffer(true, OnlineSessionErrorKeys.ConnectionLost, "session-id", "ABC123"), Is.False);
            Assert.That(RejoinOfferRules.ShouldOffer(false, OnlineSessionErrorKeys.ConnectionLost, null, "ABC123"), Is.False);
            Assert.That(RejoinOfferRules.ShouldOffer(false, OnlineSessionErrorKeys.ConnectionLost, "session-id", string.Empty), Is.False);
        }

        [Test]
        public void ExpiresAt_EndsAMarginBeforeTheGrace_AndExpiresAtTheEdge()
        {
            double expiresAt = RejoinOfferRules.ExpiresAt(100d, 20f);
            double edge = 100d + 20f - RejoinOfferRules.ExpiryMarginSeconds;

            Assert.That(RejoinOfferRules.ExpiryMarginSeconds, Is.GreaterThan(0f));
            Assert.That(expiresAt, Is.EqualTo(edge));
            Assert.That(RejoinOfferRules.IsExpired(expiresAt, edge - 0.01d), Is.False);
            Assert.That(RejoinOfferRules.IsExpired(expiresAt, edge), Is.True);
        }

        [Test]
        public void ExpiresAt_NeverBeforeNow()
        {
            Assert.That(RejoinOfferRules.ExpiresAt(100d, -5f), Is.EqualTo(100d));
            Assert.That(RejoinOfferRules.ExpiresAt(100d, RejoinOfferRules.ExpiryMarginSeconds * 0.5f), Is.EqualTo(100d));
        }

        [TestCase(OnlineSessionErrorKeys.AlreadyConnected, true)]
        [TestCase(OnlineSessionErrorKeys.ConnectionFailed, true)]
        [TestCase(OnlineSessionErrorKeys.NoConnection, true)]
        [TestCase(OnlineSessionErrorKeys.RateLimited, true)]
        [TestCase(OnlineSessionErrorKeys.SessionNotFound, false)]
        [TestCase(OnlineSessionErrorKeys.HostLeft, false)]
        [TestCase(OnlineSessionErrorKeys.ReconnectDenied, false)]
        [TestCase(OnlineSessionErrorKeys.SessionFull, false)]
        [TestCase(OnlineSessionErrorKeys.NotSessionMember, false)]
        [TestCase(OnlineSessionErrorKeys.JoinFailed, false)]
        public void KeepsOfferAfterFailure_OnlyForRetryableErrors(string key, bool expected)
        {
            Assert.That(RejoinOfferRules.KeepsOfferAfterFailure(key), Is.EqualTo(expected));
        }
    }
}
