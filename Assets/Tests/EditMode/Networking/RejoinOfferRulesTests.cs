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
        [TestCase(OnlineSessionErrorKeys.ServicesUnavailable, true)]
        [TestCase(OnlineSessionErrorKeys.SessionNotFound, false)]
        [TestCase(OnlineSessionErrorKeys.HostLeft, false)]
        [TestCase(OnlineSessionErrorKeys.ReconnectDenied, false)]
        [TestCase(OnlineSessionErrorKeys.SessionFull, true)]
        [TestCase(OnlineSessionErrorKeys.NotSessionMember, false)]
        [TestCase(OnlineSessionErrorKeys.JoinFailed, false)]
        public void KeepsOfferAfterFailure_OnlyForRetryableErrors(string key, bool expected)
        {
            Assert.That(RejoinOfferRules.KeepsOfferAfterFailure(key), Is.EqualTo(expected));
        }

        [Test]
        public void ShouldAutoRejoin_OfferReadyReachableAndDue_Retries()
        {
            Assert.That(RejoinOfferRules.ShouldAutoRejoin(true, OnlineSessionStatus.Ready, true, 10d, 10d), Is.True);
            Assert.That(RejoinOfferRules.ShouldAutoRejoin(true, OnlineSessionStatus.Ready, true, 12d, 10d), Is.True);
        }

        [Test]
        public void ShouldAutoRejoin_BeforeTheRetryDelay_Waits()
        {
            Assert.That(RejoinOfferRules.ShouldAutoRejoin(true, OnlineSessionStatus.Ready, true, 9.99d, 10d), Is.False);
        }

        [Test]
        public void ShouldAutoRejoin_WithoutOfferOrNetwork_DoesNot()
        {
            Assert.That(RejoinOfferRules.ShouldAutoRejoin(false, OnlineSessionStatus.Ready, true, 10d, 0d), Is.False);
            Assert.That(RejoinOfferRules.ShouldAutoRejoin(true, OnlineSessionStatus.Ready, false, 10d, 0d), Is.False);
        }

        [TestCase(OnlineSessionStatus.SignedOut)]
        [TestCase(OnlineSessionStatus.SigningIn)]
        [TestCase(OnlineSessionStatus.Creating)]
        [TestCase(OnlineSessionStatus.Joining)]
        [TestCase(OnlineSessionStatus.InSession)]
        [TestCase(OnlineSessionStatus.Leaving)]
        public void ShouldAutoRejoin_OnlyFromReady_NeverWhileAnAttemptIsInFlight(OnlineSessionStatus status)
        {
            Assert.That(RejoinOfferRules.ShouldAutoRejoin(true, status, true, 10d, 0d), Is.False);
        }

        [TestCase(OnlineSessionErrorKeys.NoConnection, false)]
        [TestCase(OnlineSessionErrorKeys.ServicesUnavailable, false)]
        [TestCase(OnlineSessionErrorKeys.RateLimited, false)]
        [TestCase(OnlineSessionErrorKeys.JoinFailed, true)]
        [TestCase(OnlineSessionErrorKeys.SessionNotFound, true)]
        [TestCase(OnlineSessionErrorKeys.ConnectionFailed, true)]
        [TestCase(OnlineSessionErrorKeys.RelayFailed, true)]
        [TestCase(OnlineSessionErrorKeys.Unknown, true)]
        [TestCase(null, true)]
        public void FallsBackToCodeJoin_ExceptWithoutNetworkOrService(string key, bool expected)
        {
            Assert.That(RejoinOfferRules.FallsBackToCodeJoin(key), Is.EqualTo(expected));
        }

        [TestCase(OnlineSessionErrorKeys.NoConnection)]
        [TestCase(OnlineSessionErrorKeys.ServicesUnavailable)]
        [TestCase(OnlineSessionErrorKeys.RateLimited)]
        public void NoNetworkReconnectFailure_KeepsTheOffer(string key)
        {
            // RejoinAsync returns the reconnection's own key when it does not fall back: it must keep the offer.
            Assert.That(RejoinOfferRules.FallsBackToCodeJoin(key), Is.False);
            Assert.That(RejoinOfferRules.KeepsOfferAfterFailure(key), Is.True);
        }
    }
}
