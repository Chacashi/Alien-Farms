using UnityEngine;

namespace AlienFarms.Core
{
    /// <summary>Reference gain curves for positional voice measurements.</summary>
    public static class VoiceAttenuationModel
    {
        public static float Gain(float distance, float audible, float conversational, float fade, VoiceFadeModel model)
        {
            if (distance > audible)
            {
                return 0f;
            }

            float reference = Mathf.Clamp(conversational, 0f, audible);
            if (distance <= reference || fade <= 0f)
            {
                return 1f;
            }

            float offset = distance - reference;
            return model switch
            {
                VoiceFadeModel.LinearByDistance => Mathf.Clamp01(1f - fade * (offset / (audible - reference))),
                VoiceFadeModel.ExponentialByDistance => Mathf.Pow(reference / distance, fade),
                _ => reference / (reference + fade * offset)
            };
        }

        public static float ToDecibels(float gain) => gain > 0f ? 20f * Mathf.Log10(gain) : float.NegativeInfinity;
    }
}
