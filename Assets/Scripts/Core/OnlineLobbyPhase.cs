namespace AlienFarms.Core
{
    /// <summary>Phase of the online room (character pick before the match) as published by the network lobby.</summary>
    public enum OnlineLobbyPhase
    {
        /// <summary>No lobby object exists (no session yet, still synchronizing, or shut down).</summary>
        Unavailable = 0,

        /// <summary>Players can pick and release characters.</summary>
        Open = 1,

        /// <summary>The host started the match; the gameplay scene is loading.</summary>
        Starting = 2
    }
}
