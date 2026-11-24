using AlienFarms.Core;

namespace AlienFarms.Networking.Persistence
{
    /// <summary>Pure accumulation rules of the lifetime stats. Saturating, never negative, no allocation.</summary>
    public static class PlayerStatsRules
    {
        /// <summary>
        /// Increment for one round seen by <paramref name="local"/>: one match and exactly one of win / draw / loss
        /// (a forfeit counts for its winner like any win), plus the animals in the own pen and the tackles landed.
        /// Zero for no local seat (<see cref="PlayerId.None"/>).
        /// </summary>
        public static PlayerStatsSummary ForMatch(PlayerId local, in MatchResult result, int animals, int tackles)
        {
            if (local != PlayerId.Player1 && local != PlayerId.Player2)
            {
                return default;
            }

            if (result.IsDraw)
            {
                return Draw(animals, tackles);
            }

            return result.Winner == local ? Win(animals, tackles) : Loss(animals, tackles);
        }

        /// <summary>One match won (e.g. the host left during Playing).</summary>
        public static PlayerStatsSummary Win(int animals, int tackles)
        {
            return new PlayerStatsSummary(1, 1, 0, 0, NonNegative(animals), NonNegative(tackles));
        }

        /// <summary>One match drawn.</summary>
        public static PlayerStatsSummary Draw(int animals, int tackles)
        {
            return new PlayerStatsSummary(1, 0, 1, 0, NonNegative(animals), NonNegative(tackles));
        }

        /// <summary>One match lost (e.g. leaving on purpose during Playing).</summary>
        public static PlayerStatsSummary Loss(int animals, int tackles)
        {
            return new PlayerStatsSummary(1, 0, 0, 1, NonNegative(animals), NonNegative(tackles));
        }

        /// <summary>Field by field sum, saturated at <see cref="int.MaxValue"/> (negatives count as 0).</summary>
        public static PlayerStatsSummary Add(in PlayerStatsSummary a, in PlayerStatsSummary b)
        {
            return new PlayerStatsSummary(
                SaturatingAdd(a.MatchesPlayed, b.MatchesPlayed),
                SaturatingAdd(a.Wins, b.Wins),
                SaturatingAdd(a.Draws, b.Draws),
                SaturatingAdd(a.Losses, b.Losses),
                SaturatingAdd(a.AnimalsScored, b.AnimalsScored),
                SaturatingAdd(a.TacklesLanded, b.TacklesLanded));
        }

        /// <summary>Field by field <paramref name="a"/> minus <paramref name="b"/>, floored at 0.</summary>
        public static PlayerStatsSummary Subtract(in PlayerStatsSummary a, in PlayerStatsSummary b)
        {
            return new PlayerStatsSummary(
                FlooredSubtract(a.MatchesPlayed, b.MatchesPlayed),
                FlooredSubtract(a.Wins, b.Wins),
                FlooredSubtract(a.Draws, b.Draws),
                FlooredSubtract(a.Losses, b.Losses),
                FlooredSubtract(a.AnimalsScored, b.AnimalsScored),
                FlooredSubtract(a.TacklesLanded, b.TacklesLanded));
        }

        /// <summary>
        /// Negatives become 0 and matches are repaired up to wins + draws + losses (saturated) when lower.
        /// </summary>
        public static PlayerStatsSummary Sanitize(in PlayerStatsSummary stats)
        {
            int wins = NonNegative(stats.Wins);
            int draws = NonNegative(stats.Draws);
            int losses = NonNegative(stats.Losses);
            int decided = SaturatingAdd(SaturatingAdd(wins, draws), losses);
            int matches = NonNegative(stats.MatchesPlayed);
            if (matches < decided)
            {
                matches = decided;
            }

            return new PlayerStatsSummary(matches, wins, draws, losses, NonNegative(stats.AnimalsScored), NonNegative(stats.TacklesLanded));
        }

        /// <summary>True when every field is 0 or less (nothing to record).</summary>
        public static bool IsZero(in PlayerStatsSummary stats)
        {
            return stats.MatchesPlayed <= 0
                && stats.Wins <= 0
                && stats.Draws <= 0
                && stats.Losses <= 0
                && stats.AnimalsScored <= 0
                && stats.TacklesLanded <= 0;
        }

        private static int NonNegative(int value) => value < 0 ? 0 : value;

        private static int SaturatingAdd(int a, int b)
        {
            long sum = (long)NonNegative(a) + NonNegative(b);
            return sum > int.MaxValue ? int.MaxValue : (int)sum;
        }

        private static int FlooredSubtract(int a, int b)
        {
            long difference = (long)NonNegative(a) - NonNegative(b);
            return difference < 0 ? 0 : (int)difference;
        }
    }
}
