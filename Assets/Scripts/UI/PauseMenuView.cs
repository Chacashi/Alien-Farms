using System;
using AlienFarms.Core;
using AlienFarms.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace AlienFarms.UI
{
    /// <summary>
    /// Shows the pause panel while the game is paused, with a subtitle naming who paused. Lives on an always-active
    /// object and toggles <see cref="_panelRoot"/>. Buttons are wired to PauseController in the scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseMenuView : MonoBehaviour
    {
        [Header("Texts")]
        [SerializeField, Tooltip("Subtitle format (non-Smart); {0} = name of the player who paused.")]
        private LocalizedString _subtitleEntry = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.PauseSubtitle);

        [SerializeField, Tooltip("Player 1 name when it has no character.")]
        private LocalizedString _player1Label = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.CommonP1);

        [SerializeField, Tooltip("Player 2 name when it has no character.")]
        private LocalizedString _player2Label = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.CommonP2);

        [Header("References")]
        [SerializeField, Tooltip("Pause panel (inactive by default). Must not contain this component.")]
        private GameObject _panelRoot;

        [SerializeField, Tooltip("Optional subtitle text.")]
        private TMP_Text _subtitle;

        private readonly PlayerCharacterCache _characters = new PlayerCharacterCache();
        private LocalizedStringBinding _subtitleBinding;
        private LocalizedStringBinding _player1Binding;
        private LocalizedStringBinding _player2Binding;
        private PlayerId _lastOwner = PlayerId.None;

        private void Awake()
        {
            Action refresh = RefreshSubtitleIfVisible;
            _subtitleBinding = new LocalizedStringBinding(_subtitleEntry, refresh);
            _player1Binding = new LocalizedStringBinding(_player1Label, refresh);
            _player2Binding = new LocalizedStringBinding(_player2Label, refresh);
        }

        private void OnEnable()
        {
            _subtitleBinding.Enable();
            _player1Binding.Enable();
            _player2Binding.Enable();
            GameEvents.OnPlayerCharacterAssigned += HandlePlayerCharacterAssigned;
            GameEvents.OnGamePaused += HandleGamePaused;
            GameEvents.OnGameResumed += HandleGameResumed;
        }

        private void OnDisable()
        {
            _subtitleBinding.Disable();
            _player1Binding.Disable();
            _player2Binding.Disable();
            GameEvents.OnPlayerCharacterAssigned -= HandlePlayerCharacterAssigned;
            GameEvents.OnGamePaused -= HandleGamePaused;
            GameEvents.OnGameResumed -= HandleGameResumed;
        }

        private void HandlePlayerCharacterAssigned(PlayerId playerId, CharacterDefinition character) =>
            _characters.Set(playerId, character);

        private void HandleGamePaused(PlayerId owner)
        {
            _lastOwner = owner;
            RenderSubtitle();
            SetVisible(true);
        }

        // The language can change from the options panel opened inside the pause menu.
        private void RefreshSubtitleIfVisible()
        {
            if (_panelRoot != null && _panelRoot.activeSelf)
            {
                RenderSubtitle();
            }
        }

        private void RenderSubtitle()
        {
            string format = _subtitleBinding.Value;
            if (_subtitle == null || format == null || _lastOwner == PlayerId.None)
            {
                return;
            }

            // One format per pause or language change (not per frame).
            _subtitle.SetText(string.Format(format, GetName(_lastOwner)));
        }

        private void HandleGameResumed(PlayerId owner) => SetVisible(false);

        private string GetName(PlayerId id)
        {
            CharacterDefinition character = _characters.Get(id);
            if (character != null)
            {
                return character.DisplayName;
            }

            string label = id == PlayerId.Player2 ? _player2Binding.Value : _player1Binding.Value;
            return label ?? string.Empty;
        }

        private void SetVisible(bool visible)
        {
            if (_panelRoot != null && _panelRoot.activeSelf != visible)
            {
                _panelRoot.SetActive(visible);
            }
        }

        private void OnValidate()
        {
            if (_subtitleEntry == null || _subtitleEntry.IsEmpty || _player1Label == null || _player1Label.IsEmpty
                || _player2Label == null || _player2Label.IsEmpty)
            {
                Debug.LogWarning($"[{nameof(PauseMenuView)}] Empty localized string on '{name}'.", this);
            }

            if (_panelRoot == null)
            {
                Debug.LogWarning($"[{nameof(PauseMenuView)}] Missing panel root on '{name}'.", this);
            }
            else if (_panelRoot == gameObject || transform.IsChildOf(_panelRoot.transform))
            {
                Debug.LogWarning($"[{nameof(PauseMenuView)}] Panel root on '{name}' contains this view; hiding it would stop listening to events.", this);
            }
        }
    }
}
