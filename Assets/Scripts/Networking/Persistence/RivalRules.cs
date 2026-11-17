using System;
using AlienFarms.Core;

namespace AlienFarms.Networking.Persistence
{
    /// <summary>Whose public stats the online room shows as the rival's. No allocation.</summary>
    public static class RivalRules
    {
        /// <summary>
        /// The first participant that is neither local nor a spectator (with a UGS id). False when the local participant is
        /// a spectator (no rival) or nobody qualifies.
        /// </summary>
        public static bool TryFindRival(ReadOnlySpan<OnlineParticipant> participants, out string ugsPlayerId)
        {
            ugsPlayerId = null;
            for (int i = 0; i < participants.Length; i++)
            {
                if (participants[i].IsLocal && participants[i].IsSpectator)
                {
                    return false;
                }
            }

            for (int i = 0; i < participants.Length; i++)
            {
                OnlineParticipant participant = participants[i];
                if (!participant.IsLocal && !participant.IsSpectator && !string.IsNullOrEmpty(participant.UgsPlayerId))
                {
                    ugsPlayerId = participant.UgsPlayerId;
                    return true;
                }
            }

            return false;
        }
    }
}
