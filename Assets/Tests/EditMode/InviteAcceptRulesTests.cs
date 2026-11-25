using AlienFarms.Core;
using AlienFarms.Menu;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class InviteAcceptRulesTests
    {
        [TestCase(OnlineSessionStatus.SignedOut, InviteAcceptRules.Action.JoinNow)]
        [TestCase(OnlineSessionStatus.SigningIn, InviteAcceptRules.Action.Blocked)]
        [TestCase(OnlineSessionStatus.Ready, InviteAcceptRules.Action.JoinNow)]
        [TestCase(OnlineSessionStatus.Creating, InviteAcceptRules.Action.Blocked)]
        [TestCase(OnlineSessionStatus.Joining, InviteAcceptRules.Action.Blocked)]
        [TestCase(OnlineSessionStatus.InSession, InviteAcceptRules.Action.LeaveThenJoin)]
        [TestCase(OnlineSessionStatus.Leaving, InviteAcceptRules.Action.Blocked)]
        public void Decide_HandlesEverySessionStatus(OnlineSessionStatus status, InviteAcceptRules.Action expected)
        {
            var decision = InviteAcceptRules.Decide(status, false, false, false);
            Assert.That(decision.Next, Is.EqualTo(expected));
            Assert.That(decision.ErrorKey,
                Is.EqualTo(expected == InviteAcceptRules.Action.Blocked ? OnlineSessionErrorKeys.Busy : null));
        }

        [Test]
        public void Decide_DeclinesRejoinBeforeJoining()
        {
            Assert.That(InviteAcceptRules.Decide(OnlineSessionStatus.Ready, false, true, false).Next,
                Is.EqualTo(InviteAcceptRules.Action.DeclineRejoinThenJoin));
        }

        [TestCase(true, false, OnlineSessionErrorKeys.Busy)]
        [TestCase(false, true, OnlineSessionErrorKeys.InviteExpired)]
        public void Decide_BlocksDuringMatchOrAfterExpiration(bool inMatch, bool expired, string expected)
        {
            var decision = InviteAcceptRules.Decide(OnlineSessionStatus.InSession, inMatch, false, expired);
            Assert.That(decision.Next, Is.EqualTo(InviteAcceptRules.Action.Blocked));
            Assert.That(decision.ErrorKey, Is.EqualTo(expected));
        }
    }
}
