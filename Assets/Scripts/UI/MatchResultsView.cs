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
    /// Shows the round result (winner by character name, both names and the score) while the match is in Results.
    /// The winner text is tinted with the winner's Theme.Secondary; names use the dark Theme.Accent (readable on the
    /// cream wood panel) and optional stripes under them carry Theme.Secondary. Lives on an always-active object and toggles
    /// <see cref="_panelRoot"/>. Buttons (rematch, title) are wired in the scene; the menu owner is set by ResultsMenuController.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchResultsView : MonoBehaviour
    {
        [Header("Texts")]
        [SerializeField, Tooltip("Winner format (non-Smart); {0} = winner's character name.")]
        private LocalizedString _winnerEntry = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.ResultsWinner);

        [SerializeField, Tooltip("Text shown on a draw.")]
        private LocalizedString _drawEntry = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.ResultsDraw);

        [SerializeField, Tooltip("TMP format; {0} = Player 1 score, {1} = Player 2 score.")]
        private string _scoreFormat = "{0} - {1}";

        [SerializeField, Tooltip("Player 1 name when it has no character.")]
        private LocalizedString _player1Label = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.CommonP1);

        [SerializeField, Tooltip("Player 2 name when it has no character.")]
        private LocalizedString _player2Label = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.CommonP2);

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
        private LocalizedStringBinding _winnerBinding;
        private LocalizedStringBinding _drawBinding;
        private LocalizedStringBinding _player1Binding;
        private LocalizedStringBinding _player2Binding;
        private MatchResult _lastResult;
        private bool _hasResult;

        private void Awake()
        {
            Action refresh = RefreshIfVisible;
            _winnerBinding = new LocalizedStringBinding(_winnerEntry, refresh);
            _drawBinding = new LocalizedStringBinding(_drawEntry, refresh);
            _player1Binding = new LocalizedStringBinding(_player1Label, refresh);
            _player2Binding = new LocalizedStringBinding(_player2Label, refresh);

            if (_winnerText != null)
            {
                _winnerDefaultColor = _winnerText.color;
            }
        }

        private void OnEnable()
        {
            _winnerBinding.Enable();
            _drawBinding.Enable();
            _player1Binding.Enable();
            _player2Binding.Enable();
            GameEvents.OnPlayerCharacterAssigned += HandlePlayerCharacterAssigned;
            GameEvents.OnMatchEnded += HandleMatchEnded;
            GameEvents.OnMatchPhaseChanged += HandleMatchPhaseChanged;
        }

        private void OnDisable()
        {
            _winnerBinding.Disable();
            _drawBinding.Disable();
            _player1Binding.Disable();
            _player2Binding.Disable();
            GameEvents.OnPlayerCharacterAssigned -= HandlePlayerCharacterAssigned;
            GameEvents.OnMatchEnded -= HandleMatchEnded;
            GameEvents.OnMatchPhaseChanged -= HandleMatchPhaseChanged;
        }

        private void HandlePlayerCharacterAssigned(PlayerId playerId, CharacterDefinition character) =>
            _characters.Set(playerId, character);

        private void HandleMatchEnded(MatchResult result)
        {
            _lastResult = result;
            _hasResult = true;
            RenderTexts();

            if (_scoreText != null)
            {
                _scoreText.SetText(_scoreFormat, result.Player1Score, result.Player2Score);
            }

            SetVisible(true);
        }

        // The language can change while the results panel is open (e.g. rematch flow in another locale).
        private void RefreshIfVisible()
        {
            if (_hasResult && _panelRoot != null && _panelRoot.activeSelf)
            {
                RenderTexts();
            }
        }

        private void RenderTexts()
        {
            if (_winnerText != null)
            {
                if (_lastResult.IsDraw)
                {
                    string draw = _drawBinding.Value;
                    if (draw != null)
                    {
                        _winnerText.SetText(draw);
                    }

                    _winnerText.color = _winnerDefaultColor;
                }
                else
                {
                    string format = _winnerBinding.Value;
                    if (format != null)
                    {
                        // One format per match or language change (not per frame).
                        _winnerText.SetText(string.Format(format, GetName(_lastResult.Winner)));
                    }

                    CharacterDefinition winner = _characters.Get(_lastResult.Winner);
                    _winnerText.color = winner != null ? winner.Theme.Secondary : _winnerDefaultColor;
                }
            }

            SetName(_nameP1, _nameStripeP1, PlayerId.Player1);
            SetName(_nameP2, _nameStripeP2, PlayerId.Player2);
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

            string label = id == PlayerId.Player2 ? _player2Binding.Value : _player1Binding.Value;
            return label ?? string.Empty;
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
            if (_winnerEntry == null || _winnerEntry.IsEmpty || _drawEntry == null || _drawEntry.IsEmpty
                || _player1Label == null || _player1Label.IsEmpty || _player2Label == null || _player2Label.IsEmpty)
            {
                Debug.LogWarning($"[{nameof(MatchResultsView)}] Empty localized string on '{name}'.", this);
            }

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
