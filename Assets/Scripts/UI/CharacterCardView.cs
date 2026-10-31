using AlienFarms.Core;
using AlienFarms.Data;
using TMPro;
using UnityEngine;
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

        [SerializeField, Tooltip("Locked-by format; {0} = player label.")]
        private string _lockedByFormat = "Tomado por {0}";

        [SerializeField, Tooltip("Player labels (index 0 = Player 1).")]
        private string[] _playerLabels = { "J1", "J2" };

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

        public CharacterDefinition Character => _character;

        private void Awake()
        {
            ApplyCharacter();
            Render(false, false, PlayerId.None);
        }

        /// <summary>Shows who hovers this card and who locked it (None = free).</summary>
        public void Render(bool hoverP1, bool hoverP2, PlayerId lockedBy)
        {
            bool locked = lockedBy != PlayerId.None;
            SetActive(_cursorP1, hoverP1);
            SetActive(_cursorP2, hoverP2);
            SetActive(_lockedOverlay, locked);
            SetActive(_readyStamp, locked);

            if (_lockedBy != null && locked && lockedBy != _renderedLockedBy)
            {
                // Only when the owner changes: once per confirm.
                _lockedBy.SetText(string.Format(_lockedByFormat, GetLabel(lockedBy)));
            }

            _renderedLockedBy = lockedBy;
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
            int index = (int)id - 1;
            return _playerLabels != null && index >= 0 && index < _playerLabels.Length ? _playerLabels[index] : string.Empty;
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
