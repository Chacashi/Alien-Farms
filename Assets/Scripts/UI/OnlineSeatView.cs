using System;
using AlienFarms.Core;
using AlienFarms.Data;
using TMPro;
using UnityEngine;

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

        [Header("References")]
        [SerializeField, Tooltip("Status and frame of the seat (its empty entry should be online.room.waiting_player).")]
        private PlayerSlotStatusView _status;

        [SerializeField, Tooltip("Name of the player holding the seat.")]
        private TMP_Text _name;

        [SerializeField, Tooltip("Badge shown when the seat belongs to the host.")]
        private GameObject _hostBadge;

        [SerializeField, Tooltip("Badge shown when the seat belongs to this machine.")]
        private GameObject _youBadge;

        private string _renderedName;
        private bool _hasRenderedName;

        public PlayerId Player => _player;

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

            SetName(occupied ? seat.DisplayName : null);
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
