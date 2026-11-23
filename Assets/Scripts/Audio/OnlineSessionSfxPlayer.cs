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
        private bool _joiningIsRejoin;
        private bool _roomWasOpen;
        private UiSfxCue _pendingCue;
        private float _pendingCueTime;

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
                _joiningIsRejoin = _lastStatus == OnlineSessionStatus.Joining && _session.HasRejoinOffer;
                _session.Changed += HandleSessionChanged;
            }

            if (_lobby != null)
            {
                _lastLobby = OnlineLobbySnapshot.Capture(_lobby);
                _roomWasOpen = _lastLobby.Phase == OnlineLobbyPhase.Open;
                _lobby.Changed += HandleLobbyChanged;
            }
        }

        private void Start()
        {
            if (_session != null && _library != null
                && OnlineSessionCueRules.IsArrivalNotice(_session.Status, _session.LastErrorKey))
            {
                _pendingCue = UiSfxCue.ConnectionLost;
                _pendingCueTime = Time.unscaledTime + _library.ArrivalCueDelay;
            }
        }

        private void Update()
        {
            if (_pendingCue == UiSfxCue.None || Time.unscaledTime < _pendingCueTime)
            {
                return;
            }

            UiSfxCue cue = _pendingCue;
            _pendingCue = UiSfxCue.None;
            Play(cue);
        }

        private void OnDisable()
        {
            _pendingCue = UiSfxCue.None;

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

            if (status == OnlineSessionStatus.Joining)
            {
                _joiningIsRejoin = _session.HasRejoinOffer;
            }

            UiSfxCue cue = OnlineSessionCueRules.OnStatusChanged(_lastStatus, status,
                hasError: !string.IsNullOrEmpty(_session.LastErrorKey), _joiningIsRejoin, _roomWasOpen);
            if (status == OnlineSessionStatus.InSession)
            {
                _roomWasOpen = false;
            }

            _lastStatus = status;
            Play(cue);
        }

        private void HandleLobbyChanged()
        {
            OnlineLobbySnapshot current = OnlineLobbySnapshot.Capture(_lobby);
            if (current.Phase == OnlineLobbyPhase.Open)
            {
                _roomWasOpen = true;
            }
            else if (current.Phase == OnlineLobbyPhase.Starting)
            {
                _roomWasOpen = false;
            }

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
