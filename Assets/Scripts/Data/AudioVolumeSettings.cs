using AlienFarms.Audio;
using UnityEngine;

namespace AlienFarms.Data
{
    /// <summary>Default volume levels used until the player changes them (saved choices live in PlayerPrefs).</summary>
    [CreateAssetMenu(fileName = "AudioVolumeSettings", menuName = "Alien Farms/Audio/Volume Settings")]
    public sealed class AudioVolumeSettings : ScriptableObject
    {
        [SerializeField, Range(0f, 1f), Tooltip("Default master level (linear).")]
        private float _defaultMaster = 1f;

        [SerializeField, Range(0f, 1f), Tooltip("Default music level (linear).")]
        private float _defaultMusic = 0.7f;

        [SerializeField, Range(0f, 1f), Tooltip("Default sound effects level (linear).")]
        private float _defaultSfx = 1f;

        [SerializeField, Tooltip("Start muted when no preference is saved.")]
        private bool _defaultMuted;

        public bool DefaultMuted => _defaultMuted;

        /// <summary>Default linear level (0..1) of <paramref name="channel"/>.</summary>
        public float GetDefaultLevel(VolumeChannel channel)
        {
            switch (channel)
            {
                case VolumeChannel.Master:
                    return _defaultMaster;
                case VolumeChannel.Music:
                    return _defaultMusic;
                case VolumeChannel.Sfx:
                    return _defaultSfx;
                default:
                    return 1f;
            }
        }
    }
}
