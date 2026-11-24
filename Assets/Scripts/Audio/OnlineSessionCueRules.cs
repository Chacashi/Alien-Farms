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

            if (curr == OnlineSessionStatus.InSession
                && (prev == OnlineSessionStatus.Creating || prev == OnlineSessionStatus.Joining))
            {
                return prev == OnlineSessionStatus.Joining && joiningIsRejoin
                    ? UiSfxCue.PlayerReconnected
                    : UiSfxCue.ConnectionSucceeded;
            }

            if (prev == OnlineSessionStatus.InSession && curr == OnlineSessionStatus.Leaving && hasError && roomWasOpen)
            {
                return UiSfxCue.ConnectionLost;
            }

            return UiSfxCue.None;
        }

        public static bool IsArrivalNotice(OnlineSessionStatus status, string errorKey)
        {
            return status != OnlineSessionStatus.InSession
                && (errorKey == OnlineSessionErrorKeys.HostLeft || errorKey == OnlineSessionErrorKeys.ConnectionLost);
        }
    }
}
