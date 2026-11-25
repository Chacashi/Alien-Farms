using System;
using System.Globalization;
using AlienFarms.Core;
using AlienFarms.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace AlienFarms.UI
{
    [DisallowMultipleComponent]
    public sealed class VoiceHudView : MonoBehaviour
    {
        private const double DistanceInterval = 0.25d;

        [SerializeField] private VoiceChatChannel _voice;
        [SerializeField, Tooltip("Child containing the HUD. Keep this view outside it.")]
        private GameObject _content;
        [SerializeField] private GameObject _micMuted;
        [SerializeField] private GameObject _micSpeaking;
        [SerializeField] private GameObject _rivalSpeaking;
        [SerializeField] private GameObject _rivalMuted;
        [SerializeField] private TMP_Text _rivalSeat;
        [SerializeField] private TMP_Text _rivalState;
        [SerializeField] private TMP_Text _distance;
        [SerializeField] private TMP_Text _error;
        [SerializeField] private LocalizedString _noVoice = new LocalizedString(LocalizationKeys.UiTable, "voice.state.no_voice");
        [SerializeField] private LocalizedString _inRange = new LocalizedString(LocalizationKeys.UiTable, "voice.state.in_range");
        [SerializeField] private LocalizedString _outOfRange = new LocalizedString(LocalizationKeys.UiTable, "voice.state.out_of_range");

        private readonly char[] _buffer = new char[16];
        private readonly LocalizedString _errorMessage = new LocalizedString();
        private LocalizedStringBinding _noVoiceText;
        private LocalizedStringBinding _inRangeText;
        private LocalizedStringBinding _outOfRangeText;
        private string _errorKey;
        private double _nextDistanceRefresh;
        private int _shownTenths = int.MinValue;
        private char _decimalSeparator = '.';

        private void Awake()
        {
            _noVoiceText = new LocalizedStringBinding(_noVoice, RefreshIndicators);
            _inRangeText = new LocalizedStringBinding(_inRange, RefreshIndicators);
            _outOfRangeText = new LocalizedStringBinding(_outOfRange, RefreshIndicators);
        }

        private void OnValidate()
        {
            if (_voice == null || _content == null)
            {
                Debug.LogWarning($"[{nameof(VoiceHudView)}] One or more required references are not assigned on '{name}'.", this);
            }
        }

        private void OnEnable()
        {
            if (_voice == null || _content == null)
            {
                return;
            }

            _voice.Changed += RefreshIndicators;
            GameEvents.OnVoiceParticipantChanged += HandleVoiceEvent;
            GameEvents.OnVoiceSpeakingChanged += HandleVoiceEvent;
            GameEvents.OnVoiceMuteChanged += HandleVoiceEvent;
            LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
            _noVoiceText.Enable();
            _inRangeText.Enable();
            _outOfRangeText.Enable();
            if (_error != null)
            {
                _error.text = string.Empty;
            }

            _nextDistanceRefresh = 0d;
            _shownTenths = int.MinValue;
            HandleLocaleChanged(LocalizationSettings.SelectedLocale);
            RefreshIndicators();
        }

        private void OnDisable()
        {
            if (_voice != null)
            {
                _voice.Changed -= RefreshIndicators;
            }

            GameEvents.OnVoiceParticipantChanged -= HandleVoiceEvent;
            GameEvents.OnVoiceSpeakingChanged -= HandleVoiceEvent;
            GameEvents.OnVoiceMuteChanged -= HandleVoiceEvent;
            LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
            _noVoiceText.Disable();
            _inRangeText.Disable();
            _outOfRangeText.Disable();
            _errorMessage.StringChanged -= HandleErrorText;
            _errorKey = null;
        }

        private void HandleVoiceEvent(PlayerId seat, bool value) => RefreshIndicators();

        private void RefreshIndicators()
        {
            bool available = _voice.IsAvailable;
            SetVisible(_content, available);
            if (!available)
            {
                return;
            }

            SetVisible(_micMuted, _voice.MicMuted);
            SetVisible(_micSpeaking, _voice.LocalSpeaking);
            SetVisible(_rivalSpeaking, _voice.RivalSpeaking);
            SetVisible(_rivalMuted, _voice.RivalLocallyMuted);
            if (_rivalSeat != null)
            {
                _rivalSeat.text = _voice.RivalSeat switch
                {
                    PlayerId.Player1 => "P1",
                    PlayerId.Player2 => "P2",
                    _ => string.Empty
                };
            }

            if (_rivalState != null)
            {
                _rivalState.text = _voice.RivalState switch
                {
                    VoiceRivalState.InRange => _inRangeText.Value ?? "In range",
                    VoiceRivalState.OutOfRange => _outOfRangeText.Value ?? "Out of range",
                    _ => _noVoiceText.Value ?? "No voice"
                };
            }

            RefreshError();
        }

        private void RefreshError()
        {
            if (_error == null || _errorKey == _voice.LastErrorKey)
            {
                return;
            }

            _errorMessage.StringChanged -= HandleErrorText;
            _errorKey = _voice.LastErrorKey;
            _error.text = string.Empty;
            if (_errorKey != null)
            {
                _errorMessage.SetReference(LocalizationKeys.UiTable, _errorKey);
                _errorMessage.StringChanged += HandleErrorText;
            }
        }

        private void HandleErrorText(string text) => _error.text = text;

        private void HandleLocaleChanged(Locale locale)
        {
            CultureInfo culture = locale?.Identifier.CultureInfo ?? CultureInfo.CurrentCulture;
            _decimalSeparator = culture.NumberFormat.NumberDecimalSeparator[0];
            _shownTenths = int.MinValue;
        }

        private void Update()
        {
            double now = Time.unscaledTimeAsDouble;
            if (_distance == null || _voice == null || !_voice.IsAvailable || now < _nextDistanceRefresh)
            {
                return;
            }

            _nextDistanceRefresh = now + DistanceInterval;
            float distance = _voice.RivalDistance;
            int tenths = distance < 0f ? -1 : Mathf.RoundToInt(distance * 10f);
            if (_shownTenths == tenths)
            {
                return;
            }

            _shownTenths = tenths;
            if (tenths < 0)
            {
                _distance.SetCharArray(_buffer, 0, 0);
                return;
            }

            (tenths / 10).TryFormat(_buffer.AsSpan(), out int length, provider: CultureInfo.InvariantCulture);
            _buffer[length++] = _decimalSeparator;
            _buffer[length++] = (char)('0' + tenths % 10);
            _buffer[length++] = ' ';
            _buffer[length++] = 'm';
            _distance.SetCharArray(_buffer, 0, length);
        }

        private static void SetVisible(GameObject target, bool visible)
        {
            if (target != null && target.activeSelf != visible)
            {
                target.SetActive(visible);
            }
        }
    }
}
