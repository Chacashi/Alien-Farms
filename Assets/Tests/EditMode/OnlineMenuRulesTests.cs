using AlienFarms.Core;
using AlienFarms.Menu;
using AlienFarms.UI;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    /// <summary>Online menu rules: state -> screen table, busy labels, busy cancel and room hints.</summary>
    public sealed class OnlineMenuRulesTests
    {
        private const OnlineLobbyPhase Unavailable = OnlineLobbyPhase.Unavailable;
        private const OnlineLobbyPhase Open = OnlineLobbyPhase.Open;
        private const OnlineLobbyPhase Starting = OnlineLobbyPhase.Starting;

        // Full table: every status x lobby phase x wantsJoin.
        [TestCase(OnlineSessionStatus.SignedOut, Unavailable, false, OnlineScreen.Hub)]
        [TestCase(OnlineSessionStatus.SignedOut, Unavailable, true, OnlineScreen.Join)]
        [TestCase(OnlineSessionStatus.SignedOut, Open, false, OnlineScreen.Hub)]
        [TestCase(OnlineSessionStatus.SignedOut, Open, true, OnlineScreen.Join)]
        [TestCase(OnlineSessionStatus.SignedOut, Starting, false, OnlineScreen.Hub)]
        [TestCase(OnlineSessionStatus.SignedOut, Starting, true, OnlineScreen.Join)]
        [TestCase(OnlineSessionStatus.SigningIn, Unavailable, false, OnlineScreen.Busy)]
        [TestCase(OnlineSessionStatus.SigningIn, Unavailable, true, OnlineScreen.Busy)]
        [TestCase(OnlineSessionStatus.SigningIn, Open, false, OnlineScreen.Busy)]
        [TestCase(OnlineSessionStatus.SigningIn, Open, true, OnlineScreen.Busy)]
        [TestCase(OnlineSessionStatus.SigningIn, Starting, false, OnlineScreen.Busy)]
        [TestCase(OnlineSessionStatus.SigningIn, Starting, true, OnlineScreen.Busy)]
        [TestCase(OnlineSessionStatus.Ready, Unavailable, false, OnlineScreen.Hub)]
        [TestCase(OnlineSessionStatus.Ready, Unavailable, true, OnlineScreen.Join)]
        [TestCase(OnlineSessionStatus.Ready, Open, false, OnlineScreen.Hub)]
        [TestCase(OnlineSessionStatus.Ready, Open, true, OnlineScreen.Join)]
        [TestCase(OnlineSessionStatus.Ready, Starting, false, OnlineScreen.Hub)]
        [TestCase(OnlineSessionStatus.Ready, Starting, true, OnlineScreen.Join)]
        [TestCase(OnlineSessionStatus.Creating, Unavailable, false, OnlineScreen.Busy)]
        [TestCase(OnlineSessionStatus.Creating, Unavailable, true, OnlineScreen.Busy)]
        [TestCase(OnlineSessionStatus.Creating, Open, false, OnlineScreen.Busy)]
        [TestCase(OnlineSessionStatus.Creating, Open, true, OnlineScreen.Busy)]
        [TestCase(OnlineSessionStatus.Creating, Starting, false, OnlineScreen.Busy)]
        [TestCase(OnlineSessionStatus.Creating, Starting, true, OnlineScreen.Busy)]
        [TestCase(OnlineSessionStatus.Joining, Unavailable, false, OnlineScreen.Busy)]
        [TestCase(OnlineSessionStatus.Joining, Unavailable, true, OnlineScreen.Busy)]
        [TestCase(OnlineSessionStatus.Joining, Open, false, OnlineScreen.Busy)]
        [TestCase(OnlineSessionStatus.Joining, Open, true, OnlineScreen.Busy)]
        [TestCase(OnlineSessionStatus.Joining, Starting, false, OnlineScreen.Busy)]
        [TestCase(OnlineSessionStatus.Joining, Starting, true, OnlineScreen.Busy)]
        [TestCase(OnlineSessionStatus.InSession, Unavailable, false, OnlineScreen.Room)]
        [TestCase(OnlineSessionStatus.InSession, Unavailable, true, OnlineScreen.Room)]
        [TestCase(OnlineSessionStatus.InSession, Open, false, OnlineScreen.Room)]
        [TestCase(OnlineSessionStatus.InSession, Open, true, OnlineScreen.Room)]
        [TestCase(OnlineSessionStatus.InSession, Starting, false, OnlineScreen.Busy)]
        [TestCase(OnlineSessionStatus.InSession, Starting, true, OnlineScreen.Busy)]
        [TestCase(OnlineSessionStatus.Leaving, Unavailable, false, OnlineScreen.Busy)]
        [TestCase(OnlineSessionStatus.Leaving, Unavailable, true, OnlineScreen.Busy)]
        [TestCase(OnlineSessionStatus.Leaving, Open, false, OnlineScreen.Busy)]
        [TestCase(OnlineSessionStatus.Leaving, Open, true, OnlineScreen.Busy)]
        [TestCase(OnlineSessionStatus.Leaving, Starting, false, OnlineScreen.Busy)]
        [TestCase(OnlineSessionStatus.Leaving, Starting, true, OnlineScreen.Busy)]
        public void Resolve_MapsStateToScreen(OnlineSessionStatus status, OnlineLobbyPhase phase, bool wantsJoin, OnlineScreen expected)
        {
            Assert.That(OnlineMenuRules.Resolve(status, phase, wantsJoin), Is.EqualTo(expected));
        }

        [TestCase(OnlineSessionStatus.SignedOut, Unavailable, null)]
        [TestCase(OnlineSessionStatus.SigningIn, Unavailable, LocalizationKeys.OnlineBusySigningIn)]
        [TestCase(OnlineSessionStatus.Ready, Unavailable, null)]
        [TestCase(OnlineSessionStatus.Ready, Starting, null)]
        [TestCase(OnlineSessionStatus.Creating, Unavailable, LocalizationKeys.OnlineBusyCreating)]
        [TestCase(OnlineSessionStatus.Joining, Unavailable, LocalizationKeys.OnlineBusyJoining)]
        [TestCase(OnlineSessionStatus.InSession, Unavailable, null)]
        [TestCase(OnlineSessionStatus.InSession, Open, null)]
        [TestCase(OnlineSessionStatus.InSession, Starting, LocalizationKeys.OnlineBusyStarting)]
        [TestCase(OnlineSessionStatus.Leaving, Unavailable, LocalizationKeys.OnlineBusyLeaving)]
        [TestCase(OnlineSessionStatus.Leaving, Starting, LocalizationKeys.OnlineBusyLeaving)]
        public void BusyKey_MatchesStatus(OnlineSessionStatus status, OnlineLobbyPhase phase, string expected)
        {
            Assert.That(OnlineMenuRules.BusyKey(status, phase), Is.EqualTo(expected));
        }

        [Test]
        public void BusyKey_IsSetForEveryBusyScreen()
        {
            foreach (OnlineSessionStatus status in System.Enum.GetValues(typeof(OnlineSessionStatus)))
            {
                foreach (OnlineLobbyPhase phase in System.Enum.GetValues(typeof(OnlineLobbyPhase)))
                {
                    bool busy = OnlineMenuRules.Resolve(status, phase, false) == OnlineScreen.Busy;
                    string key = OnlineMenuRules.BusyKey(status, phase);
                    Assert.That(key != null, Is.EqualTo(busy), $"{status}/{phase}");
                }
            }
        }

        [TestCase(OnlineSessionStatus.SignedOut, false)]
        [TestCase(OnlineSessionStatus.SigningIn, true)]
        [TestCase(OnlineSessionStatus.Ready, false)]
        [TestCase(OnlineSessionStatus.Creating, true)]
        [TestCase(OnlineSessionStatus.Joining, true)]
        [TestCase(OnlineSessionStatus.InSession, false)]
        [TestCase(OnlineSessionStatus.Leaving, false)]
        public void CanCancelBusy_OnlyWhileSigningInCreatingOrJoining(OnlineSessionStatus status, bool expected)
        {
            Assert.That(OnlineMenuRules.CanCancelBusy(status), Is.EqualTo(expected));
        }

        [TestCase(OnlineSessionStatus.Joining, true, false)]
        [TestCase(OnlineSessionStatus.Joining, false, false)]
        [TestCase(OnlineSessionStatus.Creating, true, false)]
        [TestCase(OnlineSessionStatus.SigningIn, true, false)]
        [TestCase(OnlineSessionStatus.Ready, true, true)]
        [TestCase(OnlineSessionStatus.SignedOut, true, true)]
        [TestCase(OnlineSessionStatus.Ready, false, false)]
        public void WantsJoinAfterCancelBusy_ClearsOnCancelledOperations(OnlineSessionStatus status, bool wantsJoin, bool expected)
        {
            Assert.That(OnlineMenuRules.WantsJoinAfterCancelBusy(status, wantsJoin), Is.EqualTo(expected));
        }

        [TestCase(false, false, false)]
        [TestCase(true, false, true)]
        [TestCase(false, true, true)]
        [TestCase(true, true, true)]
        public void OpensOnlineOnLoad_InSessionOrAfterOnlineMatch(bool inSession, bool returnRequested, bool expected)
        {
            Assert.That(OnlineMenuRules.OpensOnlineOnLoad(inSession, returnRequested), Is.EqualTo(expected));
        }

        [Test]
        public void ReturnFromOnlineMatch_LandsOnHub()
        {
            // The NetworkRoot was torn down: signed in but out of the session.
            Assert.That(OnlineMenuRules.Resolve(OnlineSessionStatus.Ready, Unavailable, false), Is.EqualTo(OnlineScreen.Hub));
        }

        [Test]
        public void CancelJoin_ReturnsToHub()
        {
            // Join screen -> Joining (busy) -> Cancel -> the service leaves back to Ready.
            bool wantsJoin = OnlineMenuRules.WantsJoinAfterCancelBusy(OnlineSessionStatus.Joining, true);
            Assert.That(OnlineMenuRules.Resolve(OnlineSessionStatus.Ready, Unavailable, wantsJoin), Is.EqualTo(OnlineScreen.Hub));
        }

        [TestCase(Unavailable, 2, true, true, false, LocalizationKeys.OnlineRoomSyncing)]
        [TestCase(Unavailable, 0, false, false, true, LocalizationKeys.OnlineRoomSyncing)]
        [TestCase(Open, 2, true, false, true, LocalizationKeys.OnlineRoomSpectator)]
        [TestCase(Open, 0, false, true, false, LocalizationKeys.OnlineRoomWaitingPlayer)]
        [TestCase(Open, 1, false, true, false, LocalizationKeys.OnlineRoomWaitingPlayer)]
        [TestCase(Open, 1, false, false, false, LocalizationKeys.OnlineRoomWaitingPlayer)]
        [TestCase(Open, 2, false, true, false, LocalizationKeys.OnlineRoomNeedPicks)]
        [TestCase(Open, 2, false, false, false, LocalizationKeys.OnlineRoomNeedPicks)]
        [TestCase(Open, 2, true, false, false, LocalizationKeys.OnlineRoomWaitingHost)]
        [TestCase(Open, 2, true, true, false, null)]
        public void HintKey_ShowsWhatIsMissing(OnlineLobbyPhase phase, int seats, bool allPicked, bool isHost, bool isSpectator, string expected)
        {
            Assert.That(OnlineMenuRules.HintKey(phase, seats, allPicked, isHost, isSpectator), Is.EqualTo(expected));
        }
    }
}
