using AlienFarms.Core;
using AlienFarms.Data;
using UnityEngine;

namespace AlienFarms.UI
{
    /// <summary>
    /// Character select panel: forwards per-character and per-player state to its cards and slots. Driven by the menu
    /// flow after each selection change. Lives on an always-active object and toggles <see cref="_panelRoot"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterSelectView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Select panel. Also the menu root the players' event systems are scoped to. Must not contain this component.")]
        private GameObject _panelRoot;

        [SerializeField, Tooltip("One card per selectable character (matched by character, any order).")]
        private CharacterCardView[] _cards = new CharacterCardView[0];

        [SerializeField, Tooltip("One status slot per player (matched by player).")]
        private PlayerSlotStatusView[] _slots = new PlayerSlotStatusView[0];

        /// <summary>Root the players navigate under while choosing.</summary>
        public GameObject MenuRoot => _panelRoot;

        public void Show() => SetPanelActive(true);

        public void Hide() => SetPanelActive(false);

        public void RenderCard(CharacterDefinition character, bool hoverP1, bool hoverP2, PlayerId lockedBy)
        {
            for (int i = 0; i < _cards.Length; i++)
            {
                if (_cards[i] != null && _cards[i].Character == character)
                {
                    _cards[i].Render(hoverP1, hoverP2, lockedBy);
                }
            }
        }

        public void RenderSlot(PlayerId player, PlayerSlotState state, CharacterDefinition character)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null && _slots[i].Player == player)
                {
                    _slots[i].Render(state, character);
                }
            }
        }

        private void SetPanelActive(bool active)
        {
            if (_panelRoot != null && _panelRoot.activeSelf != active)
            {
                _panelRoot.SetActive(active);
            }
        }

        private void OnValidate()
        {
            if (_panelRoot == null)
            {
                Debug.LogWarning($"[{nameof(CharacterSelectView)}] Missing panel root on '{name}'.", this);
            }
            else if (_panelRoot == gameObject || transform.IsChildOf(_panelRoot.transform))
            {
                Debug.LogWarning($"[{nameof(CharacterSelectView)}] Panel root on '{name}' contains this view.", this);
            }

            if (_cards == null || _cards.Length == 0 || _slots == null || _slots.Length == 0)
            {
                Debug.LogWarning($"[{nameof(CharacterSelectView)}] '{name}' needs character cards and player slots.", this);
            }
        }
    }
}
