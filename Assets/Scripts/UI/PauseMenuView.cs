using AlienFarms.Core;
using AlienFarms.Data;
using TMPro;
using UnityEngine;

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
        [SerializeField, Tooltip("Subtitle format; {0} = name of the player who paused.")]
        private string _subtitleFormat = "{0} pausó";

        [SerializeField, Tooltip("Names used when a player has no character (index 0 = Player 1).")]
        private string[] _fallbackNames = { "J1", "J2" };

        [Header("References")]
        [SerializeField, Tooltip("Pause panel (inactive by default). Must not contain this component.")]
        private GameObject _panelRoot;

        [SerializeField, Tooltip("Optional subtitle text.")]
        private TMP_Text _subtitle;

        private readonly PlayerCharacterCache _characters = new PlayerCharacterCache();

        private void OnEnable()
        {
            GameEvents.OnPlayerCharacterAssigned += HandlePlayerCharacterAssigned;
            GameEvents.OnGamePaused += HandleGamePaused;
            GameEvents.OnGameResumed += HandleGameResumed;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerCharacterAssigned -= HandlePlayerCharacterAssigned;
            GameEvents.OnGamePaused -= HandleGamePaused;
            GameEvents.OnGameResumed -= HandleGameResumed;
        }

        private void HandlePlayerCharacterAssigned(PlayerId playerId, CharacterDefinition character) =>
            _characters.Set(playerId, character);

        private void HandleGamePaused(PlayerId owner)
        {
            if (_subtitle != null)
            {
                // One format per pause (not per frame).
                _subtitle.SetText(string.Format(_subtitleFormat, GetName(owner)));
            }

            SetVisible(true);
        }

        private void HandleGameResumed(PlayerId owner) => SetVisible(false);

        private string GetName(PlayerId id)
        {
            CharacterDefinition character = _characters.Get(id);
            if (character != null)
            {
                return character.DisplayName;
            }

            int index = (int)id - 1;
            return _fallbackNames != null && index >= 0 && index < _fallbackNames.Length ? _fallbackNames[index] : string.Empty;
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
