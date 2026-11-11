using System;
using AlienFarms.Core;
using AlienFarms.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace AlienFarms.UI
{
    /// <summary>
    /// One player seat of the online room: reuses <see cref="PlayerSlotStatusView"/> for the status ("waiting for a
    /// player" / "choosing" / "ready") and adds the player's name and the host / you badges. Rendered on lobby changes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OnlineSeatView : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Seat shown by this view.")]
        private PlayerId _player = PlayerId.Player1;

        [SerializeField, Tooltip("Name shown for Player 1 when the seat has no display name.")]
        private LocalizedString _player1Label = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.CommonP1);

        [SerializeField, Tooltip("Name shown for Player 2 when the seat has no display name.")]
        private LocalizedString _player2Label = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.CommonP2);

        [Header("References")]
        [SerializeField, Tooltip("Status and frame of the seat (its empty entry should be online.room.waiting_player).")]
        private PlayerSlotStatusView _status;

        [SerializeField, Tooltip("Name of the player holding the seat.")]
        private TMP_Text _name;

        [SerializeField, Tooltip("Badge shown when the seat belongs to the host.")]
        private GameObject _hostBadge;

        [SerializeField, Tooltip("Badge shown when the seat belongs to this machine.")]
        private GameObject _youBadge;

        private LocalizedStringBinding _labelBinding;
        private string _renderedName;
        private bool _hasRenderedName;
        private bool _usesLabel;

        public PlayerId Player => _player;

        private void Awake()
        {
            _labelBinding = new LocalizedStringBinding(_player == PlayerId.Player2 ? _player2Label : _player1Label, RefreshLabel);
        }

        private void OnEnable()
        {
            _labelBinding.Enable();
        }

        private void OnDisable()
        {
            _labelBinding.Disable();
        }

        /// <summary>
        /// Shows <paramref name="seat"/> (ignored when not <paramref name="occupied"/>); <paramref name="character"/>
        /// (picked or hovered, may be null) tints the frame.
        /// </summary>
        public void Render(in OnlineLobbySeat seat, bool occupied, CharacterDefinition character)
        {
            PlayerSlotState state = !occupied
                ? PlayerSlotState.Empty
                : seat.HasCharacter ? PlayerSlotState.Ready : PlayerSlotState.Choosing;
            if (_status != null)
            {
                _status.Render(state, character);
            }

            // An occupied seat without a display name shows the localized player label (J1 / P1).
            bool usesLabel = occupied && string.IsNullOrEmpty(seat.DisplayName);
            if (usesLabel != _usesLabel)
            {
                _usesLabel = usesLabel;
                _hasRenderedName = false;
            }

            SetName(usesLabel ? _labelBinding?.Value : occupied ? seat.DisplayName : null);
            SetActive(_hostBadge, occupied && seat.IsHost);
            SetActive(_youBadge, occupied && seat.IsLocal);
        }

        private void SetName(string displayName)
        {
            if (_name == null || (_hasRenderedName && string.Equals(_renderedName, displayName, StringComparison.Ordinal)))
            {
                return;
            }

            _hasRenderedName = true;
            _renderedName = displayName;
            _name.SetText(displayName ?? string.Empty);
        }

        // Language change only: re-renders the label while the seat uses it.
        private void RefreshLabel()
        {
            if (_usesLabel)
            {
                SetName(_labelBinding.Value);
            }
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
            if (_status == null)
            {
                TryGetComponent(out _status);
            }

            if (_player1Label == null || _player1Label.IsEmpty || _player2Label == null || _player2Label.IsEmpty)
            {
                Debug.LogWarning($"[{nameof(OnlineSeatView)}] Empty localized player label on '{name}'.", this);
            }

            if (_player == PlayerId.None)
            {
                Debug.LogWarning($"[{nameof(OnlineSeatView)}] Player is None on '{name}'.", this);
            }

            if (_status == null || _name == null || _hostBadge == null || _youBadge == null)
            {
                Debug.LogWarning($"[{nameof(OnlineSeatView)}] Missing status, name or badges on '{name}'.", this);
            }
            else if (_status.Player != _player)
            {
                Debug.LogWarning($"[{nameof(OnlineSeatView)}] Status view player ({_status.Player}) differs from {_player} on '{name}'.", this);
            }
        }
    }
}
