using AlienFarms.Core;
using AlienFarms.UI;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    /// <summary>HUD connection banner rules: status -> banner, toasts, keys, displayed seconds and template escaping.</summary>
    public sealed class ConnectionStatusRulesTests
    {
        [TestCase(PlayerConnectionStatus.Reconnecting, false, ConnectionBanner.Reconnecting)]
        [TestCase(PlayerConnectionStatus.Reconnecting, true, ConnectionBanner.Reconnecting)]
        [TestCase(PlayerConnectionStatus.Connected, true, ConnectionBanner.Returned)]
        [TestCase(PlayerConnectionStatus.Connected, false, ConnectionBanner.None)]
        [TestCase(PlayerConnectionStatus.Left, false, ConnectionBanner.Left)]
        [TestCase(PlayerConnectionStatus.Left, true, ConnectionBanner.Left)]
        [TestCase(PlayerConnectionStatus.TimedOut, true, ConnectionBanner.TimedOut)]
        [TestCase(PlayerConnectionStatus.TimedOut, false, ConnectionBanner.TimedOut)]
        public void Resolve_MapsStatusAndHistory(PlayerConnectionStatus status, bool wasReconnecting, ConnectionBanner expected)
        {
            Assert.That(ConnectionStatusRules.Resolve(status, 12f, wasReconnecting), Is.EqualTo(expected));
        }

        [Test]
        public void Resolve_LateInstanceConnectedSeat_ShowsNothing()
        {
            // A late spectator receives Connected for every held seat on spawn: that is not news.
            Assert.That(ConnectionStatusRules.Resolve(PlayerConnectionStatus.Connected, 0f, false), Is.EqualTo(ConnectionBanner.None));
        }

        [TestCase(ConnectionBanner.None, false)]
        [TestCase(ConnectionBanner.Reconnecting, false)]
        [TestCase(ConnectionBanner.Returned, true)]
        [TestCase(ConnectionBanner.Left, true)]
        [TestCase(ConnectionBanner.TimedOut, true)]
        public void IsToast_OnlyForOneShotMessages(ConnectionBanner banner, bool expected)
        {
            Assert.That(ConnectionStatusRules.IsToast(banner), Is.EqualTo(expected));
        }

        [TestCase(ConnectionBanner.Reconnecting, LocalizationKeys.OnlineStatusReconnecting)]
        [TestCase(ConnectionBanner.Returned, LocalizationKeys.OnlineStatusReturned)]
        [TestCase(ConnectionBanner.Left, LocalizationKeys.OnlineStatusLeft)]
        [TestCase(ConnectionBanner.TimedOut, LocalizationKeys.OnlineStatusTimedOut)]
        public void Key_MatchesBanner(ConnectionBanner banner, string expected)
        {
            Assert.That(ConnectionStatusRules.Key(banner), Is.EqualTo(expected));
        }

        [Test]
        public void Key_None_IsNull()
        {
            Assert.That(ConnectionStatusRules.Key(ConnectionBanner.None), Is.Null);
        }

        [TestCase(20f, 20)]
        [TestCase(19.99f, 20)]
        [TestCase(19.01f, 20)]
        [TestCase(19f, 19)]
        [TestCase(0.2f, 1)]
        [TestCase(0f, 0)]
        [TestCase(-3f, 0)]
        public void DisplaySeconds_RoundsUpAndClampsAtZero(float graceLeft, int expected)
        {
            Assert.That(ConnectionStatusRules.DisplaySeconds(graceLeft), Is.EqualTo(expected));
        }

        [TestCase(PlayerConnectionStatus.Reconnecting, true)]
        [TestCase(PlayerConnectionStatus.Connected, false)]
        [TestCase(PlayerConnectionStatus.Left, false)]
        [TestCase(PlayerConnectionStatus.TimedOut, false)]
        public void IsAwaiting_OnlyWhileReconnecting(PlayerConnectionStatus status, bool expected)
        {
            Assert.That(ConnectionStatusRules.IsAwaiting(status), Is.EqualTo(expected));
        }

        [Test]
        public void Sequence_DropThenReturn_ShowsCountdownThenReturned()
        {
            bool awaiting = false;
            ConnectionBanner first = ConnectionStatusRules.Resolve(PlayerConnectionStatus.Reconnecting, 20f, awaiting);
            awaiting = ConnectionStatusRules.IsAwaiting(PlayerConnectionStatus.Reconnecting);
            ConnectionBanner second = ConnectionStatusRules.Resolve(PlayerConnectionStatus.Connected, 0f, awaiting);
            awaiting = ConnectionStatusRules.IsAwaiting(PlayerConnectionStatus.Connected);
            ConnectionBanner third = ConnectionStatusRules.Resolve(PlayerConnectionStatus.Connected, 0f, awaiting);

            Assert.That(first, Is.EqualTo(ConnectionBanner.Reconnecting));
            Assert.That(second, Is.EqualTo(ConnectionBanner.Returned));
            Assert.That(third, Is.EqualTo(ConnectionBanner.None));
        }

        [Test]
        public void EscapeForTemplate_ReplacesBraces()
        {
            Assert.That(ConnectionStatusRules.EscapeForTemplate("P2 · {Ana}"), Is.EqualTo("P2 · (Ana)"));
        }

        [Test]
        public void EscapeForTemplate_CleanText_ReturnsSameInstance()
        {
            const string text = "P2 · Ana";
            Assert.That(ConnectionStatusRules.EscapeForTemplate(text), Is.SameAs(text));
        }

        [Test]
        public void EscapeForTemplate_Null_ReturnsEmpty()
        {
            Assert.That(ConnectionStatusRules.EscapeForTemplate(null), Is.Empty);
        }
    }
}
