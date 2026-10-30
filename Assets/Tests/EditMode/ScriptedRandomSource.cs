using System.Collections.Generic;
using AlienFarms.Utilities;

namespace AlienFarms.Tests
{
    /// <summary>
    /// Test fake: <see cref="Range"/> returns the queued values in order (clamped into the requested range) and the
    /// range minimum once the queue is exhausted; <see cref="Value01"/> returns the queued floats in order (see
    /// <see cref="WithFloats"/>) and 0 once that queue is exhausted.
    /// </summary>
    public sealed class ScriptedRandomSource : IRandomSource
    {
        private readonly Queue<int> _values;
        private readonly Queue<float> _floats = new Queue<float>();

        public ScriptedRandomSource(params int[] values)
        {
            _values = new Queue<int>(values);
        }

        /// <summary>Fake whose <see cref="Value01"/> returns <paramref name="values"/> in order (no int values queued).</summary>
        public static ScriptedRandomSource WithFloats(params float[] values)
        {
            var source = new ScriptedRandomSource();
            for (int i = 0; i < values.Length; i++)
            {
                source._floats.Enqueue(values[i]);
            }

            return source;
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

        public float Value01() => _floats.Count > 0 ? _floats.Dequeue() : 0f;
    }
}
