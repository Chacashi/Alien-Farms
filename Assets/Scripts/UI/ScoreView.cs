using AlienFarms.Core;
using TMPro;
using UnityEngine;

namespace AlienFarms.UI
{
    /// <summary>Shows one player's score. Updated only on score changes.</summary>
    [DisallowMultipleComponent]
    public sealed class ScoreView : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Player whose score this view shows.")]
        private PlayerId _player = PlayerId.Player1;

        [SerializeField, Tooltip("TMP format; {0} is the score.")]
        private string _format = "P1  {0}";

        [Header("References")]
        [SerializeField, Tooltip("Text that displays the score.")]
        private TMP_Text _text;

        private void OnEnable()
        {
            GameEvents.OnScoreChanged += HandleScoreChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnScoreChanged -= HandleScoreChanged;
        }

        private void HandleScoreChanged(PlayerId playerId, int score)
        {
            if (playerId != _player || _text == null)
            {
                return;
            }

            _text.SetText(_format, score);
        }

        private void OnValidate()
        {
            if (_player == PlayerId.None)
            {
                Debug.LogWarning($"[{nameof(ScoreView)}] Player is None on '{name}'.", this);
            }

            if (_text == null)
            {
                Debug.LogWarning($"[{nameof(ScoreView)}] Missing text on '{name}'.", this);
            }
        }
    }
}
