using UnityEngine;

namespace AlienFarms.Core
{
    public static class VoiceVolumeRules
    {
        public const int MinVolume = -50;
        public const int MaxVolume = 50;

        public static int ToVivox(float slider01) =>
            Mathf.RoundToInt(Mathf.Lerp(MinVolume, MaxVolume, Mathf.Clamp01(slider01)));

        public static float ToSlider(int volume) => Mathf.InverseLerp(MinVolume, MaxVolume, Clamp(volume));

        public static int Clamp(int volume) => Mathf.Clamp(volume, MinVolume, MaxVolume);
    }
}
