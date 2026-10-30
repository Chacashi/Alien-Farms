using System.Collections.Generic;
using AlienFarms.Core;

namespace AlienFarms.Match
{
    /// <summary>
    /// Zone occupancy for the current round: the set of animals each player's zone holds. A player's score is the size
    /// of its set. Idempotent (duplicate events are absorbed) and an animal is in at most one zone.
    /// Pure logic, no Unity dependencies.
    /// </summary>
    public sealed class MatchScore
    {
        // Indexed by (int)PlayerId; slot 0 (None) stays unused. Preallocated: no allocations while playing.
        private readonly HashSet<object>[] _zones =
        {
            new HashSet<object>(),
            new HashSet<object>(),
            new HashSet<object>()
        };

        public int Get(PlayerId id) => IsPlayer(id) ? _zones[(int)id].Count : 0;

        /// <summary>
        /// Records <paramref name="animal"/> inside <paramref name="zoneOwner"/>'s zone. If the other zone held it, it
        /// is moved and <paramref name="movedFrom"/> names that zone. Returns false (no change) when it was already
        /// there, the owner is None or the animal is null.
        /// </summary>
        public bool TryAdd(PlayerId zoneOwner, object animal, out PlayerId movedFrom)
        {
            movedFrom = PlayerId.None;
            if (!IsPlayer(zoneOwner) || animal == null)
            {
                return false;
            }

            HashSet<object> target = _zones[(int)zoneOwner];
            if (target.Contains(animal))
            {
                return false;
            }

            PlayerId other = zoneOwner == PlayerId.Player1 ? PlayerId.Player2 : PlayerId.Player1;
            if (_zones[(int)other].Remove(animal))
            {
                movedFrom = other;
            }

            target.Add(animal);
            return true;
        }

        /// <summary>Removes <paramref name="animal"/> from <paramref name="zoneOwner"/>'s zone. False if it was not there.</summary>
        public bool TryRemove(PlayerId zoneOwner, object animal)
        {
            return IsPlayer(zoneOwner) && animal != null && _zones[(int)zoneOwner].Remove(animal);
        }

        public void Reset()
        {
            for (int i = 0; i < _zones.Length; i++)
            {
                _zones[i].Clear();
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
