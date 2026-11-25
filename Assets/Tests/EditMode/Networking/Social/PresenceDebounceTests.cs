using AlienFarms.Core;
using AlienFarms.Networking.Social;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests.Social
{
    public sealed class PresenceDebounceTests
    {
        private static readonly (FriendAvailability, string) Hub = (FriendAvailability.Online, FriendPresenceActivity.Hub);
        private static readonly (FriendAvailability, string) Room = (FriendAvailability.Online, FriendPresenceActivity.Room);
        private static readonly (FriendAvailability, string) Match = (FriendAvailability.Busy, FriendPresenceActivity.Match);

        [Test]
        public void Schedule_PublishesOnlyTheLatestChangeAfterItsDelay()
        {
            var debounce = new PresenceDebounce();
            debounce.Schedule(Room, 1d, 1d);
            debounce.Schedule(Match, 1.5d, 1d);
            debounce.Schedule(Match, 2d, 1d);

            Assert.That(debounce.TryTake(2.49d, out _), Is.False);
            Assert.That(debounce.TryTake(2.5d, out var desired), Is.True);
            Assert.That(desired, Is.EqualTo(Match));
            Assert.That(debounce.TryTake(3d, out _), Is.False);
        }

        [Test]
        public void Schedule_WithZeroDelayPublishesImmediately()
        {
            var debounce = new PresenceDebounce();
            debounce.Schedule(Hub, 1d, 0d);

            Assert.That(debounce.TryTake(1d, out var desired), Is.True);
            Assert.That(desired, Is.EqualTo(Hub));
        }

        [Test]
        public void Retry_PreservesNewerPresenceAndResetDiscardsPendingPresence()
        {
            var debounce = new PresenceDebounce();
            debounce.Schedule(Hub, 1d, 0d);
            Assert.That(debounce.TryTake(1d, out _), Is.True);

            debounce.Schedule(Room, 2d, 1d);
            debounce.Retry(Hub, 5d);
            Assert.That(debounce.TryTake(3d, out var desired), Is.True);
            Assert.That(desired, Is.EqualTo(Room));

            debounce.Retry(Room, 5d);
            Assert.That(debounce.TryTake(4d, out _), Is.False);
            debounce.Reset();
            Assert.That(debounce.TryTake(5d, out _), Is.False);
        }
    }
}
