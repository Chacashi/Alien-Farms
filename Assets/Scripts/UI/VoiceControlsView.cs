using AlienFarms.Core;
using AlienFarms.Data;
using UnityEngine;
using UnityEngine.UI;

namespace AlienFarms.UI
{
    [DisallowMultipleComponent]
    public sealed class VoiceControlsView : MonoBehaviour
    {
        [SerializeField] private VoiceChatChannel _voice;
        [SerializeField, Tooltip("Child containing the controls. Keep this view outside it.")]
        private GameObject _content;
        [SerializeField, Tooltip("On means the microphone is muted.")]
        private Toggle _micMuted;
        [SerializeField, Tooltip("Range 0 to 1, whole numbers off.")]
        private Slider _rivalVolume;
        [SerializeField] private Button _toggleRivalMute;
        [SerializeField, Tooltip("Optional indicator displayed when the rival is muted locally.")]
        private GameObject _rivalMutedIndicator;

        private void OnValidate()
        {
            if (_voice == null || _content == null || _micMuted == null ||
                _rivalVolume == null || _toggleRivalMute == null)
            {
                Debug.LogWarning($"[{nameof(VoiceControlsView)}] One or more required references are not assigned on '{name}'.", this);
            }
        }

        private void OnEnable()
        {
            if (_voice == null || _content == null || _micMuted == null ||
                _rivalVolume == null || _toggleRivalMute == null)
            {
                return;
            }

            _voice.Changed += Refresh;
            _micMuted.onValueChanged.AddListener(HandleMicMuted);
            _rivalVolume.onValueChanged.AddListener(HandleRivalVolume);
            _toggleRivalMute.onClick.AddListener(HandleRivalMute);
            Refresh();
        }

        private void OnDisable()
        {
            if (_voice != null)
            {
                _voice.Changed -= Refresh;
            }

            if (_micMuted != null)
            {
                _micMuted.onValueChanged.RemoveListener(HandleMicMuted);
            }

            if (_rivalVolume != null)
            {
                _rivalVolume.onValueChanged.RemoveListener(HandleRivalVolume);
            }

            if (_toggleRivalMute != null)
            {
                _toggleRivalMute.onClick.RemoveListener(HandleRivalMute);
            }
        }

        private void Refresh()
        {
            bool available = _voice.IsAvailable;
            if (_content.activeSelf != available)
            {
                _content.SetActive(available);
            }

            if (!available)
            {
                return;
            }

            _micMuted.SetIsOnWithoutNotify(_voice.MicMuted);
            _rivalVolume.SetValueWithoutNotify(VoiceVolumeRules.ToSlider(_voice.RivalVolume));
            if (_rivalMutedIndicator != null && _rivalMutedIndicator.activeSelf != _voice.RivalLocallyMuted)
            {
                _rivalMutedIndicator.SetActive(_voice.RivalLocallyMuted);
            }
        }

        private void HandleMicMuted(bool muted)
        {
            if (muted != _voice.MicMuted)
            {
                _voice.RequestToggleMic();
            }
        }

        private void HandleRivalVolume(float volume) => _voice.RequestSetRivalVolume(VoiceVolumeRules.ToVivox(volume));

        private void HandleRivalMute() => _voice.RequestToggleRivalMute();
    }
}
