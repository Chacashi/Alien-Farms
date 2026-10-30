using System.Collections.Generic;
using AlienFarms.Utilities;

namespace AlienFarms.Tests
{
    /// <summary>
    /// Test fake: <see cref="Range"/> returns the queued values in order (clamped into the requested range) and the
    /// range minimum once the queue is exhausted; <see cref="Value01"/> returns 0.
    /// </summary>
    public sealed class ScriptedRandomSource : IRandomSource
    {
        private readonly Queue<int> _values;

        public ScriptedRandomSource(params int[] values)
        {
            _values = new Queue<int>(values);
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive || _values.Count == 0)
            {
                return minInclusive;
            }

            int value = _values.Dequeue();
            if (value < minInclusive)
            {
                return minInclusive;
            }

            return value >= maxExclusive ? maxExclusive - 1 : value;
        }

        public float Value01() => 0f;
    }
}
