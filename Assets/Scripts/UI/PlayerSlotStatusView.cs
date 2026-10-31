using AlienFarms.Core;
using AlienFarms.Data;
using TMPro;
using UnityEngine;
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

        [SerializeField] private string _emptyText = "Presiona X / Enter para unirte";
        [SerializeField] private string _choosingText = "Eligiendo…";
        [SerializeField] private string _readyText = "¡LISTO!";

        [SerializeField, Tooltip("Frame color while the slot is empty.")]
        private Color _emptyFrameColor = Color.white;

        [Header("References")]
        [SerializeField] private TMP_Text _status;

        [SerializeField, Tooltip("Optional frame tinted with the hovered character's Theme.Primary.")]
        private Image _frame;

        private bool _hasRendered;
        private PlayerSlotState _renderedState;

        public PlayerId Player => _player;

        private void Awake()
        {
            Render(PlayerSlotState.Empty, null);
        }

        /// <summary>Shows <paramref name="state"/>; <paramref name="character"/> (hovered or locked, may be null) tints the frame.</summary>
        public void Render(PlayerSlotState state, CharacterDefinition character)
        {
            if (_status != null && (!_hasRendered || state != _renderedState))
            {
                _status.SetText(GetText(state));
            }

            _hasRendered = true;
            _renderedState = state;

            if (_frame != null)
            {
                _frame.color = state != PlayerSlotState.Empty && character != null ? character.Theme.Primary : _emptyFrameColor;
            }
        }

        private string GetText(PlayerSlotState state)
        {
            switch (state)
            {
                case PlayerSlotState.Choosing:
                    return _choosingText;
                case PlayerSlotState.Ready:
                    return _readyText;
                default:
                    return _emptyText;
            }
        }

        private void OnValidate()
        {
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
