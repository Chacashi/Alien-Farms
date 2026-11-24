using System;
using AlienFarms.Core;

namespace AlienFarms.Networking.Persistence
{
    /// <summary>Whose public stats the online room shows as the rival's. No allocation.</summary>
    public static class RivalRules
    {
        /// <summary>
        /// The first remote participant with a UGS id, if one exists.
        /// </summary>
        public static bool TryFindRival(ReadOnlySpan<OnlineParticipant> participants, out string ugsPlayerId)
        {
            ugsPlayerId = null;
            for (int i = 0; i < participants.Length; i++)
            {
                OnlineParticipant participant = participants[i];
                if (!participant.IsLocal && !string.IsNullOrEmpty(participant.UgsPlayerId))
                {
                    ugsPlayerId = participant.UgsPlayerId;
                    return true;
                }
            }

            return false;
        }
    }
}
