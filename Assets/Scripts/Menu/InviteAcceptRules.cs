using AlienFarms.Core;

namespace AlienFarms.Menu
{
    public static class InviteAcceptRules
    {
        public enum Action
        {
            Blocked,
            JoinNow,
            LeaveThenJoin,
            DeclineRejoinThenJoin
        }

        public static (Action Next, string ErrorKey) Decide(OnlineSessionStatus status, bool inMatch,
            bool hasRejoinOffer, bool expired)
        {
            if (expired)
            {
                return (Action.Blocked, OnlineSessionErrorKeys.InviteExpired);
            }

            if (inMatch)
            {
                return (Action.Blocked, OnlineSessionErrorKeys.Busy);
            }

            return status switch
            {
                // The menu must sign in and wait for Ready before dispatching the join from the title.
                OnlineSessionStatus.SignedOut => (Action.JoinNow, null),
                OnlineSessionStatus.Ready => (hasRejoinOffer ? Action.DeclineRejoinThenJoin : Action.JoinNow, null),
                OnlineSessionStatus.InSession => (Action.LeaveThenJoin, null),
                _ => (Action.Blocked, OnlineSessionErrorKeys.Busy)
            };
        }
    }
}
