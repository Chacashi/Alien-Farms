using AlienFarms.Core;
using AlienFarms.Menu;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class InviteSendRulesTests
    {
        [TestCase(OnlineSessionStatus.SignedOut, false)]
        [TestCase(OnlineSessionStatus.SigningIn, false)]
        [TestCase(OnlineSessionStatus.Ready, false)]
        [TestCase(OnlineSessionStatus.Creating, false)]
        [TestCase(OnlineSessionStatus.Joining, false)]
        [TestCase(OnlineSessionStatus.InSession, true)]
        [TestCase(OnlineSessionStatus.Leaving, false)]
        public void CanInvite_RequiresRoom(OnlineSessionStatus status, bool expected)
        {
            Assert.That(InviteSendRules.CanInvite(true, status, 1, FriendAvailability.Online, false, null, 0, 5),
                Is.EqualTo(expected));
        }

        [TestCase(false, 1, FriendAvailability.Online, false)]
        [TestCase(true, 2, FriendAvailability.Online, false)]
        [TestCase(true, 1, FriendAvailability.Offline, false)]
        [TestCase(true, 1, FriendAvailability.Busy, false)]
        [TestCase(true, 1, FriendAvailability.Online, true)]
        public void CanInvite_RejectsIneligibleSend(bool host, int count, FriendAvailability availability, bool inMatch)
        {
            Assert.That(InviteSendRules.CanInvite(host, OnlineSessionStatus.InSession, count, availability,
                inMatch, null, 0, 5), Is.False);
        }

        [TestCase(4.999, false)]
        [TestCase(5.0, true)]
        public void CanInvite_AllowsAtCooldownBoundary(double now, bool expected)
        {
            Assert.That(InviteSendRules.CanInvite(true, OnlineSessionStatus.InSession, 1, FriendAvailability.Online,
                false, 0, now, 5), Is.EqualTo(expected));
        }
    }
}
