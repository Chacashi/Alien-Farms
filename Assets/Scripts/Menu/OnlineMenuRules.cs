using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.UI;

namespace AlienFarms.Menu
{
    /// <summary>
    /// Pure rules of the online menu: which screen the session and lobby state lead to, and which localization keys the
    /// busy label and the room hint show. Shared by <see cref="OnlineMenuController"/> and its tests.
    /// </summary>
    public static class OnlineMenuRules
    {
        /// <summary>
        /// Screen for the current state. Pending operations (and the match loading) are Busy, a session is the Room, and
        /// otherwise the Hub, or the Join screen when the player opened it.
        /// </summary>
        public static OnlineScreen Resolve(OnlineSessionStatus status, OnlineLobbyPhase lobbyPhase, bool wantsJoin)
        {
            switch (status)
            {
                case OnlineSessionStatus.SigningIn:
                case OnlineSessionStatus.Creating:
                case OnlineSessionStatus.Joining:
                case OnlineSessionStatus.Leaving:
                    return OnlineScreen.Busy;
                case OnlineSessionStatus.InSession:
                    return lobbyPhase == OnlineLobbyPhase.Starting ? OnlineScreen.Busy : OnlineScreen.Room;
                default:
                    return wantsJoin ? OnlineScreen.Join : OnlineScreen.Hub;
            }
        }

        /// <summary>Busy label key for the state (null when the state is not busy).</summary>
        public static string BusyKey(OnlineSessionStatus status, OnlineLobbyPhase lobbyPhase)
        {
            switch (status)
            {
                case OnlineSessionStatus.SigningIn:
                    return LocalizationKeys.OnlineBusySigningIn;
                case OnlineSessionStatus.Creating:
                    return LocalizationKeys.OnlineBusyCreating;
                case OnlineSessionStatus.Joining:
                    return LocalizationKeys.OnlineBusyJoining;
                case OnlineSessionStatus.Leaving:
                    return LocalizationKeys.OnlineBusyLeaving;
                case OnlineSessionStatus.InSession:
                    return lobbyPhase == OnlineLobbyPhase.Starting ? LocalizationKeys.OnlineBusyStarting : null;
                default:
                    return null;
            }
        }

        /// <summary>
        /// True when the busy screen offers Cancel: while signing in (back to the title) or creating / joining (leave).
        /// Leaving and loading the match cannot be cancelled.
        /// </summary>
        public static bool CanCancelBusy(OnlineSessionStatus status)
        {
            return status == OnlineSessionStatus.SigningIn
                || status == OnlineSessionStatus.Creating
                || status == OnlineSessionStatus.Joining;
        }

        /// <summary>
        /// Whether a MainMenu that just loaded opens on the online panel instead of the title: while still in a session
        /// (R1, room) or when an online match ended and asked to come back (hub, with the session error if any).
        /// </summary>
        public static bool OpensOnlineOnLoad(bool inOnlineSession, bool returnToOnlineRequested)
        {
            return inOnlineSession || returnToOnlineRequested;
        }

        /// <summary>
        /// Whether the join screen is still wanted after cancelling the busy <paramref name="status"/>: cancelling a join
        /// (or any other pending operation) goes back to the hub, never to the code screen.
        /// </summary>
        public static bool WantsJoinAfterCancelBusy(OnlineSessionStatus status, bool wantsJoin)
        {
            return wantsJoin && !CanCancelBusy(status);
        }

        /// <summary>
        /// The hub shows 'Reconnect (n s)' while this machine holds a rejoin offer that has not expired at
        /// <paramref name="now"/> (same clock as <paramref name="expiresAt"/>: realtime since startup).
        /// </summary>
        public static bool ShowsRejoin(bool hasOffer, double expiresAt, double now)
        {
            return hasOffer && now < expiresAt;
        }

        /// <summary>Whole seconds left on the rejoin offer, rounded up (0 once expired).</summary>
        public static int RejoinSecondsLeft(double expiresAt, double now)
        {
            double left = expiresAt - now;
            return left > 0d ? (int)System.Math.Ceiling(left) : 0;
        }

        /// <summary>
        /// Room hint key: syncing until the lobby appears, the spectator notice, then waiting for a player, for both picks
        /// or for the host. Null when nothing is missing for the host (Start is available).
        /// </summary>
        public static string HintKey(OnlineLobbyPhase lobbyPhase, int seatCount, bool allPicked, bool isHost, bool isSpectator)
        {
            if (lobbyPhase == OnlineLobbyPhase.Unavailable)
            {
                return LocalizationKeys.OnlineRoomSyncing;
            }

            if (isSpectator)
            {
                return LocalizationKeys.OnlineRoomSpectator;
            }

            if (seatCount < OnlineLobbyChannel.MaxSeats)
            {
                return LocalizationKeys.OnlineRoomWaitingPlayer;
            }

            if (!allPicked)
            {
                return LocalizationKeys.OnlineRoomNeedPicks;
            }

            return isHost ? null : LocalizationKeys.OnlineRoomWaitingHost;
        }
    }
}
