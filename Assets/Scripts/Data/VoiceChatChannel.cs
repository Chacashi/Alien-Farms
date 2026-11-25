using System;
using System.Collections.Generic;
using AlienFarms.Core;
using UnityEngine;

namespace AlienFarms.Data
{
    [CreateAssetMenu(fileName = "VoiceChatChannel", menuName = "Alien Farms/Networking/Voice Chat Channel (Runtime)")]
    public sealed class VoiceChatChannel : ScriptableObject
    {
        private static int s_generation;

        [NonSerialized] private int _generation = -1;
        [NonSerialized] private VoiceStatus _status;
        [NonSerialized] private bool _hasDesiredChannel;
        [NonSerialized] private string _channelName;
        [NonSerialized] private bool _micMuted;
        [NonSerialized] private bool _localSpeaking;
        [NonSerialized] private PlayerId _rivalSeat;
        [NonSerialized] private VoiceRivalState _rivalState;
        [NonSerialized] private bool _rivalSpeaking;
        [NonSerialized] private int _rivalVolume;
        [NonSerialized] private bool _rivalLocallyMuted;
        [NonSerialized] private string _rivalPreferencesSessionId;
        [NonSerialized] private float _rivalDistance;
        [NonSerialized] private string _lastErrorKey;
        private Action _toggleMicRequested;
        private Action<int> _setRivalVolumeRequested;
        private Action _toggleRivalMuteRequested;

        public event Action Changed;

        public event Action ToggleMicRequested
        {
            add
            {
                EnsureCurrent();
                bool wasAvailable = IsAvailable;
                _toggleMicRequested += value;
                NotifyAvailabilityChanged(wasAvailable);
            }
            remove
            {
                bool wasAvailable = IsAvailable;
                _toggleMicRequested -= value;
                NotifyAvailabilityChanged(wasAvailable);
            }
        }

        public event Action<int> SetRivalVolumeRequested
        {
            add
            {
                EnsureCurrent();
                bool wasAvailable = IsAvailable;
                _setRivalVolumeRequested += value;
                NotifyAvailabilityChanged(wasAvailable);
            }
            remove
            {
                bool wasAvailable = IsAvailable;
                _setRivalVolumeRequested -= value;
                NotifyAvailabilityChanged(wasAvailable);
            }
        }

        public event Action ToggleRivalMuteRequested
        {
            add
            {
                EnsureCurrent();
                bool wasAvailable = IsAvailable;
                _toggleRivalMuteRequested += value;
                NotifyAvailabilityChanged(wasAvailable);
            }
            remove
            {
                bool wasAvailable = IsAvailable;
                _toggleRivalMuteRequested -= value;
                NotifyAvailabilityChanged(wasAvailable);
            }
        }

        public VoiceStatus Status => IsCurrent ? _status : VoiceStatus.Off;
        public string ChannelName => IsCurrent ? _channelName : null;
        public bool MicMuted => IsCurrent ? _micMuted : false;
        public bool LocalSpeaking => IsCurrent ? _localSpeaking : false;
        public PlayerId RivalSeat => IsCurrent ? _rivalSeat : PlayerId.None;
        public VoiceRivalState RivalState => IsCurrent ? _rivalState : VoiceRivalState.NoVoice;
        public bool RivalSpeaking => IsCurrent ? _rivalSpeaking : false;
        public int RivalVolume => IsCurrent ? _rivalVolume : 0;
        public bool RivalLocallyMuted => IsCurrent ? _rivalLocallyMuted : false;
        public float RivalDistance => IsCurrent ? _rivalDistance : -1f;
        public string LastErrorKey => IsCurrent ? _lastErrorKey : null;

        public bool IsAvailable => IsCurrent &&
            (_hasDesiredChannel || _status == VoiceStatus.Joining || _status == VoiceStatus.InChannel) &&
            (_toggleMicRequested != null || _setRivalVolumeRequested != null || _toggleRivalMuteRequested != null);

        private bool IsCurrent => _generation == s_generation;

        private void OnEnable()
        {
            hideFlags |= HideFlags.DontUnloadUnusedAsset;
        }

        public void RequestToggleMic()
        {
            if (IsAvailable)
            {
                _toggleMicRequested?.Invoke();
            }
        }

        public void RequestSetRivalVolume(int volume)
        {
            if (IsAvailable)
            {
                _setRivalVolumeRequested?.Invoke(VoiceVolumeRules.Clamp(volume));
            }
        }

        public void RequestToggleRivalMute()
        {
            if (IsAvailable)
            {
                _toggleRivalMuteRequested?.Invoke();
            }
        }

        public void SetStatus(VoiceStatus value) => Set(ref _status, value);
        public void SetHasDesiredChannel(bool value) => Set(ref _hasDesiredChannel, value);
        public void SetChannelName(string value) => Set(ref _channelName, value);
        public void SetMicMuted(bool value) => Set(ref _micMuted, value);
        public void SetLocalSpeaking(bool value) => Set(ref _localSpeaking, value);
        public void SetRivalSeat(PlayerId value) => Set(ref _rivalSeat, value);
        public void SetRivalState(VoiceRivalState value) => Set(ref _rivalState, value);
        public void SetRivalSpeaking(bool value) => Set(ref _rivalSpeaking, value);
        public void SetRivalVolume(int value) => Set(ref _rivalVolume, VoiceVolumeRules.Clamp(value));
        public void SetRivalLocallyMuted(bool value) => Set(ref _rivalLocallyMuted, value);
        public void SetRivalDistance(float value) => Set(ref _rivalDistance, value);
        public void SetLastErrorKey(string value) => Set(ref _lastErrorKey, value);

        public void SetRivalPreferencesSession(string sessionId)
        {
            EnsureCurrent();
            if (string.IsNullOrEmpty(sessionId) ||
                string.Equals(_rivalPreferencesSessionId, sessionId, StringComparison.Ordinal))
            {
                return;
            }

            _rivalPreferencesSessionId = sessionId;
            if (_rivalVolume != 0 || _rivalLocallyMuted)
            {
                _rivalVolume = 0;
                _rivalLocallyMuted = false;
                Changed?.Invoke();
            }
        }

        private void Set<T>(ref T field, T value)
        {
            EnsureCurrent();
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;
            Changed?.Invoke();
        }

        private void NotifyAvailabilityChanged(bool wasAvailable)
        {
            if (wasAvailable != IsAvailable)
            {
                Changed?.Invoke();
            }
        }

        private void EnsureCurrent()
        {
            if (IsCurrent)
            {
                return;
            }

            _generation = s_generation;
            _status = VoiceStatus.Off;
            _hasDesiredChannel = false;
            _channelName = null;
            _micMuted = false;
            _localSpeaking = false;
            _rivalSeat = PlayerId.None;
            _rivalState = VoiceRivalState.NoVoice;
            _rivalSpeaking = false;
            _rivalVolume = 0;
            _rivalLocallyMuted = false;
            _rivalPreferencesSessionId = null;
            _rivalDistance = -1f;
            _lastErrorKey = null;
            _toggleMicRequested = null;
            _setRivalVolumeRequested = null;
            _toggleRivalMuteRequested = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_generation++;
        }
    }
}
