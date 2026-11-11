using System;
using AlienFarms.Core;
using AlienFarms.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace AlienFarms.UI
{
    /// <summary>
    /// Lifetime stats on the title screen, read from the <see cref="PlayerProgressChannel"/>. Shows a dash and a hint
    /// until stats are loaded (the cloud save service writes them from task 2.8). Redraws only when the channel changes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TitleStatsView : MonoBehaviour
    {
        /// <summary>Number of value texts: matches, wins, draws, losses, animals scored, tackles landed.</summary>
        public const int ValueCount = 6;

        private const string Placeholder = "—";

        [Header("Config")]
        [SerializeField, Tooltip("Runtime progress channel (PlayerProgressChannel.asset).")]
        private PlayerProgressChannel _progress;

        [SerializeField, Tooltip("Hint shown while there are no stats.")]
        private LocalizedString _emptyHint = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.StatsEmptyHint);

        [Header("References")]
        [SerializeField, Tooltip("Value texts in order: matches, wins, draws, losses, animals scored, tackles landed.")]
        private TMP_Text[] _values = new TMP_Text[ValueCount];

        [SerializeField, Tooltip("Optional: shown while there are no stats (e.g. the hint row).")]
        private GameObject _placeholderRoot;

        [SerializeField, Tooltip("Optional: hint text written from the empty hint entry (no LocalizedText on it).")]
        private TMP_Text _hint;

        [SerializeField, Tooltip("Optional: badge shown after the last save succeeded.")]
        private GameObject _savedBadge;

        private LocalizedStringBinding _hintBinding;

        private void Awake()
        {
            _hintBinding = new LocalizedStringBinding(_emptyHint, RefreshHint);
        }

        private void OnEnable()
        {
            _hintBinding.Enable();
            if (_progress != null)
            {
                _progress.Changed += Redraw;
            }

            Redraw();
        }

        private void OnDisable()
        {
            _hintBinding.Disable();
            if (_progress != null)
            {
                _progress.Changed -= Redraw;
            }
        }

        // On channel change only (rare): int formatting allocates once per change, never per frame.
        private void Redraw()
        {
            bool hasStats = _progress != null && _progress.HasStats;
            PlayerStatsSummary stats = hasStats ? _progress.Stats : default;
            SetValue(0, hasStats, stats.MatchesPlayed);
            SetValue(1, hasStats, stats.Wins);
            SetValue(2, hasStats, stats.Draws);
            SetValue(3, hasStats, stats.Losses);
            SetValue(4, hasStats, stats.AnimalsScored);
            SetValue(5, hasStats, stats.TacklesLanded);

            SetActive(_placeholderRoot, !hasStats);
            if (_hint != null && _hint.gameObject != _placeholderRoot)
            {
                SetActive(_hint.gameObject, !hasStats);
            }

            SetActive(_savedBadge, _progress != null && _progress.SaveStatus == PlayerProgressSaveStatus.Saved);
        }

        private void SetValue(int index, bool hasStats, int value)
        {
            if (_values == null || index >= _values.Length || _values[index] == null)
            {
                return;
            }

            if (hasStats)
            {
                // TMP formats the number without allocating a string.
                _values[index].SetText("{0}", value);
            }
            else
            {
                _values[index].SetText(Placeholder);
            }
        }

        private void RefreshHint()
        {
            string text = _hintBinding?.Value;
            if (_hint != null && text != null)
            {
                _hint.SetText(text);
            }
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
            if (_emptyHint == null || _emptyHint.IsEmpty)
            {
                Debug.LogWarning($"[{nameof(TitleStatsView)}] Empty localized hint on '{name}'.", this);
            }

            if (_progress == null)
            {
                Debug.LogWarning($"[{nameof(TitleStatsView)}] Missing PlayerProgressChannel on '{name}'.", this);
            }

            if (_values == null || _values.Length != ValueCount || Array.IndexOf(_values, null) >= 0)
            {
                Debug.LogWarning($"[{nameof(TitleStatsView)}] '{name}' needs {ValueCount} value texts (matches, wins, draws, losses, animals, tackles).", this);
            }
        }
    }
}
