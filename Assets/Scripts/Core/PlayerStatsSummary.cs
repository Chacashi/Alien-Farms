using System;

namespace AlienFarms.Core
{
    /// <summary>Lifetime stats of the local player shown on the title screen.</summary>
    public readonly struct PlayerStatsSummary : IEquatable<PlayerStatsSummary>
    {
        public PlayerStatsSummary(int matchesPlayed, int wins, int draws, int losses, int animalsScored, int tacklesLanded)
        {
            MatchesPlayed = matchesPlayed;
            Wins = wins;
            Draws = draws;
            Losses = losses;
            AnimalsScored = animalsScored;
            TacklesLanded = tacklesLanded;
        }

        public int MatchesPlayed { get; }

        public int Wins { get; }

        public int Draws { get; }

        public int Losses { get; }

        public int AnimalsScored { get; }

        public int TacklesLanded { get; }

        public bool Equals(PlayerStatsSummary other)
        {
            return MatchesPlayed == other.MatchesPlayed
                && Wins == other.Wins
                && Draws == other.Draws
                && Losses == other.Losses
                && AnimalsScored == other.AnimalsScored
                && TacklesLanded == other.TacklesLanded;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerStatsSummary other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(MatchesPlayed, Wins, Draws, Losses, AnimalsScored, TacklesLanded);
        }
    }
}
