using AlienFarms.Data;
using UnityEngine;
using UnityEngine.Audio;

namespace AlienFarms.Audio
{
    /// <summary>
    /// Per-scene volume service: loads the saved levels (PlayerPrefs) and drives the exposed volume parameters of the
    /// AudioMixer. Not a singleton: each scene has one and options views reference it in the Inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioVolumeService : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("AlienFarmsMixer with MasterVolume, MusicVolume and SfxVolume exposed.")]
        private AudioMixer _mixer;

        [SerializeField, Tooltip("Defaults used while no preference is saved.")]
        private AudioVolumeSettings _settings;

        private VolumeState _state;

        public bool IsMuted => State.IsMuted;

        // Lazy so views enabled before this Awake (undefined order between objects) still read saved levels.
        private VolumeState State => _state ??= new VolumeState(new PlayerPrefsVolumeStore(), _settings);

        private void Awake()
        {
            _ = State;
        }

        private void Start()
        {
            // AudioMixer.SetFloat is ignored in Awake (the mixer is not ready yet): apply from Start.
            ApplyAll();
        }

        private void OnApplicationQuit()
        {
            Save();
        }

        /// <summary>Linear level (0..1) of <paramref name="channel"/>.</summary>
        public float GetLevel(VolumeChannel channel) => State.GetLevel(channel);

        /// <summary>Sets and applies a linear level (clamped to 0..1). Persisted in memory; call <see cref="Save"/> to flush.</summary>
        public void SetLevel(VolumeChannel channel, float level)
        {
            if (State.SetLevel(channel, level))
            {
                Apply(channel);
            }
        }

        /// <summary>Mutes or unmutes everything (through Master).</summary>
        public void SetMuted(bool muted)
        {
            if (State.SetMuted(muted))
            {
                Apply(VolumeChannel.Master);
            }
        }

        /// <summary>Writes the preferences to disk.</summary>
        public void Save()
        {
            _state?.Save();
        }

        private void ApplyAll()
        {
            for (int i = 0; i < AudioMixerParameters.ChannelCount; i++)
            {
                Apply((VolumeChannel)i);
            }
        }

        private void Apply(VolumeChannel channel)
        {
            if (_mixer == null)
            {
                return;
            }

            string parameter = AudioMixerParameters.GetVolumeParameter(channel);
            if (!_mixer.SetFloat(parameter, State.GetAppliedDecibels(channel)))
            {
                Debug.LogWarning($"[{nameof(AudioVolumeService)}] Mixer '{_mixer.name}' has no exposed parameter '{parameter}'.", this);
            }
        }

        private void OnValidate()
        {
            if (_mixer == null || _settings == null)
            {
                Debug.LogWarning($"[{nameof(AudioVolumeService)}] Missing AudioMixer or AudioVolumeSettings on '{name}'.", this);
            }
        }
    }
}
