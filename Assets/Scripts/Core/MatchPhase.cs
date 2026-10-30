namespace AlienFarms.Core
{
    /// <summary>Phases of a match, driven by the MatchManager state machine.</summary>
    public enum MatchPhase
    {
        /// <summary>No state entered yet (before the manager starts).</summary>
        None = 0,

        /// <summary>Fewer than two players joined: free roam, captures not scored.</summary>
        WaitingForPlayers = 1,

        /// <summary>Round reset done, players frozen while the countdown runs.</summary>
        Countdown = 2,

        /// <summary>Timer running and score changes published.</summary>
        Playing = 3,

        /// <summary>Round over: results shown, players frozen until Confirm.</summary>
        Results = 4
    }
}
