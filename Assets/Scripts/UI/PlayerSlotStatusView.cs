using System;
using AlienFarms.Core;
using AlienFarms.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace AlienFarms.UI
{
    /// <summary>
    /// One player's status on the character select screen ("press to join" / "choosing" / "ready"), with the frame
    /// tinted by the hovered character's Theme.Primary. Text is only rewritten when the state changes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerSlotStatusView : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Player shown by this slot.")]
        private PlayerId _player = PlayerId.Player1;

        [SerializeField, Tooltip("Status while nobody occupies the slot.")]
        private LocalizedString _emptyEntry = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.CommonPressToJoin);

        [SerializeField, Tooltip("Status while the player is choosing.")]
        private LocalizedString _choosingEntry = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.SelectChoosing);

        [SerializeField, Tooltip("Status once the player confirmed.")]
        private LocalizedString _readyEntry = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.SelectReady);

        [SerializeField, Tooltip("Frame color while the slot is empty.")]
        private Color _emptyFrameColor = Color.white;

        [Header("References")]
        [SerializeField] private TMP_Text _status;

        [SerializeField, Tooltip("Optional frame tinted with the hovered character's Theme.Primary.")]
        private Image _frame;

        private bool _hasRendered;
        private PlayerSlotState _renderedState;
        private LocalizedStringBinding _emptyBinding;
        private LocalizedStringBinding _choosingBinding;
        private LocalizedStringBinding _readyBinding;

        public PlayerId Player => _player;

        private void Awake()
        {
            Action refresh = RefreshStatusText;
            _emptyBinding = new LocalizedStringBinding(_emptyEntry, refresh);
            _choosingBinding = new LocalizedStringBinding(_choosingEntry, refresh);
            _readyBinding = new LocalizedStringBinding(_readyEntry, refresh);
            Render(PlayerSlotState.Empty, null);
        }

        private void OnEnable()
        {
            _emptyBinding.Enable();
            _choosingBinding.Enable();
            _readyBinding.Enable();
        }

        private void OnDisable()
        {
            _emptyBinding.Disable();
            _choosingBinding.Disable();
            _readyBinding.Disable();
        }

        /// <summary>Shows <paramref name="state"/>; <paramref name="character"/> (hovered or locked, may be null) tints the frame.</summary>
        public void Render(PlayerSlotState state, CharacterDefinition character)
        {
            bool changed = !_hasRendered || state != _renderedState;
            _hasRendered = true;
            _renderedState = state;
            if (changed)
            {
                RefreshStatusText();
            }

            if (_frame != null)
            {
                _frame.color = state != PlayerSlotState.Empty && character != null ? character.Theme.Primary : _emptyFrameColor;
            }
        }

        // On state change or language change (not per frame).
        private void RefreshStatusText()
        {
            if (_status == null || !_hasRendered)
            {
                return;
            }

            // Bindings exist after Awake; until then the scene text stays.
            string text = GetBinding(_renderedState)?.Value;
            if (text != null)
            {
                _status.SetText(text);
            }
        }

        private LocalizedStringBinding GetBinding(PlayerSlotState state)
        {
            switch (state)
            {
                case PlayerSlotState.Choosing:
                    return _choosingBinding;
                case PlayerSlotState.Ready:
                    return _readyBinding;
                default:
                    return _emptyBinding;
            }
        }

        private void OnValidate()
        {
            if (_emptyEntry == null || _emptyEntry.IsEmpty || _choosingEntry == null || _choosingEntry.IsEmpty
                || _readyEntry == null || _readyEntry.IsEmpty)
            {
                Debug.LogWarning($"[{nameof(PlayerSlotStatusView)}] Empty localized string on '{name}'.", this);
            }

            if (_player == PlayerId.None)
            {
                Debug.LogWarning($"[{nameof(PlayerSlotStatusView)}] Player is None on '{name}'.", this);
            }

            if (_status == null)
            {
                Debug.LogWarning($"[{nameof(PlayerSlotStatusView)}] Missing status text on '{name}'.", this);
            }
        }
    }
}
