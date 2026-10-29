using AlienFarms.Core;
using TMPro;
using UnityEngine;

namespace AlienFarms.UI
{
    /// <summary>
    /// Shows the round result (winner and score) while the match is in Results. Lives on an always-active object and
    /// toggles <see cref="_panelRoot"/>. The restart hint is static text in the scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchResultsView : MonoBehaviour
    {
        [Header("Texts")]
        [SerializeField] private string _player1WinsText = "¡Gana P1!";
        [SerializeField] private string _player2WinsText = "¡Gana P2!";
        [SerializeField] private string _drawText = "¡Empate!";

        [SerializeField, Tooltip("TMP format; {0} = Player 1 score, {1} = Player 2 score.")]
        private string _scoreFormat = "{0} - {1}";

        [Header("References")]
        [SerializeField, Tooltip("Results panel (text lives in the scene). Must not contain this component.")]
        private GameObject _panelRoot;

        [SerializeField] private TMP_Text _winnerText;
        [SerializeField] private TMP_Text _scoreText;

        private void OnEnable()
        {
            GameEvents.OnMatchEnded += HandleMatchEnded;
            GameEvents.OnMatchPhaseChanged += HandleMatchPhaseChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnMatchEnded -= HandleMatchEnded;
            GameEvents.OnMatchPhaseChanged -= HandleMatchPhaseChanged;
        }

        private void HandleMatchEnded(MatchResult result)
        {
            if (_winnerText != null)
            {
                string winner = result.IsDraw
                    ? _drawText
                    : result.Winner == PlayerId.Player1 ? _player1WinsText : _player2WinsText;
                _winnerText.SetText(winner);
            }

            if (_scoreText != null)
            {
                _scoreText.SetText(_scoreFormat, result.Player1Score, result.Player2Score);
            }

            SetVisible(true);
        }

        private void HandleMatchPhaseChanged(MatchPhase previous, MatchPhase current)
        {
            if (current != MatchPhase.Results)
            {
                SetVisible(false);
            }
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
