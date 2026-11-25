using AlienFarms.Core;

namespace AlienFarms.Menu
{
    public static class InviteJoinSequence
    {
        public enum Step
        {
            Wait,
            Join,
            DeclineRejoin,
            Expire,
            Cancel
        }

        public static Step Decide(MenuPhase phase, OnlineSessionStatus status, bool hasRejoinOffer,
            bool inMatch, double expiresAt, double now)
        {
            if (phase != MenuPhase.Online || inMatch)
            {
                return Step.Cancel;
            }

            if (now >= expiresAt)
            {
                return Step.Expire;
            }

            return status switch
            {
                OnlineSessionStatus.SigningIn or OnlineSessionStatus.Leaving => Step.Wait,
                OnlineSessionStatus.Ready => hasRejoinOffer ? Step.DeclineRejoin : Step.Join,
                _ => Step.Cancel
            };
        }
    }
}
