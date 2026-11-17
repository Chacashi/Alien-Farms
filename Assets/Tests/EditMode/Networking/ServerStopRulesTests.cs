using AlienFarms.Networking.Session;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests
{
    /// <summary>While the host leaves, disconnections only free places: no grace, no Left, no lobby removal (B3).</summary>
    public sealed class ServerStopRulesTests
    {
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void OnDisconnect_ServerStopping_AlwaysReleased(bool noticed, bool grace)
        {
            Assert.That(SeatGraceRules.OnDisconnect(noticed, grace, true), Is.EqualTo(SeatDropOutcome.Released));
        }

        [TestCase(false, false, SeatDropOutcome.Released)]
        [TestCase(false, true, SeatDropOutcome.Reserved)]
        [TestCase(true, true, SeatDropOutcome.Left)]
        public void OnDisconnect_NotStopping_SameAsBefore(bool noticed, bool grace, SeatDropOutcome expected)
        {
            Assert.That(SeatGraceRules.OnDisconnect(noticed, grace, false), Is.EqualTo(expected));
            Assert.That(SeatGraceRules.OnDisconnect(noticed, grace), Is.EqualTo(expected));
        }

        [Test]
        public void ShouldRemoveSpectatorMember_SilentDropWhileRunning_Removes()
        {
            Assert.That(SeatGraceRules.ShouldRemoveSpectatorMember(false, false, true, false), Is.True);
        }

        [Test]
        public void ShouldRemoveSpectatorMember_ServerStopping_NeverRemoves()
        {
            Assert.That(SeatGraceRules.ShouldRemoveSpectatorMember(false, true, true, false), Is.False);
        }

        [TestCase(true, true, false)]
        [TestCase(false, false, false)]
        [TestCase(false, true, true)]
        public void ShouldRemoveSpectatorMember_NoticedUnknownOrReusedId_Keeps(bool noticed, bool hasId, bool inUse)
        {
            Assert.That(SeatGraceRules.ShouldRemoveSpectatorMember(noticed, false, hasId, inUse), Is.False);
        }
    }
}
