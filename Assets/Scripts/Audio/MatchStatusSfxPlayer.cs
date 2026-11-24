using System;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Utilities;
using UnityEngine;

namespace AlienFarms.Audio
{
    [DisallowMultipleComponent]
    public sealed class MatchStatusSfxPlayer : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Sound cues for match connection status and the pause menu.")]
        private UiSfxLibrary _library;

        [Header("Voices")]
        [SerializeField, Tooltip("Three pooled AudioSources routed to AlienFarmsMixer > SFX.")]
        private AudioSource[] _voices;

        private readonly MatchConnectionCueTracker _tracker = new MatchConnectionCueTracker();
        private readonly UiSfxCue[] _buffer = new UiSfxCue[2];
        private SfxVoicePool _pool;
        private IRandomSource _rng;
        private UiSfxCue _pendingCue;
        private float _pendingCueTime;

        private void Awake()
        {
            _pool = new SfxVoicePool(_voices);
            _rng = new SystemRandomSource(Environment.TickCount);
        }

        private void OnEnable()
        {
            GameEvents.OnMatchPhaseChanged += HandleMatchPhaseChanged;
            GameEvents.OnPlayerConnectionChanged += HandlePlayerConnectionChanged;
            GameEvents.OnGamePaused += HandleGamePaused;
            GameEvents.OnPauseMenuResumed += HandlePauseMenuResumed;
        }

        private void OnDisable()
        {
            GameEvents.OnMatchPhaseChanged -= HandleMatchPhaseChanged;
            GameEvents.OnPlayerConnectionChanged -= HandlePlayerConnectionChanged;
            GameEvents.OnGamePaused -= HandleGamePaused;
            GameEvents.OnPauseMenuResumed -= HandlePauseMenuResumed;
            _tracker.Reset();
            _pendingCue = UiSfxCue.None;
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

        private void HandleMatchPhaseChanged(MatchPhase previous, MatchPhase current) => _tracker.SetPhase(current);

        private void HandlePlayerConnectionChanged(PlayerId id, PlayerConnectionStatus status, float graceSeconds)
        {
            int count = _tracker.Apply(id, status, _buffer);
            if (count == 0)
            {
                return;
            }

            Play(_buffer[0]);
            if (count == 2 && _buffer[1] == UiSfxCue.MatchResumed && _library != null)
            {
                _pendingCue = _buffer[1];
                _pendingCueTime = Time.unscaledTime + _library.ResumeAfterReconnectDelay;
            }
        }

        private void HandleGamePaused(PlayerId owner) => Play(UiSfxCue.MatchPaused, ignoreListenerPause: true);

        private void HandlePauseMenuResumed(PlayerId owner) => Play(UiSfxCue.MatchResumed);

        private void Play(UiSfxCue cue, bool ignoreListenerPause = false)
        {
            if (_library != null)
            {
                _pool.Play(_library.Get(cue), _rng, ignoreListenerPause);
            }
        }

        private void OnValidate()
        {
            if (_library == null)
            {
                Debug.LogWarning($"[{nameof(MatchStatusSfxPlayer)}] Assign the UI SFX library on '{name}'.", this);
            }

            SfxVoicePool.ValidateVoices(this, _voices);
        }
    }
}
