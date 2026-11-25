using System.Reflection;
using AlienFarms.Core;
using AlienFarms.Data;
using UnityEngine;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class FriendsChannelTests
    {
        private FriendsChannel _channel;

        [SetUp]
        public void SetUp() => _channel = ScriptableObject.CreateInstance<FriendsChannel>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_channel);

        [Test]
        public void RequestsWithoutListeners_ReportNotEnabled()
        {
            Assert.That(_channel.IsAvailable, Is.False);
            Assert.That(_channel.RequestAddFriend("friend"), Is.False);
            Assert.That(_channel.RequestAcceptRequest("friend"), Is.False);
            Assert.That(_channel.RequestDeclineRequest("friend"), Is.False);
            Assert.That(_channel.RequestCancelOutgoing("friend"), Is.False);
            Assert.That(_channel.RequestInvite("friend"), Is.False);
            Assert.That(_channel.RequestRefresh(), Is.False);
            Assert.That(_channel.LastErrorKey, Is.EqualTo(FriendsErrorKeys.NotEnabled));
        }

        [Test]
        public void RequestWithListener_DispatchesAndPublishesAvailability()
        {
            string received = null;
            int changes = 0;
            void Receive(string id) => received = id;
            _channel.Changed += () => changes++;

            _channel.InviteRequested += Receive;
            Assert.That(_channel.IsAvailable, Is.True);
            Assert.That(_channel.RequestInvite("friend"), Is.True);
            Assert.That(received, Is.EqualTo("friend"));

            _channel.InviteRequested -= Receive;
            Assert.That(_channel.IsAvailable, Is.False);
            Assert.That(changes, Is.EqualTo(2));
        }

        [Test]
        public void NewGeneration_DiscardsStateAndRequestListeners()
        {
            _channel.SetStatus(FriendsServiceStatus.Ready);
            _channel.SetLocalPlayerId("local");
            _channel.SetNotificationsConnected(true);
            _channel.SetLastInfoKey(FriendsErrorKeys.InviteSent);
            _channel.SetPendingInvite(CreateOffer("first"));
            _channel.SetFriends(new[] { new FriendEntry("friend", null, FriendAvailability.Online, null, 0) }, 1);
            _channel.RefreshRequested += () => Assert.Fail("Stale listener invoked");

            MethodInfo reset = typeof(FriendsChannel).GetMethod("ResetStatics", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(reset, Is.Not.Null);
            reset.Invoke(null, null);

            Assert.That(_channel.Status, Is.EqualTo(FriendsServiceStatus.Off));
            Assert.That(_channel.LocalPlayerId, Is.Null);
            Assert.That(_channel.NotificationsConnected, Is.False);
            Assert.That(_channel.LastInfoKey, Is.Null);
            Assert.That(_channel.HasInvite, Is.False);
            Assert.That(_channel.FriendCount, Is.Zero);
            Assert.That(_channel.IsAvailable, Is.False);
            Assert.That(_channel.RequestRefresh(), Is.False);
        }

        [Test]
        public void Lists_CopyAndClampToFixedCapacity()
        {
            Assert.That(_channel.Capacity, Is.EqualTo(16));
            _channel.ConfigureCapacity(1);
            var friends = new[]
            {
                new FriendEntry("first", null, FriendAvailability.Online, null, 0),
                new FriendEntry("second", null, FriendAvailability.Online, null, 0)
            };
            var requests = new[]
            {
                new FriendRequestEntry("incoming", null, true),
                new FriendRequestEntry("outgoing", null, false)
            };

            _channel.SetFriends(friends, friends.Length);
            _channel.SetRequests(requests, requests.Length);
            friends[0] = default;
            requests[0] = default;

            Assert.That(_channel.FriendCount, Is.EqualTo(1));
            Assert.That(_channel.RequestCount, Is.EqualTo(1));
            Assert.That(_channel.GetFriend(0).UgsPlayerId, Is.EqualTo("first"));
            Assert.That(_channel.GetRequest(0).UgsPlayerId, Is.EqualTo("incoming"));
            Assert.That(_channel.GetFriend(1).UgsPlayerId, Is.Null);

            _channel.SetFriends(null, 0);
            _channel.SetRequests(null, 0);
            Assert.That(_channel.GetFriend(0).UgsPlayerId, Is.Null);
            Assert.That(_channel.GetRequest(0).UgsPlayerId, Is.Null);
            Assert.That(_channel.Capacity, Is.EqualTo(1));
        }

        [Test]
        public void PendingInvite_ReplacesOlderOfferAndCanBeConsumedOrDeclined()
        {
            _channel.SetPendingInvite(CreateOffer("first"));
            _channel.SetPendingInvite(CreateOffer("second"));

            Assert.That(_channel.ConsumeInvite(out var offer), Is.True);
            Assert.That(offer.SessionId, Is.EqualTo("second"));
            Assert.That(_channel.ConsumeInvite(out _), Is.False);

            _channel.SetPendingInvite(CreateOffer("third"));
            _channel.DeclineInvite();
            Assert.That(_channel.HasInvite, Is.False);
        }

        private static SessionInviteOffer CreateOffer(string sessionId) =>
            new SessionInviteOffer("host", "Host", "BCDFGH", sessionId, 120);
    }
}
