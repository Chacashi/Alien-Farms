namespace AlienFarms.Core
{
    /// <summary>Final outcome of a round. <see cref="Winner"/> is <see cref="PlayerId.None"/> on a draw.</summary>
    public readonly struct MatchResult
    {
        public MatchResult(PlayerId winner, int player1Score, int player2Score)
        {
            Winner = winner;
            Player1Score = player1Score;
            Player2Score = player2Score;
        }

        public PlayerId Winner { get; }
        public int Player1Score { get; }
        public int Player2Score { get; }

        public bool IsDraw => Winner == PlayerId.None;

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
