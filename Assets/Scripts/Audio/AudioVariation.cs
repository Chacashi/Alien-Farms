using AlienFarms.Data;
using AlienFarms.Utilities;
using UnityEngine;

namespace AlienFarms.Audio
{
    /// <summary>Pure helpers that add variety to repeated sounds (random clip and pitch, voice selection).</summary>
    public static class AudioVariation
    {
        /// <summary>Lowest pitch accepted for a random pitch range.</summary>
        public const float MinPitch = SoundCue.MinPitch;

        /// <summary>Highest pitch accepted for a random pitch range.</summary>
        public const float MaxPitch = SoundCue.MaxPitch;

        /// <summary>
        /// Random non-null entry of <paramref name="items"/>, or null when it is null/empty or holds only nulls.
        /// Null slots (unassigned in the Inspector) are skipped by probing forward from the rolled index.
        /// </summary>
        public static T PickRandom<T>(T[] items, IRandomSource rng) where T : class
        {
            if (items == null || items.Length == 0)
            {
                return null;
            }

            int start = rng.Range(0, items.Length);
            for (int i = 0; i < items.Length; i++)
            {
                T item = items[(start + i) % items.Length];
                if (item != null)
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>Random pitch in [range.x, range.y], clamped to [<see cref="MinPitch"/>, <see cref="MaxPitch"/>].</summary>
        public static float PickPitch(Vector2 range, IRandomSource rng)
        {
            float min = Mathf.Clamp(range.x, MinPitch, MaxPitch);
            float max = Mathf.Clamp(range.y, min, MaxPitch);
            return Mathf.Lerp(min, max, rng.Value01());
        }

        /// <summary>
        /// Pitch of a sound that speeds up over time: <paramref name="basePitch"/> at 0 s, rising linearly to
        /// basePitch * <paramref name="multiplier"/> at <paramref name="rampTime"/> and holding there. Clamped to
        /// [<see cref="MinPitch"/>, <see cref="MaxPitch"/>].
        /// </summary>
        public static float RampPitch(float basePitch, float elapsed, float rampTime, float multiplier)
        {
            float t = rampTime > 0f ? Mathf.Clamp01(elapsed / rampTime) : 1f;
            float pitch = basePitch * Mathf.Lerp(1f, Mathf.Max(0f, multiplier), t);
            return Mathf.Clamp(pitch, MinPitch, MaxPitch);
        }

        /// <summary>
        /// Index of the voice to use: the first idle voice found scanning from <paramref name="next"/> (round-robin),
        /// or <paramref name="next"/> itself when all are busy (steals the oldest started voice). -1 when there are none.
        /// </summary>
        /// <param name="busy">Per-voice "currently playing" flags.</param>
        public static int SelectVoice(bool[] busy, int next)
        {
            if (busy == null || busy.Length == 0)
            {
                return -1;
            }

            int count = busy.Length;
            int start = ((next % count) + count) % count;
            for (int i = 0; i < count; i++)
            {
                int index = (start + i) % count;
                if (!busy[index])
                {
                    return index;
                }
            }

            return start;
        }
    }
}
