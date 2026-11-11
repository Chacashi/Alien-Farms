using AlienFarms.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AlienFarms.UI
{
    /// <summary>
    /// Audio options panel: Master/Music/SFX sliders (linear 0..1), optional mute toggle, percentage labels and SFX
    /// preview. Reads the service when shown, writes on every change and saves when hidden. Lives on the panel root.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioOptionsView : MonoBehaviour
    {
        private const string PercentFormat = "{0}%";

        [Header("Service")]
        [SerializeField, Tooltip("Volume service of this scene.")]
        private AudioVolumeService _service;

        [Header("Sliders (min 0, max 1, whole numbers off)")]
        [SerializeField] private Slider _master;
        [SerializeField] private Slider _music;
        [SerializeField] private Slider _sfx;

        [Header("Optional")]
        [SerializeField, Tooltip("Mutes everything when on.")]
        private Toggle _mute;

        [SerializeField] private TMP_Text _masterValue;
        [SerializeField] private TMP_Text _musicValue;
        [SerializeField] private TMP_Text _sfxValue;

        [SerializeField, Tooltip("2D AudioSource routed to the SFX group; plays the preview clip when the SFX slider moves.")]
        private AudioSource _sfxPreview;

        [SerializeField] private AudioClip _sfxPreviewClip;

        private UnityAction<float> _masterChanged;
        private UnityAction<float> _musicChanged;
        private UnityAction<float> _sfxChanged;
        private UnityAction<bool> _muteChanged;

        private void Awake()
        {
            _masterChanged = HandleMasterChanged;
            _musicChanged = HandleMusicChanged;
            _sfxChanged = HandleSfxChanged;
            _muteChanged = HandleMuteChanged;

            if (_sfxPreview != null)
            {
                _sfxPreview.playOnAwake = false;
                _sfxPreview.loop = false;
                _sfxPreview.spatialBlend = 0f;
                // The pause menu freezes the listener: the preview must still be heard there.
                _sfxPreview.ignoreListenerPause = true;
            }
        }

        private void OnEnable()
        {
            Bind(_master, VolumeChannel.Master, _masterValue, _masterChanged);
            Bind(_music, VolumeChannel.Music, _musicValue, _musicChanged);
            Bind(_sfx, VolumeChannel.Sfx, _sfxValue, _sfxChanged);

            if (_mute != null)
            {
                _mute.SetIsOnWithoutNotify(_service != null && _service.IsMuted);
                _mute.onValueChanged.AddListener(_muteChanged);
            }
        }

        private void OnDisable()
        {
            Unbind(_master, _masterChanged);
            Unbind(_music, _musicChanged);
            Unbind(_sfx, _sfxChanged);

            if (_mute != null)
            {
                _mute.onValueChanged.RemoveListener(_muteChanged);
            }

            if (_service != null)
            {
                _service.Save();
            }
        }

        private void Bind(Slider slider, VolumeChannel channel, TMP_Text label, UnityAction<float> handler)
        {
            if (slider == null)
            {
                return;
            }

            float level = _service != null ? _service.GetLevel(channel) : slider.value;
            slider.SetValueWithoutNotify(level);
            SetLabel(label, level);
            slider.onValueChanged.AddListener(handler);
        }

        private static void Unbind(Slider slider, UnityAction<float> handler)
        {
            if (slider != null)
            {
                slider.onValueChanged.RemoveListener(handler);
            }
        }

        private void HandleMasterChanged(float value) => Change(VolumeChannel.Master, value, _masterValue);

        private void HandleMusicChanged(float value) => Change(VolumeChannel.Music, value, _musicValue);

        private void HandleSfxChanged(float value)
        {
            Change(VolumeChannel.Sfx, value, _sfxValue);
            PlayPreview();
        }

        private void HandleMuteChanged(bool muted)
        {
            if (_service != null)
            {
                _service.SetMuted(muted);
            }
        }

        private void Change(VolumeChannel channel, float value, TMP_Text label)
        {
            if (_service != null)
            {
                _service.SetLevel(channel, value);
            }

            SetLabel(label, value);
        }

        private void PlayPreview()
        {
            if (_sfxPreview == null || _sfxPreviewClip == null || _sfxPreview.isPlaying)
            {
                return;
            }

            _sfxPreview.clip = _sfxPreviewClip;
            _sfxPreview.Play();
        }

        private static void SetLabel(TMP_Text label, float level)
        {
            if (label != null)
            {
                // TMP formats the float without allocating.
                label.SetText(PercentFormat, Mathf.Round(Mathf.Clamp01(level) * 100f));
            }
        }

        private void OnValidate()
        {
            // The service is a scene object: a prefab asset can never reference it, so only check scene instances.
            if (_service == null && gameObject.scene.IsValid())
            {
                Debug.LogWarning($"[{nameof(AudioOptionsView)}] Missing AudioVolumeService on '{name}'.", this);
            }

            if (_master == null || _music == null || _sfx == null)
            {
                Debug.LogWarning($"[{nameof(AudioOptionsView)}] Missing Master, Music or SFX slider on '{name}'.", this);
            }

            WarnIfNotNormalized(_master);
            WarnIfNotNormalized(_music);
            WarnIfNotNormalized(_sfx);

            if (_sfxPreview != null && _sfxPreview.outputAudioMixerGroup == null)
            {
                Debug.LogWarning($"[{nameof(AudioOptionsView)}] SFX preview source on '{name}' has no output mixer group; route it to SFX.", this);
            }
        }

        private void WarnIfNotNormalized(Slider slider)
        {
            if (slider != null && (slider.minValue != 0f || slider.maxValue != 1f || slider.wholeNumbers))
            {
                Debug.LogWarning($"[{nameof(AudioOptionsView)}] Slider '{slider.name}' on '{name}' must be 0..1 without whole numbers.", this);
            }
        }
    }
}
