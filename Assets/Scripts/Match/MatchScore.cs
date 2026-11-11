using System.Collections.Generic;
using AlienFarms.Core;
using AlienFarms.Data;

namespace AlienFarms.Match
{
    /// <summary>
    /// Zone occupancy for the current round: the animals each player's zone holds, the points each one is worth and its
    /// type. A player's score is the sum of the points in its zone. Idempotent (duplicate events are absorbed) and an
    /// animal is in at most one zone; the points recorded on entry are the ones removed on exit.
    /// Pure logic: animals and types are opaque keys.
    /// </summary>
    public sealed class MatchScore
    {
        // Indexed by (int)PlayerId; slot 0 (None) stays unused. Preallocated: no allocations while playing.
        private readonly Dictionary<object, (int Points, object TypeKey)>[] _zones =
        {
            new Dictionary<object, (int Points, object TypeKey)>(),
            new Dictionary<object, (int Points, object TypeKey)>(),
            new Dictionary<object, (int Points, object TypeKey)>()
        };

        private readonly int[] _totals = new int[3];

        /// <summary>Sum of the points of the animals inside <paramref name="id"/>'s zone.</summary>
        public int Get(PlayerId id) => IsPlayer(id) ? _totals[(int)id] : 0;

        /// <summary>Number of animals inside <paramref name="id"/>'s zone, regardless of their points.</summary>
        public int GetAnimalCount(PlayerId id) => IsPlayer(id) ? _zones[(int)id].Count : 0;

        /// <summary>Number of animals of <paramref name="type"/> inside <paramref name="id"/>'s zone (0 for a null type).</summary>
        public int GetCountByType(PlayerId id, AnimalData type) => CountByTypeKey(id, type);

        /// <summary>Same as <see cref="TryAdd(PlayerId, object, int, object, out PlayerId)"/> without a type.</summary>
        public bool TryAdd(PlayerId zoneOwner, object animal, int points, out PlayerId movedFrom)
        {
            return TryAdd(zoneOwner, animal, points, null, out movedFrom);
        }

        /// <summary>
        /// Records <paramref name="animal"/>, of <paramref name="typeKey"/> (may be null) and worth <paramref name="points"/>
        /// (clamped to at least 1), inside <paramref name="zoneOwner"/>'s zone. If the other zone held it, it is moved and
        /// <paramref name="movedFrom"/> names that zone. Returns false (no change) when it was already there, the owner is
        /// None or the animal is null.
        /// </summary>
        public bool TryAdd(PlayerId zoneOwner, object animal, int points, object typeKey, out PlayerId movedFrom)
        {
            movedFrom = PlayerId.None;
            if (!IsPlayer(zoneOwner) || animal == null)
            {
                return false;
            }

            Dictionary<object, (int Points, object TypeKey)> target = _zones[(int)zoneOwner];
            if (target.ContainsKey(animal))
            {
                return false;
            }

            PlayerId other = zoneOwner == PlayerId.Player1 ? PlayerId.Player2 : PlayerId.Player1;
            if (RemoveFrom(other, animal))
            {
                movedFrom = other;
            }

            int value = points < 1 ? 1 : points;
            target.Add(animal, (value, typeKey));
            _totals[(int)zoneOwner] += value;
            return true;
        }

        /// <summary>Removes <paramref name="animal"/> from <paramref name="zoneOwner"/>'s zone. False if it was not there.</summary>
        public bool TryRemove(PlayerId zoneOwner, object animal)
        {
            return IsPlayer(zoneOwner) && animal != null && RemoveFrom(zoneOwner, animal);
        }

        public void Reset()
        {
            for (int i = 0; i < _zones.Length; i++)
            {
                _zones[i].Clear();
                _totals[i] = 0;
            }
        }

        /// <summary>Highest score (points) wins; any tie (including 0-0) is a draw.</summary>
        public MatchResult Evaluate()
        {
            int p1 = Get(PlayerId.Player1);
            int p2 = Get(PlayerId.Player2);
            PlayerId winner = p1 > p2 ? PlayerId.Player1 : p2 > p1 ? PlayerId.Player2 : PlayerId.None;
            return new MatchResult(winner, p1, p2);
        }

        private int CountByTypeKey(PlayerId id, object typeKey)
        {
            if (!IsPlayer(id) || typeKey == null)
            {
                return 0;
            }

            int count = 0;
            // Struct enumerator: no allocation.
            foreach ((int Points, object TypeKey) entry in _zones[(int)id].Values)
            {
                if (ReferenceEquals(entry.TypeKey, typeKey))
                {
                    count++;
                }
            }

            return count;
        }

        private bool RemoveFrom(PlayerId zoneOwner, object animal)
        {
            Dictionary<object, (int Points, object TypeKey)> zone = _zones[(int)zoneOwner];
            if (!zone.Remove(animal, out (int Points, object TypeKey) entry))
            {
                return false;
            }

            _totals[(int)zoneOwner] -= entry.Points;
            return true;
        }

        private static bool IsPlayer(PlayerId id) => id == PlayerId.Player1 || id == PlayerId.Player2;
    }
}
