using System;
using AlienFarms.Core;
using AlienFarms.Data;

namespace AlienFarms.Audio
{
    public static class OnlineLobbyCueRules
    {
        public static int Diff(in OnlineLobbySnapshot prev, in OnlineLobbySnapshot curr,
            bool includeRemotePicks, Span<UiSfxCue> output)
        {
            if (!prev.IsInRoom || !curr.IsInRoom)
            {
                return 0;
            }

            int count = 0;
            for (int i = (int)PlayerId.Player1; i <= (int)PlayerId.Player2 && count < output.Length; i++)
            {
                var id = (PlayerId)i;
                bool hadSeat = prev.TryGetSeat(id, out OnlineLobbySeat oldSeat);
                bool hasSeat = curr.TryGetSeat(id, out OnlineLobbySeat newSeat);
                if (!hadSeat && hasSeat && !newSeat.IsLocal)
                {
                    output[count++] = UiSfxCue.PlayerJoined;
                }
                else if (hadSeat && !hasSeat && !oldSeat.IsLocal)
                {
                    output[count++] = UiSfxCue.ConnectionLost;
                }
                else if (hadSeat && hasSeat && (newSeat.IsLocal || includeRemotePicks)
                    && oldSeat.HasCharacter != newSeat.HasCharacter)
                {
                    output[count++] = newSeat.HasCharacter ? UiSfxCue.CharacterConfirm : UiSfxCue.CharacterRelease;
                }
            }

            return count;
        }
    }
}
