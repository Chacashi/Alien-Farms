using AlienFarms.Core;
using AlienFarms.Data;

namespace AlienFarms.Menu
{
    public static class InviteSendRules
    {
        /// <summary>Use a null last-send time before the first successful send to this friend.</summary>
        public static bool CanInvite(bool isHost, OnlineSessionStatus status, int participantCount,
            FriendAvailability availability, bool inMatch, double? lastSentAt, double now, double cooldown)
        {
            return isHost && status == OnlineSessionStatus.InSession && participantCount < OnlineSessionChannel.MaxParticipants &&
                availability == FriendAvailability.Online && !inMatch &&
                (!lastSentAt.HasValue || now - lastSentAt.Value >= cooldown);
        }
    }
}
