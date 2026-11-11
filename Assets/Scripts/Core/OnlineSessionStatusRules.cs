namespace AlienFarms.Core
{
    /// <summary>
    /// Allowed transitions of <see cref="OnlineSessionStatus"/>. Pure, so the session channel and its tests share them.
    /// Any status may fall back to <see cref="OnlineSessionStatus.SignedOut"/> (services reset or root torn down).
    /// </summary>
    public static class OnlineSessionStatusRules
    {
        public static bool CanTransition(OnlineSessionStatus from, OnlineSessionStatus to)
        {
            if (from == to)
            {
                return false;
            }

            if (to == OnlineSessionStatus.SignedOut)
            {
                return true;
            }

            switch (from)
            {
                case OnlineSessionStatus.SignedOut:
                    return to == OnlineSessionStatus.SigningIn;
                case OnlineSessionStatus.SigningIn:
                    return to == OnlineSessionStatus.Ready;
                case OnlineSessionStatus.Ready:
                    return to == OnlineSessionStatus.Creating || to == OnlineSessionStatus.Joining;
                case OnlineSessionStatus.Creating:
                case OnlineSessionStatus.Joining:
                    // Leaving: a Leave arrived while starting; it holds until the pending start ends and is cleaned up.
                    return to == OnlineSessionStatus.InSession || to == OnlineSessionStatus.Ready || to == OnlineSessionStatus.Leaving;
                case OnlineSessionStatus.InSession:
                    // Ready directly: removed from the session (host closed it, kicked) without a local Leave.
                    return to == OnlineSessionStatus.Leaving || to == OnlineSessionStatus.Ready;
                case OnlineSessionStatus.Leaving:
                    return to == OnlineSessionStatus.Ready;
                default:
                    return false;
            }
        }

        /// <summary>Create and Join are accepted only when signed in and in no session.</summary>
        public static bool CanStartSession(OnlineSessionStatus status)
        {
            return status == OnlineSessionStatus.Ready;
        }

        /// <summary>Leave is accepted while starting or in a session.</summary>
        public static bool CanLeave(OnlineSessionStatus status)
        {
            return status == OnlineSessionStatus.Creating
                || status == OnlineSessionStatus.Joining
                || status == OnlineSessionStatus.InSession;
        }

        /// <summary>Sign-in is accepted only when signed out.</summary>
        public static bool CanSignIn(OnlineSessionStatus status)
        {
            return status == OnlineSessionStatus.SignedOut;
        }
    }
}
