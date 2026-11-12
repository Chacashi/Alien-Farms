namespace AlienFarms.Match
{
    /// <summary>What the match does when a suspension ends (see <see cref="MatchSuspendRules.OnResume"/>).</summary>
    public enum MatchResumeAction
    {
        /// <summary>Nothing: the phase keeps its own state (Results), or a missing player makes it abort on the next tick.</summary>
        None = 0,

        /// <summary>Playing: give the controls back to the players.</summary>
        EnableControls = 1,

        /// <summary>Countdown: start it over (the round is reset with respawn).</summary>
        RestartCountdown = 2
    }
}
