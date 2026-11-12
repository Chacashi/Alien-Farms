using AlienFarms.Core;
using AlienFarms.Networking.Session;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests
{
    /// <summary>
    /// MatchSession mode per online status: Online from the moment a start begins (the host may sync the gameplay scene
    /// before the start returns), Local when idle or signed out.
    /// </summary>
    public sealed class OnlineModeRulesTests
    {
        [TestCase(OnlineSessionStatus.Creating)]
        [TestCase(OnlineSessionStatus.Joining)]
        [TestCase(OnlineSessionStatus.InSession)]
        [TestCase(OnlineSessionStatus.Leaving)]
        public void StartingOrInASession_IsOnline(OnlineSessionStatus status)
        {
            Assert.That(OnlineModeRules.ModeFor(status), Is.EqualTo(GameMode.Online));
        }

        [TestCase(OnlineSessionStatus.SignedOut)]
        [TestCase(OnlineSessionStatus.SigningIn)]
        [TestCase(OnlineSessionStatus.Ready)]
        public void Idle_IsLocal(OnlineSessionStatus status)
        {
            Assert.That(OnlineModeRules.ModeFor(status), Is.EqualTo(GameMode.Local));
        }

        [Test]
        public void JoiningAMatchInProgress_IsOnlineBeforeTheSceneSyncs_AndLocalAfterAFailure()
        {
            // BeginStart (Join / Rejoin) sets the mode from Joining; a failed start goes back to Ready.
            Assert.That(OnlineModeRules.ModeFor(OnlineSessionStatus.Joining), Is.EqualTo(GameMode.Online));
            Assert.That(OnlineModeRules.ModeFor(OnlineSessionStatus.Ready), Is.EqualTo(GameMode.Local));
        }
    }
}
