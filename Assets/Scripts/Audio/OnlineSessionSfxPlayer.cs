using System;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Utilities;
using UnityEngine;

namespace AlienFarms.Audio
{
    [DisallowMultipleComponent]
    public sealed class OnlineSessionSfxPlayer : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Sound cues for the online session and room.")]
        private UiSfxLibrary _library;

        [SerializeField, Tooltip("Runtime channel that publishes the online session status.")]
        private OnlineSessionChannel _session;

        [SerializeField, Tooltip("Runtime channel that publishes the online room seats.")]
        private OnlineLobbyChannel _lobby;

        [SerializeField, Tooltip("Play character pick sounds for remote seats as well as the local seat.")]
        private bool _playRemotePicks = true;

        [Header("Voices")]
        [SerializeField, Tooltip("Pooled AudioSources routed to AlienFarmsMixer > SFX.")]
        private AudioSource[] _voices;

        private readonly UiSfxCue[] _buffer = new UiSfxCue[4];
        private SfxVoicePool _pool;
        private IRandomSource _rng;
        private OnlineSessionStatus _lastStatus;
        private OnlineLobbySnapshot _lastLobby;

        private void Awake()
        {
            _pool = new SfxVoicePool(_voices);
            _rng = new SystemRandomSource(Environment.TickCount);
        }

        private void OnEnable()
        {
            if (_session != null)
            {
                _lastStatus = _session.Status;
                _session.Changed += HandleSessionChanged;
            }

            if (_lobby != null)
            {
                _lastLobby = OnlineLobbySnapshot.Capture(_lobby);
                _lobby.Changed += HandleLobbyChanged;
            }
        }

        private void OnDisable()
        {
            if (_session != null)
            {
                _session.Changed -= HandleSessionChanged;
            }

            if (_lobby != null)
            {
                _lobby.Changed -= HandleLobbyChanged;
            }
        }

        private void HandleSessionChanged()
        {
            OnlineSessionStatus status = _session.Status;
            if (status == _lastStatus)
            {
                return;
            }

            bool joiningIsRejoin = status == OnlineSessionStatus.Joining && _session.HasRejoinOffer;
            UiSfxCue cue = OnlineSessionCueRules.OnStatusChanged(_lastStatus, status,
                hasError: !string.IsNullOrEmpty(_session.LastErrorKey), joiningIsRejoin: joiningIsRejoin, roomWasOpen: false);
            _lastStatus = status;
            Play(cue);
        }

        private void HandleLobbyChanged()
        {
            OnlineLobbySnapshot current = OnlineLobbySnapshot.Capture(_lobby);
            int count = OnlineLobbyCueRules.Diff(_lastLobby, current, _playRemotePicks, _buffer);
            _lastLobby = current;
            for (int i = 0; i < count; i++)
            {
                Play(_buffer[i]);
            }
        }

        private void Play(UiSfxCue cue)
        {
            if (_library != null && cue != UiSfxCue.None)
            {
                _pool.Play(_library.Get(cue), _rng);
            }
        }

        private void OnValidate()
        {
            if (_library == null || _session == null || _lobby == null)
            {
                Debug.LogWarning($"[{nameof(OnlineSessionSfxPlayer)}] Assign the library, session channel and lobby channel on '{name}'.", this);
            }

            SfxVoicePool.ValidateVoices(this, _voices);
        }
    }
}
