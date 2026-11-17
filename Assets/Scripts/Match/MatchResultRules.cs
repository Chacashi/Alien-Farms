using AlienFarms.Core;

namespace AlienFarms.Match
{
    /// <summary>
    /// How the published <see cref="MatchResult"/> is built from the scored one. Shared by the match (server / local)
    /// and the online replica, so both raise the same result.
    /// </summary>
    public static class MatchResultRules
    {
        /// <summary>
        /// <paramref name="scored"/> as is when <paramref name="forfeitWinner"/> is <see cref="PlayerId.None"/>; otherwise a
        /// <see cref="MatchEndReason.Forfeit"/> won by <paramref name="forfeitWinner"/> with the same scores.
        /// </summary>
        public static MatchResult Resolve(in MatchResult scored, PlayerId forfeitWinner)
        {
            if (forfeitWinner != PlayerId.Player1 && forfeitWinner != PlayerId.Player2)
            {
                return scored;
            }

            return new MatchResult(forfeitWinner, scored.Player1Score, scored.Player2Score, MatchEndReason.Forfeit);
        }

        /// <summary>
        /// The only player still present when exactly one of both is (the forfeit winner); false when both or neither are.
        /// </summary>
        public static bool TryGetSoleRemaining(bool player1Present, bool player2Present, out PlayerId remaining)
        {
            if (player1Present == player2Present)
            {
                remaining = PlayerId.None;
                return false;
            }

            remaining = player1Present ? PlayerId.Player1 : PlayerId.Player2;
            return true;
        }
    }
}
