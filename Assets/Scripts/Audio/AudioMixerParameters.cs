using System;

namespace AlienFarms.Audio
{
    /// <summary>
    /// Names of the exposed AudioMixer parameters (volume of each group, in dB). The only place these strings live:
    /// they must match the exposed parameters of AlienFarmsMixer exactly.
    /// </summary>
    public static class AudioMixerParameters
    {
        public const string MasterVolume = "MasterVolume";
        public const string MusicVolume = "MusicVolume";
        public const string SfxVolume = "SfxVolume";

        /// <summary>Number of <see cref="VolumeChannel"/> values.</summary>
        public const int ChannelCount = 3;

        /// <summary>Exposed volume parameter of <paramref name="channel"/>.</summary>
        public static string GetVolumeParameter(VolumeChannel channel)
        {
            switch (channel)
            {
                case VolumeChannel.Master:
                    return MasterVolume;
                case VolumeChannel.Music:
                    return MusicVolume;
                case VolumeChannel.Sfx:
                    return SfxVolume;
                default:
                    throw new ArgumentOutOfRangeException(nameof(channel), channel, null);
            }
        }
    }
}
