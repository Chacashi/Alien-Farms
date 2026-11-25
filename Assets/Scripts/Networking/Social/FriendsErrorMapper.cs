using AlienFarms.Core;

namespace AlienFarms.Networking.Social
{
    public static class FriendsErrorMapper
    {
        public static string Map(int errorCode) => errorCode switch
        {
            24003 => FriendsErrorKeys.RequestExists,
            24008 => FriendsErrorKeys.Self,
            24011 => FriendsErrorKeys.Blocked,
            24012 => FriendsErrorKeys.AlreadyFriends,
            24013 => FriendsErrorKeys.NotFriend,
            24022 => FriendsErrorKeys.Payload,
            24024 => FriendsErrorKeys.NotEnabled,
            24044 or 24045 => FriendsErrorKeys.LimitReached,
            24429 => FriendsErrorKeys.RateLimited,
            24602 => FriendsErrorKeys.Network,
            _ => FriendsErrorKeys.Unknown
        };
    }
}
