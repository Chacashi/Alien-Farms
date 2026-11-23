using AlienFarms.Core;
using AlienFarms.Data;

namespace AlienFarms.Audio
{
    public static class OnlineSessionCueRules
    {
        public static UiSfxCue OnStatusChanged(OnlineSessionStatus prev, OnlineSessionStatus curr,
            bool hasError, bool joiningIsRejoin, bool roomWasOpen)
        {
            if (prev == OnlineSessionStatus.Ready && curr == OnlineSessionStatus.Creating)
            {
                return UiSfxCue.CreateRoom;
            }

            if (prev == OnlineSessionStatus.Ready && curr == OnlineSessionStatus.Joining && !joiningIsRejoin)
            {
                return UiSfxCue.JoinRoom;
            }

            return UiSfxCue.None;
        }
    }
}
