using System;
using AlienFarms.Data;
using UnityEngine;

namespace AlienFarms.Audio
{
    /// <summary>
    /// Plain C# volume model: linear level per <see cref="VolumeChannel"/> plus a master mute, loaded from and written
    /// through an <see cref="IVolumeStore"/>. Missing keys fall back to <see cref="AudioVolumeSettings"/> defaults.
    /// </summary>
    public sealed class VolumeState
    {
        public const string MasterKey = "AlienFarms.Volume.Master";
        public const string MusicKey = "AlienFarms.Volume.Music";
        public const string SfxKey = "AlienFarms.Volume.Sfx";
        public const string MutedKey = "AlienFarms.Volume.Muted";

        private readonly IVolumeStore _store;
        private readonly float[] _levels = new float[AudioMixerParameters.ChannelCount];
        private bool _isMuted;

        public VolumeState(IVolumeStore store, AudioVolumeSettings defaults)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            for (int i = 0; i < _levels.Length; i++)
            {
                VolumeChannel channel = (VolumeChannel)i;
                float fallback = defaults != null ? defaults.GetDefaultLevel(channel) : 1f;
                _levels[i] = Mathf.Clamp01(_store.GetFloat(GetKey(channel), fallback));
            }

            _isMuted = _store.GetBool(MutedKey, defaults != null && defaults.DefaultMuted);
        }

        public bool IsMuted => _isMuted;

        /// <summary>Linear level (0..1) of <paramref name="channel"/>.</summary>
        public float GetLevel(VolumeChannel channel) => _levels[ToIndex(channel)];

        /// <summary>Clamps to 0..1 and writes through to the store. Returns true if the level changed.</summary>
        public bool SetLevel(VolumeChannel channel, float level)
        {
            int index = ToIndex(channel);
            float clamped = Mathf.Clamp01(level);
            if (Mathf.Approximately(_levels[index], clamped))
            {
                return false;
            }

            _levels[index] = clamped;
            _store.SetFloat(GetKey(channel), clamped);
            return true;
        }

        /// <summary>Mutes everything (through Master). Returns true if the state changed.</summary>
        public bool SetMuted(bool muted)
        {
            if (_isMuted == muted)
            {
                return false;
            }

            _isMuted = muted;
            _store.SetBool(MutedKey, muted);
            return true;
        }

        /// <summary>Attenuation to send to the mixer for <paramref name="channel"/> (Master is silent while muted).</summary>
        public float GetAppliedDecibels(VolumeChannel channel)
        {
            if (channel == VolumeChannel.Master && _isMuted)
            {
                return VolumeMath.MinDecibels;
            }

            return VolumeMath.LinearToDecibels(GetLevel(channel));
        }

        /// <summary>Flushes the store to disk.</summary>
        public void Save() => _store.Save();

        /// <summary>Store key of <paramref name="channel"/>'s level.</summary>
        public static string GetKey(VolumeChannel channel)
        {
            switch (channel)
            {
                case VolumeChannel.Master:
                    return MasterKey;
                case VolumeChannel.Music:
                    return MusicKey;
                case VolumeChannel.Sfx:
                    return SfxKey;
                default:
                    throw new ArgumentOutOfRangeException(nameof(channel), channel, null);
            }
        }

        private static int ToIndex(VolumeChannel channel)
        {
            int index = (int)channel;
            if (index < 0 || index >= AudioMixerParameters.ChannelCount)
            {
                throw new ArgumentOutOfRangeException(nameof(channel), channel, null);
            }

            return index;
        }
    }
}
