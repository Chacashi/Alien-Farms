using AlienFarms.Core;

namespace AlienFarms.Match
{
    /// <summary>Per-player capture count for the current round. Pure logic, no Unity dependencies.</summary>
    public sealed class MatchScore
    {
        // Indexed by (int)PlayerId; slot 0 (None) stays unused.
        private readonly int[] _scores = new int[3];

        public int Get(PlayerId id)
        {
            int index = (int)id;
            return IsPlayer(id) ? _scores[index] : 0;
        }

        /// <summary>Adds <paramref name="amount"/> to the player's score and returns the new total. None is ignored.</summary>
        public int Add(PlayerId id, int amount = 1)
        {
            if (!IsPlayer(id))
            {
                return 0;
            }

            int index = (int)id;
            _scores[index] += amount;
            return _scores[index];
        }

        public void Reset()
        {
            for (int i = 0; i < _scores.Length; i++)
            {
                _scores[i] = 0;
            }
        }

        /// <summary>Highest score wins; any tie (including 0-0) is a draw.</summary>
        public MatchResult Evaluate()
        {
            int p1 = Get(PlayerId.Player1);
            int p2 = Get(PlayerId.Player2);
            PlayerId winner = p1 > p2 ? PlayerId.Player1 : p2 > p1 ? PlayerId.Player2 : PlayerId.None;
            return new MatchResult(winner, p1, p2);
        }

        private static bool IsPlayer(PlayerId id) => id == PlayerId.Player1 || id == PlayerId.Player2;
    }
}
