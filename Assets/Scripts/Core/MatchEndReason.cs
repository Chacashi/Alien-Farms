namespace AlienFarms.Core
{
    /// <summary>Why a round ended. Replicated as a byte, so values must stay stable.</summary>
    public enum MatchEndReason : byte
    {
        /// <summary>The round timer ran out; the winner comes from the score.</summary>
        TimeUp = 0,

        /// <summary>The rival left during Playing (online only); the player who stayed wins whatever the score.</summary>
        Forfeit = 1
    }
}
