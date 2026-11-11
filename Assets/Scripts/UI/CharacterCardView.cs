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
    /// One character card on the select screen: portrait and name of its character, which players hover it and who
    /// locked it. Rendered by <see cref="CharacterSelectView"/> after each selection change (not per frame).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterCardView : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Character shown by this card.")]
        private CharacterDefinition _character;

        [SerializeField, Tooltip("Locked-by format (non-Smart); {0} = player label.")]
        private LocalizedString _takenByEntry = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.SelectTakenBy);

        [SerializeField, Tooltip("Player 1 label.")]
        private LocalizedString _player1Label = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.CommonP1);

        [SerializeField, Tooltip("Player 2 label.")]
        private LocalizedString _player2Label = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.CommonP2);

        [Header("References")]
        [SerializeField, Tooltip("Portrait image. Without a portrait sprite it shows a Theme.Primary square.")]
        private Image _portrait;

        [SerializeField] private TMP_Text _name;

        [SerializeField, Tooltip("Overlay shown while the character is locked by a player.")]
        private GameObject _lockedOverlay;

        [SerializeField, Tooltip("Optional: 'taken by' text inside the locked overlay.")]
        private TMP_Text _lockedBy;

        [SerializeField, Tooltip("Player 1 cursor badge (hovering, not confirmed).")]
        private GameObject _cursorP1;

        [SerializeField, Tooltip("Player 2 cursor badge (hovering, not confirmed).")]
        private GameObject _cursorP2;

        [SerializeField, Tooltip("'Ready' stamp shown while locked.")]
        private GameObject _readyStamp;

        private PlayerId _renderedLockedBy = PlayerId.None;
        private LocalizedStringBinding _takenByBinding;
        private LocalizedStringBinding _player1Binding;
        private LocalizedStringBinding _player2Binding;

        public CharacterDefinition Character => _character;

        private void Awake()
        {
            Action refresh = RefreshLockedByText;
            _takenByBinding = new LocalizedStringBinding(_takenByEntry, refresh);
            _player1Binding = new LocalizedStringBinding(_player1Label, refresh);
            _player2Binding = new LocalizedStringBinding(_player2Label, refresh);
            ApplyCharacter();
            Render(false, false, PlayerId.None);
        }

        private void OnEnable()
        {
            _takenByBinding.Enable();
            _player1Binding.Enable();
            _player2Binding.Enable();
        }

        private void OnDisable()
        {
            _takenByBinding.Disable();
            _player1Binding.Disable();
            _player2Binding.Disable();
        }

        /// <summary>Shows who hovers this card and who locked it (None = free).</summary>
        public void Render(bool hoverP1, bool hoverP2, PlayerId lockedBy)
        {
            bool locked = lockedBy != PlayerId.None;
            SetActive(_cursorP1, hoverP1);
            SetActive(_cursorP2, hoverP2);
            SetActive(_lockedOverlay, locked);
            SetActive(_readyStamp, locked);

            bool ownerChanged = lockedBy != _renderedLockedBy;
            _renderedLockedBy = lockedBy;
            if (locked && ownerChanged)
            {
                // Only when the owner changes: once per confirm.
                RefreshLockedByText();
            }
        }

        // On owner change or language change (not per frame).
        private void RefreshLockedByText()
        {
            // Bindings exist after Awake; until then the prefab text stays.
            string format = _takenByBinding?.Value;
            if (_lockedBy == null || format == null || _renderedLockedBy == PlayerId.None)
            {
                return;
            }

            _lockedBy.SetText(string.Format(format, GetLabel(_renderedLockedBy)));
        }

        private void ApplyCharacter()
        {
            if (_character == null)
            {
                return;
            }

            if (_name != null)
            {
                _name.SetText(_character.DisplayName);
            }

            if (_portrait != null)
            {
                Sprite portrait = _character.Portrait;
                _portrait.sprite = portrait;
                _portrait.color = portrait != null ? Color.white : _character.Theme.Primary;
            }
        }

        private string GetLabel(PlayerId id)
        {
            string label = id == PlayerId.Player2 ? _player2Binding.Value : _player1Binding.Value;
            return label ?? string.Empty;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }

        private void OnValidate()
        {
            if (_takenByEntry == null || _takenByEntry.IsEmpty || _player1Label == null || _player1Label.IsEmpty
                || _player2Label == null || _player2Label.IsEmpty)
            {
                Debug.LogWarning($"[{nameof(CharacterCardView)}] Empty localized string on '{name}'.", this);
            }

            if (_character == null)
            {
                Debug.LogWarning($"[{nameof(CharacterCardView)}] Missing character on '{name}'.", this);
            }

            if (_portrait == null || _name == null)
            {
                Debug.LogWarning($"[{nameof(CharacterCardView)}] Missing portrait or name on '{name}'.", this);
            }

            if (_lockedOverlay == null || _cursorP1 == null || _cursorP2 == null || _readyStamp == null)
            {
                Debug.LogWarning($"[{nameof(CharacterCardView)}] Missing locked overlay, cursor or ready stamp on '{name}'.", this);
            }
        }
    }
}
