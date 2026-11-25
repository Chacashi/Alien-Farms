using AlienFarms.Core;

namespace AlienFarms.Networking.Social
{
    public static class FriendPresenceRules
    {
        public static (FriendAvailability Availability, string ActivityState) Resolve(OnlineSessionStatus status, bool inMatch)
        {
            if (inMatch)
            {
                return (FriendAvailability.Busy, FriendActivityKeys.Match);
            }

            return status switch
            {
                OnlineSessionStatus.SignedOut or OnlineSessionStatus.SigningIn => (FriendAvailability.Offline, null),
                OnlineSessionStatus.InSession => (FriendAvailability.Online, FriendActivityKeys.Room),
                _ => (FriendAvailability.Online, FriendActivityKeys.Hub)
            };
        }
    }
}
