using AlienFarms.Core;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Which <see cref="GameMode"/> the <see cref="AlienFarms.Data.MatchSession"/> must hold for an online session status.
    /// Online from the moment a create / join / rejoin begins, before NGO starts: the host may approve the client and
    /// synchronize the gameplay scene (whose <see cref="GameModeRouter"/> reads the mode in Awake) before the start
    /// returns. Local again once the start fails, is cancelled or the session is left. Pure.
    /// </summary>
    public static class OnlineModeRules
    {
        public static GameMode ModeFor(OnlineSessionStatus status)
        {
            switch (status)
            {
                case OnlineSessionStatus.Creating:
                case OnlineSessionStatus.Joining:
                case OnlineSessionStatus.InSession:
                case OnlineSessionStatus.Leaving:
                    return GameMode.Online;
                default:
                    return GameMode.Local;
            }
        }
    }
}
