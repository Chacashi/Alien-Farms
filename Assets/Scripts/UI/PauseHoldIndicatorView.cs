using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Match;
using UnityEngine;
using UnityEngine.UI;

namespace AlienFarms.UI
{
    /// <summary>
    /// Shows one player's hold-to-pause progress as a filled bar. Appears only after holding for
    /// <see cref="PauseSettings.ShowProgressAfter"/>, never while paused or in a phase that cannot pause.
    /// Lives on an always-active object and toggles <see cref="_root"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseHoldIndicatorView : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Player whose hold progress this shows.")]
        private PlayerId _player = PlayerId.Player1;

        [SerializeField, Tooltip("Hold duration and show threshold.")]
        private PauseSettings _settings;

        [Header("References")]
        [SerializeField, Tooltip("Indicator root (inactive by default). Must not contain this component.")]
        private GameObject _root;

        [SerializeField, Tooltip("Image with Image Type = Filled (horizontal); fillAmount = progress.")]
        private Image _fill;

        private bool _isPaused;
        private bool _canPause = true;

        private void OnEnable()
        {
            GameEvents.OnPauseHoldProgress += HandlePauseHoldProgress;
            GameEvents.OnGamePaused += HandleGamePaused;
            GameEvents.OnGameResumed += HandleGameResumed;
            GameEvents.OnMatchPhaseChanged += HandleMatchPhaseChanged;
            SetVisible(false);
        }

        private void OnDisable()
        {
            GameEvents.OnPauseHoldProgress -= HandlePauseHoldProgress;
            GameEvents.OnGamePaused -= HandleGamePaused;
            GameEvents.OnGameResumed -= HandleGameResumed;
            GameEvents.OnMatchPhaseChanged -= HandleMatchPhaseChanged;
        }

        private void HandlePauseHoldProgress(PlayerId playerId, float normalized)
        {
            if (playerId != _player)
            {
                return;
            }

            float heldSeconds = _settings != null ? normalized * _settings.HoldDuration : normalized;
            float showAfter = _settings != null ? _settings.ShowProgressAfter : 0f;
            bool visible = !_isPaused && _canPause && normalized > 0f && heldSeconds >= showAfter;

            if (visible && _fill != null)
            {
                _fill.fillAmount = normalized;
            }

            SetVisible(visible);
        }

        private void HandleGamePaused(PlayerId owner)
        {
            _isPaused = true;
            SetVisible(false);
        }

        private void HandleGameResumed(PlayerId owner)
        {
            _isPaused = false;
        }

        private void HandleMatchPhaseChanged(MatchPhase previous, MatchPhase current)
        {
            _canPause = PauseRules.CanPause(current);
            if (!_canPause)
            {
                SetVisible(false);
            }
        }

        private void SetVisible(bool visible)
        {
            if (_root != null && _root.activeSelf != visible)
            {
                _root.SetActive(visible);
            }
        }

        private void OnValidate()
        {
            if (_player == PlayerId.None)
            {
                Debug.LogWarning($"[{nameof(PauseHoldIndicatorView)}] Player is None on '{name}'.", this);
            }

            if (_settings == null)
            {
                Debug.LogWarning($"[{nameof(PauseHoldIndicatorView)}] Missing PauseSettings on '{name}'.", this);
            }

            if (_fill == null)
            {
                Debug.LogWarning($"[{nameof(PauseHoldIndicatorView)}] Missing fill image on '{name}'.", this);
            }
            else if (_fill.type != Image.Type.Filled)
            {
                Debug.LogWarning($"[{nameof(PauseHoldIndicatorView)}] Fill image on '{name}' should use Image Type = Filled.", this);
            }

            if (_root == null)
            {
                Debug.LogWarning($"[{nameof(PauseHoldIndicatorView)}] Missing root on '{name}'.", this);
            }
            else if (_root == gameObject || transform.IsChildOf(_root.transform))
            {
                Debug.LogWarning($"[{nameof(PauseHoldIndicatorView)}] Root on '{name}' contains this view; hiding it would stop listening to events.", this);
            }
        }
    }
}
