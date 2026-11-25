using AlienFarms.Core;
using AlienFarms.Networking.Social;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests.Social
{
    public sealed class FriendListBuilderTests
    {
        [Test]
        public void Build_RanksByPresenceThenNameAndSkipsMissingIds()
        {
            var destination = new FriendEntry[6];

            Assert.That(FriendListBuilder.Build(CreateFriends(), destination), Is.EqualTo(5));
            Assert.That(destination[0].DisplayName, Is.EqualTo("alice"));
            Assert.That(destination[1].DisplayName, Is.EqualTo("Bob"));
            Assert.That(destination[2].Availability, Is.EqualTo(FriendAvailability.Busy));
            Assert.That(destination[3].Availability, Is.EqualTo(FriendAvailability.Away));
            Assert.That(destination[4].Availability, Is.EqualTo(FriendAvailability.Offline));
            Assert.That(destination[5].UgsPlayerId, Is.Null);
        }

        [Test]
        public void Build_KeepsBestEntriesEvenIfTheyArriveLastAndClearsOldRows()
        {
            var destination = new FriendEntry[2];
            Assert.That(FriendListBuilder.Build(CreateFriends(), destination), Is.EqualTo(2));
            Assert.That(destination[0].DisplayName, Is.EqualTo("alice"));
            Assert.That(destination[1].DisplayName, Is.EqualTo("Bob"));

            Assert.That(FriendListBuilder.Build(null, destination), Is.Zero);
            Assert.That(destination[0].UgsPlayerId, Is.Null);
            Assert.That(destination[1].UgsPlayerId, Is.Null);
        }

        [Test]
        public void BuildRequests_PrioritizesIncomingAndSkipsMissingIds()
        {
            var destination = new FriendRequestEntry[1];
            var incoming = new FriendRequestEntry[] { default, new FriendRequestEntry("in", null, true) };
            var outgoing = new[] { new FriendRequestEntry("out", null, false) };

            Assert.That(FriendListBuilder.BuildRequests(incoming, outgoing, destination), Is.EqualTo(1));
            Assert.That(destination[0].UgsPlayerId, Is.EqualTo("in"));
            Assert.That(destination[0].Incoming, Is.True);
        }

        private static FriendEntry[] CreateFriends() => new FriendEntry[]
        {
            new FriendEntry("a", "Aaron", FriendAvailability.Offline, null, 0),
            default,
            new FriendEntry("b", "Bob", FriendAvailability.Online, null, 0),
            new FriendEntry("d", "Dan", FriendAvailability.Away, null, 0),
            new FriendEntry("c", "Charlie", FriendAvailability.Busy, null, 0),
            new FriendEntry("e", "alice", FriendAvailability.Online, null, 0)
        };
    }
}
