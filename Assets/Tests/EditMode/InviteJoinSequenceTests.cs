using AlienFarms.Core;
using AlienFarms.Menu;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class InviteJoinSequenceTests
    {
        [TestCase(OnlineSessionStatus.SigningIn, false, InviteJoinSequence.Step.Wait)]
        [TestCase(OnlineSessionStatus.Leaving, false, InviteJoinSequence.Step.Wait)]
        [TestCase(OnlineSessionStatus.Ready, false, InviteJoinSequence.Step.Join)]
        [TestCase(OnlineSessionStatus.Ready, true, InviteJoinSequence.Step.DeclineRejoin)]
        [TestCase(OnlineSessionStatus.SignedOut, false, InviteJoinSequence.Step.Cancel)]
        [TestCase(OnlineSessionStatus.Creating, false, InviteJoinSequence.Step.Cancel)]
        [TestCase(OnlineSessionStatus.Joining, false, InviteJoinSequence.Step.Cancel)]
        [TestCase(OnlineSessionStatus.InSession, false, InviteJoinSequence.Step.Cancel)]
        public void DecideUsesCurrentSessionState(OnlineSessionStatus status, bool hasRejoinOffer,
            InviteJoinSequence.Step expected)
        {
            Assert.That(InviteJoinSequence.Decide(MenuPhase.Online, status, hasRejoinOffer, false, 10d, 9d),
                Is.EqualTo(expected));
        }

        [TestCase(OnlineSessionStatus.SigningIn)]
        [TestCase(OnlineSessionStatus.Leaving)]
        [TestCase(OnlineSessionStatus.Ready)]
        public void ExpirationAtDeadlinePreventsJoin(OnlineSessionStatus status)
        {
            Assert.That(InviteJoinSequence.Decide(MenuPhase.Online, status, false, false, 10d, 10d),
                Is.EqualTo(InviteJoinSequence.Step.Expire));
        }

        [TestCase(MenuPhase.Title, false)]
        [TestCase(MenuPhase.Online, true)]
        public void LeavingOnlineMenuOrStartingMatchCancelsPendingJoin(MenuPhase phase, bool inMatch)
        {
            Assert.That(InviteJoinSequence.Decide(phase, OnlineSessionStatus.Ready, false, inMatch, 10d, 9d),
                Is.EqualTo(InviteJoinSequence.Step.Cancel));
        }
    }
}
