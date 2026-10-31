using AlienFarms.Core;

namespace AlienFarms.Match
{
    /// <summary>Pure rules for when the game may be paused.</summary>
    public static class PauseRules
    {
        /// <summary>Pausing is allowed while waiting, counting down or playing; never on results or before start.</summary>
        public static bool CanPause(MatchPhase phase)
        {
            switch (phase)
            {
                case MatchPhase.WaitingForPlayers:
                case MatchPhase.Countdown:
                case MatchPhase.Playing:
                    return true;
                default:
                    return false;
            }
        }
    }
}
