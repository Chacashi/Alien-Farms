namespace AlienFarms.Utilities
{
    /// <summary>
    /// <see cref="IRandomSource"/> backed by its own <see cref="System.Random"/>: the same seed always yields the same
    /// sequence, independently of anything else using UnityEngine.Random.
    /// </summary>
    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly System.Random _random;

        public SystemRandomSource(int seed)
        {
            _random = new System.Random(seed);
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            return maxExclusive <= minInclusive ? minInclusive : _random.Next(minInclusive, maxExclusive);
        }

        public float Value01()
        {
            return (float)_random.NextDouble();
        }
    }
}
