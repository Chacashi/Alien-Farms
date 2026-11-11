using System;
using AlienFarms.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AlienFarms.UI
{
    /// <summary>
    /// Makes a <see cref="CharacterCardView"/> a navigable button (online room): selecting it reports the hovered
    /// character and clicking (Submit) reports the pick. The card itself still renders cursors and locks.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class CharacterCardButton : MonoBehaviour, ISelectHandler
    {
        [Header("References")]
        [SerializeField, Tooltip("Card rendered by this button (same object or a child).")]
        private CharacterCardView _card;

        private Button _button;

        /// <summary>The card was clicked or submitted.</summary>
        public event Action<CharacterDefinition> Clicked;

        /// <summary>The card received the focus of its player's event system.</summary>
        public event Action<CharacterDefinition> Selected;

        public CharacterCardView Card => _card;

        public CharacterDefinition Character => _card != null ? _card.Character : null;

        public bool Interactable
        {
            get => _button != null && _button.interactable;
            set
            {
                if (_button != null && _button.interactable != value)
                {
                    _button.interactable = value;
                }
            }
        }

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClick);
            }
        }

        public void OnSelect(BaseEventData eventData)
        {
            Selected?.Invoke(Character);
        }

        private void HandleClick()
        {
            Clicked?.Invoke(Character);
        }

        private void OnValidate()
        {
            if (_card == null)
            {
                TryGetComponent(out _card);
            }

            if (_card == null)
            {
                Debug.LogWarning($"[{nameof(CharacterCardButton)}] Missing CharacterCardView on '{name}'.", this);
            }
        }
    }
}
