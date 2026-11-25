using AlienFarms.Core;
using AlienFarms.Networking.Social;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests.Social
{
    public sealed class FriendsErrorMapperTests
    {
        [TestCase(24003, FriendsErrorKeys.RequestExists)]
        [TestCase(24008, FriendsErrorKeys.Self)]
        [TestCase(24011, FriendsErrorKeys.Blocked)]
        [TestCase(24012, FriendsErrorKeys.AlreadyFriends)]
        [TestCase(24013, FriendsErrorKeys.NotFriend)]
        [TestCase(24022, FriendsErrorKeys.Payload)]
        [TestCase(24024, FriendsErrorKeys.NotEnabled)]
        [TestCase(24602, FriendsErrorKeys.Network)]
        [TestCase(-1, FriendsErrorKeys.Unknown)]
        public void Map_ReturnsLocalizationKey(int code, string key)
        {
            Assert.That(FriendsErrorMapper.Map(code), Is.EqualTo(key));
        }
    }
}
