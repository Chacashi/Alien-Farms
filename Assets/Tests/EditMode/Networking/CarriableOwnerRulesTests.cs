using AlienFarms.Core;
using AlienFarms.Networking.Player;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests
{
    /// <summary>NGO ownership → owner shown by the tint / label / log of carried objects (task 2.4).</summary>
    public sealed class CarriableOwnerRulesTests
    {
        private const ulong Server = 0;
        private const ulong Client1 = 1;

        [Test]
        public void ClientOwner_IsItsSeat()
        {
            Assert.That(CarriableOwnerRules.Resolve(Client1, Server, PlayerId.Player2, PlayerId.Player2),
                Is.EqualTo(CarriableOwner.ForSeat(PlayerId.Player2)));
        }

        [Test]
        public void ClientOwner_WithoutKnownAvatar_IsUnresolved()
        {
            Assert.That(CarriableOwnerRules.Resolve(Client1, Server, PlayerId.Player2, PlayerId.None),
                Is.EqualTo(CarriableOwner.Unresolved));
        }

        [Test]
        public void ArmedBombOwner_IsTheThrowerSeat_EvenWithoutClaim()
        {
            Assert.That(CarriableOwnerRules.Resolve(Client1, Server, PlayerId.None, PlayerId.Player2),
                Is.EqualTo(CarriableOwner.ForSeat(PlayerId.Player2)));
        }

        [Test]
        public void ServerOwner_Unclaimed_IsServer()
        {
            Assert.That(CarriableOwnerRules.Resolve(Server, Server, PlayerId.None, PlayerId.Player1), Is.EqualTo(CarriableOwner.Server));
            Assert.That(CarriableOwnerRules.Resolve(Server, Server, PlayerId.None, PlayerId.None), Is.EqualTo(CarriableOwner.Server));
        }

        [Test]
        public void ServerOwner_ClaimedByTheHostSeat_IsTheHostSeat()
        {
            Assert.That(CarriableOwnerRules.Resolve(Server, Server, PlayerId.Player1, PlayerId.Player1),
                Is.EqualTo(CarriableOwner.ForSeat(PlayerId.Player1)));
        }

        [Test]
        public void ServerOwner_ClaimedByAnotherSeat_IsServer()
        {
            // Transient: the holder's client just disconnected and NGO gave the object back before the drop.
            Assert.That(CarriableOwnerRules.Resolve(Server, Server, PlayerId.Player2, PlayerId.Player1), Is.EqualTo(CarriableOwner.Server));
        }

        [Test]
        public void ServerOwner_ClaimedWithoutKnownHostAvatar_IsUnresolved()
        {
            Assert.That(CarriableOwnerRules.Resolve(Server, Server, PlayerId.Player1, PlayerId.None), Is.EqualTo(CarriableOwner.Unresolved));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Label_ShowsWhenNotServerOwned(bool isPureClient)
        {
            Assert.That(CarriableOwnerRules.IsLabelShown(CarriableOwner.Server, PlayerId.None, isPureClient), Is.False);
            Assert.That(CarriableOwnerRules.IsLabelShown(CarriableOwner.ForSeat(PlayerId.Player2), PlayerId.None, isPureClient), Is.True);
            Assert.That(CarriableOwnerRules.IsLabelShown(CarriableOwner.ForSeat(PlayerId.Player1), PlayerId.Player1, isPureClient), Is.True);
            Assert.That(CarriableOwnerRules.IsLabelShown(CarriableOwner.Unresolved, PlayerId.None, isPureClient), Is.True);
        }

        [Test]
        public void Label_ClaimedButServerOwned_ShowsOnTheServerOnly()
        {
            // Pure client: ownership and replicated state arrive separately on grab / release; no "Owner: Server" flash.
            Assert.That(CarriableOwnerRules.IsLabelShown(CarriableOwner.Server, PlayerId.Player2, false), Is.True);
            Assert.That(CarriableOwnerRules.IsLabelShown(CarriableOwner.Server, PlayerId.Player2, true), Is.False);
        }

        [Test]
        public void PreviousOwnerSeat_PrefersLookupThenCacheThenClaimant()
        {
            Assert.That(CarriableOwnerRules.PreviousOwnerSeat(PlayerId.Player2, PlayerId.Player1, PlayerId.Player1), Is.EqualTo(PlayerId.Player2));

            // Disconnect: the leaving avatar is already the server's, so only the cached seat names P2.
            Assert.That(CarriableOwnerRules.PreviousOwnerSeat(PlayerId.None, PlayerId.Player2, PlayerId.None), Is.EqualTo(PlayerId.Player2));
            Assert.That(CarriableOwnerRules.PreviousOwnerSeat(PlayerId.None, PlayerId.None, PlayerId.Player2), Is.EqualTo(PlayerId.Player2));
            Assert.That(CarriableOwnerRules.PreviousOwnerSeat(PlayerId.None, PlayerId.None, PlayerId.None), Is.EqualTo(PlayerId.None));
        }

        [Test]
        public void Tags_AreStablePerOwner()
        {
            Assert.That(CarriableOwnerRules.Tag(CarriableOwner.Server), Is.EqualTo("Server"));
            Assert.That(CarriableOwnerRules.Tag(CarriableOwner.ForSeat(PlayerId.Player1)), Is.EqualTo("P1"));
            Assert.That(CarriableOwnerRules.Tag(CarriableOwner.ForSeat(PlayerId.Player2)), Is.EqualTo("P2"));
            Assert.That(CarriableOwnerRules.Tag(CarriableOwner.Unresolved), Is.EqualTo("?"));
            Assert.That(CarriableOwner.ForSeat(PlayerId.None), Is.EqualTo(CarriableOwner.Server));
        }

        [Test]
        public void TagIndices_AreDistinctAndInRange()
        {
            int server = CarriableOwnerRules.TagIndex(CarriableOwner.Server);
            int p1 = CarriableOwnerRules.TagIndex(CarriableOwner.ForSeat(PlayerId.Player1));
            int p2 = CarriableOwnerRules.TagIndex(CarriableOwner.ForSeat(PlayerId.Player2));
            int unresolved = CarriableOwnerRules.TagIndex(CarriableOwner.Unresolved);

            Assert.That(new[] { server, p1, p2, unresolved }, Is.Unique);
            Assert.That(new[] { server, p1, p2, unresolved }, Is.All.InRange(0, CarriableOwnerRules.TagCount - 1));
        }

        [Test]
        public void DescribeClient_NamesSeatAndClientId()
        {
            Assert.That(CarriableOwnerRules.DescribeClient(Client1, Server, PlayerId.Player2), Is.EqualTo("P2 (client 1)"));
            Assert.That(CarriableOwnerRules.DescribeClient(Server, Server, PlayerId.Player1), Is.EqualTo("Server/P1 (client 0)"));
            Assert.That(CarriableOwnerRules.DescribeClient(Server, Server, PlayerId.None), Is.EqualTo("Server (client 0)"));
            Assert.That(CarriableOwnerRules.DescribeClient(7, Server, PlayerId.None), Is.EqualTo("? (client 7)"));
        }
    }
}
