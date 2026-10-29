using AlienFarms.Core;
using AlienFarms.Match;
using TMPro;
using UnityEngine;

namespace AlienFarms.UI
{
    /// <summary>Shows the remaining round time as "m:ss". Updated only when the whole second changes (no per-frame work).</summary>
    [DisallowMultipleComponent]
    public sealed class MatchTimerView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Text that displays the remaining time.")]
        private TMP_Text _text;

        private readonly char[] _buffer = new char[8];

        private void OnEnable()
        {
            GameEvents.OnMatchTimeChanged += HandleMatchTimeChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnMatchTimeChanged -= HandleMatchTimeChanged;
        }

        private void HandleMatchTimeChanged(int secondsLeft)
        {
            if (_text == null)
            {
                return;
            }

            int length = TimeFormat.WriteMinutesSeconds(secondsLeft, _buffer);
            _text.SetText(_buffer, 0, length);
        }

        private void OnValidate()
        {
            if (_text == null)
            {
                Debug.LogWarning($"[{nameof(MatchTimerView)}] Missing text on '{name}'.", this);
            }
        }
    }
}
