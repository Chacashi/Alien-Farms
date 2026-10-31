using AlienFarms.Core;
using AlienFarms.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AlienFarms.UI
{
    /// <summary>
    /// Styles one player's HUD block with its character: name, portrait and theme colors. The score number itself is
    /// written by <see cref="ScoreView"/>; this view only tints it. Applied once per character assignment.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerHudThemeView : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Player whose HUD this is.")]
        private PlayerId _player = PlayerId.Player1;

        [SerializeField, Tooltip("Character shown before the player joins (and after it leaves).")]
        private CharacterDefinition _default;

        [Header("References")]
        [SerializeField, Tooltip("Character name text (Theme.Text color).")]
        private TMP_Text _name;

        [SerializeField, Tooltip("Portrait image. Without a portrait sprite it shows a Theme.Primary square.")]
        private Image _portrait;

        [SerializeField, Tooltip("Optional stripe under the name (Theme.Secondary color).")]
        private Image _themeStripe;

        [SerializeField, Tooltip("Optional score text, tinted with Theme.Secondary (number written by ScoreView).")]
        private TMP_Text _score;

        private void OnEnable()
        {
            GameEvents.OnPlayerCharacterAssigned += HandlePlayerCharacterAssigned;
            GameEvents.OnPlayerLeft += HandlePlayerLeft;
            Apply(_default);
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerCharacterAssigned -= HandlePlayerCharacterAssigned;
            GameEvents.OnPlayerLeft -= HandlePlayerLeft;
        }

        private void HandlePlayerCharacterAssigned(PlayerId playerId, CharacterDefinition character)
        {
            if (playerId == _player)
            {
                Apply(character != null ? character : _default);
            }
        }

        private void HandlePlayerLeft(PlayerId playerId)
        {
            if (playerId == _player)
            {
                Apply(_default);
            }
        }

        private void Apply(CharacterDefinition character)
        {
            if (character == null)
            {
                return;
            }

            CharacterTheme theme = character.Theme;

            if (_name != null)
            {
                _name.SetText(character.DisplayName);
                _name.color = theme.Text;
            }

            if (_portrait != null)
            {
                Sprite portrait = character.Portrait;
                _portrait.sprite = portrait;
                _portrait.color = portrait != null ? Color.white : theme.Primary;
            }

            if (_themeStripe != null)
            {
                _themeStripe.color = theme.Secondary;
            }

            if (_score != null)
            {
                _score.color = theme.Secondary;
            }
        }

        private void OnValidate()
        {
            if (_player == PlayerId.None)
            {
                Debug.LogWarning($"[{nameof(PlayerHudThemeView)}] Player is None on '{name}'.", this);
            }

            if (_default == null)
            {
                Debug.LogWarning($"[{nameof(PlayerHudThemeView)}] Missing default character on '{name}'.", this);
            }

            if (_name == null || _portrait == null)
            {
                Debug.LogWarning($"[{nameof(PlayerHudThemeView)}] Missing name text or portrait image on '{name}'.", this);
            }
        }
    }
}
