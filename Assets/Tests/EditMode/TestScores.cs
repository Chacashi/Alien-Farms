using AlienFarms.Core;
using AlienFarms.Match;

namespace AlienFarms.Tests
{
    /// <summary>Test helpers for <see cref="MatchScore"/>.</summary>
    public static class TestScores
    {
        /// <summary>Puts <paramref name="count"/> distinct stand-in animals in <paramref name="zoneOwner"/>'s zone.</summary>
        public static void AddAnimals(MatchScore score, PlayerId zoneOwner, int count)
        {
            for (int i = 0; i < count; i++)
            {
                score.TryAdd(zoneOwner, new object(), out _);
            }
        }
    }
}
