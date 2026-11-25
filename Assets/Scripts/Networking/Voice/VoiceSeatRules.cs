using System;
using AlienFarms.Core;
using AlienFarms.Data;

namespace AlienFarms.Networking.Voice
{
    public static class VoiceSeatRules
    {
        public static PlayerId SeatOf(string ugsId, OnlineSessionChannel participants)
        {
            if (string.IsNullOrEmpty(ugsId) || participants == null)
            {
                return PlayerId.None;
            }

            int count = participants.ParticipantCount;
            for (int i = 0; i < count; i++)
            {
                OnlineParticipant participant = participants.GetParticipant(i);
                if (string.Equals(participant.UgsPlayerId, ugsId, StringComparison.Ordinal))
                {
                    return participant.IsHost ? PlayerId.Player1 : PlayerId.Player2;
                }
            }

            return PlayerId.None;
        }
    }
}
