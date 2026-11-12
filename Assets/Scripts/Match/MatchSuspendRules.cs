using AlienFarms.Core;

namespace AlienFarms.Match
{
    /// <summary>
    /// Pure rules of a match suspension (online reconnection grace): which phases are suspended and what to do when the
    /// suspension ends.
    /// </summary>
    public static class MatchSuspendRules
    {
        /// <summary>True for the phases a dropped player suspends: Countdown, Playing and Results.</summary>
        public static bool SuspendsIn(MatchPhase phase)
        {
            return phase == MatchPhase.Countdown || phase == MatchPhase.Playing || phase == MatchPhase.Results;
        }

        /// <summary>
        /// Action to apply when the suspension ends in <paramref name="phase"/>. With a player still missing
        /// (<paramref name="playersComplete"/> false) nothing is done: the state aborts to WaitingForPlayers by itself.
        /// </summary>
        public static MatchResumeAction OnResume(MatchPhase phase, bool playersComplete)
        {
            if (!playersComplete)
            {
                return MatchResumeAction.None;
            }

            switch (phase)
            {
                case MatchPhase.Playing:
                    return MatchResumeAction.EnableControls;
                case MatchPhase.Countdown:
                    return MatchResumeAction.RestartCountdown;
                default:
                    return MatchResumeAction.None;
            }
        }
    }
}
