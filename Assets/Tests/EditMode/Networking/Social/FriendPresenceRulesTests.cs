using AlienFarms.Core;
using AlienFarms.Networking.Social;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests.Social
{
    public sealed class FriendPresenceRulesTests
    {
        [TestCase(OnlineSessionStatus.SignedOut, false, FriendAvailability.Offline, null)]
        [TestCase(OnlineSessionStatus.SigningIn, false, FriendAvailability.Offline, null)]
        [TestCase(OnlineSessionStatus.Ready, false, FriendAvailability.Online, "hub")]
        [TestCase(OnlineSessionStatus.Creating, false, FriendAvailability.Online, "hub")]
        [TestCase(OnlineSessionStatus.Joining, false, FriendAvailability.Online, "hub")]
        [TestCase(OnlineSessionStatus.InSession, false, FriendAvailability.Online, "room")]
        [TestCase(OnlineSessionStatus.Leaving, false, FriendAvailability.Online, "hub")]
        [TestCase(OnlineSessionStatus.InSession, true, FriendAvailability.Busy, "match")]
        public void Resolve_MapsStatusAndMatch(OnlineSessionStatus status, bool inMatch,
            FriendAvailability expectedAvailability, string expectedActivity)
        {
            var presence = FriendPresenceRules.Resolve(status, inMatch);
            Assert.That(presence.Availability, Is.EqualTo(expectedAvailability));
            Assert.That(presence.ActivityState, Is.EqualTo(expectedActivity));
        }
    }
}
