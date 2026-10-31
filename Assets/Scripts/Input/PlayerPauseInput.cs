using AlienFarms.Core;
using AlienFarms.Data;
using UnityEngine;

namespace AlienFarms.Input
{
    /// <summary>
    /// Hold-to-pause for one player: holding this player's Pause button for <see cref="PauseSettings.HoldDuration"/>
    /// (unscaled) raises <see cref="GameEvents.OnPauseRequested"/>. Progress is published only while it changes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerPauseInput : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Hold duration and indicator tuning.")]
        private PauseSettings _settings;

        [Header("References")]
        [SerializeField, Tooltip("Input source for this player (Pause held state).")]
        private PlayerInputHandler _input;

        [SerializeField, Tooltip("Identity used as payload for global events.")]
        private PlayerIdentity _identity;

        private HoldToPauseTracker _tracker;
        private float _lastProgress;

        private void Awake()
        {
            if (_settings == null || _input == null || _identity == null)
            {
                Debug.LogError($"[{nameof(PlayerPauseInput)}] Missing settings, input handler or identity on '{name}'; hold-to-pause disabled.", this);
                enabled = false;
                return;
            }

            _tracker = new HoldToPauseTracker(_settings.HoldDuration);
        }

        private void OnDisable()
        {
            if (_tracker == null)
            {
                return;
            }

            _tracker.Reset();
            PublishProgress();
        }

        private void Update()
        {
            // Clamped like scaled time, so a hitch (or a focus-loss frame) cannot complete the hold in one step.
            float deltaTime = Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime);
            bool completed = _tracker.Tick(_input.IsPauseHeld, deltaTime);
            PublishProgress();

            if (completed)
            {
                GameEvents.RaisePauseRequested(_identity.Id);
            }
        }

        private void PublishProgress()
        {
            float progress = _tracker.Progress;
            // Exact comparison on purpose: only an actual change (holding, or the reset to 0) is published.
            if (progress == _lastProgress)
            {
                return;
            }

            _lastProgress = progress;
            GameEvents.RaisePauseHoldProgress(_identity.Id, progress);
        }

        private void OnValidate()
        {
            if (_settings == null)
            {
                Debug.LogWarning($"[{nameof(PlayerPauseInput)}] Missing PauseSettings on '{name}'.", this);
            }

            if (_input == null)
            {
                TryGetComponent(out _input);
            }

            if (_identity == null)
            {
                TryGetComponent(out _identity);
            }
        }
    }
}
