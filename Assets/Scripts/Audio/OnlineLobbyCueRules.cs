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
                if (!prev.TryGetSeat(id, out OnlineLobbySeat oldSeat)
                    || !curr.TryGetSeat(id, out OnlineLobbySeat newSeat)
                    || (!newSeat.IsLocal && !includeRemotePicks)
                    || oldSeat.HasCharacter == newSeat.HasCharacter)
                {
                    continue;
                }

                output[count++] = newSeat.HasCharacter ? UiSfxCue.CharacterConfirm : UiSfxCue.CharacterRelease;
            }

            return count;
        }
    }
}
