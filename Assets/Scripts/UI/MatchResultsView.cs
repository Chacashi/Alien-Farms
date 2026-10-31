using AlienFarms.Core;
using AlienFarms.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AlienFarms.UI
{
    /// <summary>
    /// Shows the round result (winner by character name, both names and the score) while the match is in Results.
    /// The winner text is tinted with the winner's Theme.Secondary; names use the dark Theme.Accent (readable on the
    /// cream wood panel) and optional stripes under them carry Theme.Secondary. Lives on an always-active object and toggles
    /// <see cref="_panelRoot"/>. Buttons (rematch, title) are wired in the scene; the menu owner is set by ResultsMenuController.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchResultsView : MonoBehaviour
    {
        [Header("Texts")]
        [SerializeField, Tooltip("Winner format; {0} = winner's character name.")]
        private string _winnerFormat = "¡Gana {0}!";

        [SerializeField] private string _drawText = "¡Empate!";

        [SerializeField, Tooltip("TMP format; {0} = Player 1 score, {1} = Player 2 score.")]
        private string _scoreFormat = "{0} - {1}";

        [SerializeField, Tooltip("Names used when a player has no character (index 0 = Player 1).")]
        private string[] _fallbackNames = { "J1", "J2" };

        [Header("References")]
        [SerializeField, Tooltip("Results panel (text lives in the scene). Must not contain this component.")]
        private GameObject _panelRoot;

        [SerializeField] private TMP_Text _winnerText;
        [SerializeField] private TMP_Text _scoreText;

        [SerializeField, Tooltip("Optional: Player 1 character name (left).")]
        private TMP_Text _nameP1;

        [SerializeField, Tooltip("Optional: Player 2 character name (right).")]
        private TMP_Text _nameP2;

        [SerializeField, Tooltip("Optional: stripe/underline under Player 1's name (Theme.Secondary color).")]
        private Image _nameStripeP1;

        [SerializeField, Tooltip("Optional: stripe/underline under Player 2's name (Theme.Secondary color).")]
        private Image _nameStripeP2;

        private readonly PlayerCharacterCache _characters = new PlayerCharacterCache();
        private Color _winnerDefaultColor = Color.white;

        private void Awake()
        {
            if (_winnerText != null)
            {
                _winnerDefaultColor = _winnerText.color;
            }
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerCharacterAssigned += HandlePlayerCharacterAssigned;
            GameEvents.OnMatchEnded += HandleMatchEnded;
            GameEvents.OnMatchPhaseChanged += HandleMatchPhaseChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerCharacterAssigned -= HandlePlayerCharacterAssigned;
            GameEvents.OnMatchEnded -= HandleMatchEnded;
            GameEvents.OnMatchPhaseChanged -= HandleMatchPhaseChanged;
        }

        private void HandlePlayerCharacterAssigned(PlayerId playerId, CharacterDefinition character) =>
            _characters.Set(playerId, character);

        private void HandleMatchEnded(MatchResult result)
        {
            if (_winnerText != null)
            {
                if (result.IsDraw)
                {
                    _winnerText.SetText(_drawText);
                    _winnerText.color = _winnerDefaultColor;
                }
                else
                {
                    // One format per match (not per frame).
                    _winnerText.SetText(string.Format(_winnerFormat, GetName(result.Winner)));
                    CharacterDefinition winner = _characters.Get(result.Winner);
                    _winnerText.color = winner != null ? winner.Theme.Secondary : _winnerDefaultColor;
                }
            }

            if (_scoreText != null)
            {
                _scoreText.SetText(_scoreFormat, result.Player1Score, result.Player2Score);
            }

            SetName(_nameP1, _nameStripeP1, PlayerId.Player1);
            SetName(_nameP2, _nameStripeP2, PlayerId.Player2);
            SetVisible(true);
        }

        private void HandleMatchPhaseChanged(MatchPhase previous, MatchPhase current)
        {
            if (current != MatchPhase.Results)
            {
                SetVisible(false);
            }
        }

        private void SetName(TMP_Text text, Image stripe, PlayerId id)
        {
            CharacterDefinition character = _characters.Get(id);
            if (text != null)
            {
                text.SetText(GetName(id));
                if (character != null)
                {
                    text.color = character.Theme.Accent;
                }
            }

            if (stripe != null && character != null)
            {
                stripe.color = character.Theme.Secondary;
            }
        }

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
            if (_winnerText == null || _scoreText == null)
            {
                Debug.LogWarning($"[{nameof(MatchResultsView)}] Missing winner or score text on '{name}'.", this);
            }

            if (_panelRoot == null)
            {
                Debug.LogWarning($"[{nameof(MatchResultsView)}] Missing panel root on '{name}'.", this);
            }
            else if (_panelRoot == gameObject || transform.IsChildOf(_panelRoot.transform))
            {
                Debug.LogWarning($"[{nameof(MatchResultsView)}] Panel root on '{name}' contains this view; hiding it would stop listening to events.", this);
            }
        }
    }
}
