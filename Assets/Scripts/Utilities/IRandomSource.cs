namespace AlienFarms.Utilities
{
    /// <summary>
    /// Injectable random number source, so pure logic can be seeded and tested deterministically
    /// (never touches the global UnityEngine.Random state).
    /// </summary>
    public interface IRandomSource
    {
        /// <summary>Integer in [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>); returns min when the range is empty.</summary>
        int Range(int minInclusive, int maxExclusive);

        /// <summary>Float in [0, 1).</summary>
        float Value01();
    }
}
