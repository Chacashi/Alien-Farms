using UnityEngine;

namespace AlienFarms.Audio
{
    /// <summary>Conversions between linear slider volume (0..1) and AudioMixer attenuation (dB).</summary>
    public static class VolumeMath
    {
        /// <summary>Attenuation used for silence (the AudioMixer floor).</summary>
        public const float MinDecibels = -80f;

        /// <summary>Linear levels at or below this are treated as silence.</summary>
        public const float SilenceThreshold = 0.0001f;

        /// <summary>Linear 0..1 (clamped) to dB: 1 = 0 dB, 0.5 ~ -6 dB, 0 = <see cref="MinDecibels"/>.</summary>
        public static float LinearToDecibels(float linear)
        {
            float clamped = Mathf.Clamp01(linear);
            if (clamped <= SilenceThreshold)
            {
                return MinDecibels;
            }

            return Mathf.Max(MinDecibels, Mathf.Log10(clamped) * 20f);
        }

        /// <summary>dB to linear 0..1: <see cref="MinDecibels"/> or lower = 0, 0 dB or higher = 1.</summary>
        public static float DecibelsToLinear(float decibels)
        {
            if (decibels <= MinDecibels)
            {
                return 0f;
            }

            return Mathf.Clamp01(Mathf.Pow(10f, decibels / 20f));
        }
    }
}
