namespace AlienFarms.Core
{
    /// <summary>
    /// Final outcome of a round. <see cref="Winner"/> is <see cref="PlayerId.None"/> on a draw. On a
    /// <see cref="MatchEndReason.Forfeit"/> the winner is the player who stayed, not the score leader.
    /// </summary>
    public readonly struct MatchResult
    {
        public MatchResult(PlayerId winner, int player1Score, int player2Score)
            : this(winner, player1Score, player2Score, MatchEndReason.TimeUp)
        {
        }

        public MatchResult(PlayerId winner, int player1Score, int player2Score, MatchEndReason endReason)
        {
            Winner = winner;
            Player1Score = player1Score;
            Player2Score = player2Score;
            EndReason = endReason;
        }

        public PlayerId Winner { get; }
        public int Player1Score { get; }
        public int Player2Score { get; }

        /// <summary>Why the round ended (<see cref="MatchEndReason.TimeUp"/> by default).</summary>
        public MatchEndReason EndReason { get; }

        public bool IsDraw => Winner == PlayerId.None;

        /// <summary>The rival abandoned the round; <see cref="Winner"/> is the player who stayed.</summary>
        public bool IsForfeit => EndReason == MatchEndReason.Forfeit;

        /// <summary>The player who abandoned on a forfeit, <see cref="PlayerId.None"/> otherwise.</summary>
        public PlayerId Forfeiter => IsForfeit ? Winner.Other() : PlayerId.None;

        public int GetScore(PlayerId id)
        {
            switch (id)
            {
                case PlayerId.Player1:
                    return Player1Score;
                case PlayerId.Player2:
                    return Player2Score;
                default:
                    return 0;
            }
        }
    }
}
