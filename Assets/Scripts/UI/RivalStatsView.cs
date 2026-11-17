using AlienFarms.Core;
using AlienFarms.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace AlienFarms.UI
{
    /// <summary>
    /// The rival's public stats in the online room ("Rival: {0} matches · {1} W · {2} D · {3} L"), read from the
    /// <see cref="PlayerProgressChannel"/>. Hidden while the channel has no rival stats. Redraws only when the channel
    /// changes (or the format resolves); TMP formats the numbers without allocating.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RivalStatsView : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Runtime progress channel (PlayerProgressChannel.asset).")]
        private PlayerProgressChannel _progress;

        [SerializeField, Tooltip("TMP format (non-Smart); {0} = matches, {1} = wins, {2} = draws, {3} = losses.")]
        private LocalizedString _format = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.StatsRival);

        [Header("References")]
        [SerializeField, Tooltip("Root toggled on and off. Must not contain this component.")]
        private GameObject _root;

        [SerializeField, Tooltip("Rival stats text (no LocalizedText on it).")]
        private TMP_Text _label;

        private LocalizedStringBinding _formatBinding;

        private void Awake()
        {
            _formatBinding = new LocalizedStringBinding(_format, Redraw);
        }

        private void OnEnable()
        {
            _formatBinding.Enable();
            if (_progress != null)
            {
                _progress.Changed += Redraw;
            }

            Redraw();
        }

        private void OnDisable()
        {
            _formatBinding.Disable();
            if (_progress != null)
            {
                _progress.Changed -= Redraw;
            }
        }

        private void Redraw()
        {
            string format = _formatBinding?.Value;
            bool visible = _progress != null && _progress.HasRivalStats && format != null;
            if (visible && _label != null)
            {
                PlayerStatsSummary stats = _progress.RivalStats;
                _label.SetText(format, stats.MatchesPlayed, stats.Wins, stats.Draws, stats.Losses);
            }

            if (_root != null && _root.activeSelf != visible)
            {
                _root.SetActive(visible);
            }
        }

        private void OnValidate()
        {
            if (_format == null || _format.IsEmpty)
            {
                Debug.LogWarning($"[{nameof(RivalStatsView)}] Empty localized format on '{name}'.", this);
            }

            if (_progress == null)
            {
                Debug.LogWarning($"[{nameof(RivalStatsView)}] Missing PlayerProgressChannel on '{name}'.", this);
            }

            if (_root == null || _label == null)
            {
                Debug.LogWarning($"[{nameof(RivalStatsView)}] Missing root or label on '{name}'.", this);
            }
            else if (_root == gameObject || transform.IsChildOf(_root.transform))
            {
                Debug.LogWarning($"[{nameof(RivalStatsView)}] Root on '{name}' contains this view; hiding it would stop listening to the channel.", this);
            }
        }
    }
}
