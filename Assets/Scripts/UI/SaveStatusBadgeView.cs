using AlienFarms.Core;
using AlienFarms.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace AlienFarms.UI
{
    /// <summary>
    /// Cloud save badge on the results panel: "Saving… / Progress saved / Could not save (will retry)". Visible only in
    /// online matches and while the <see cref="PlayerProgressChannel"/> reports a save status other than Idle.
    /// Redraws only when the channel changes (or a string resolves), never per frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SaveStatusBadgeView : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Runtime progress channel (PlayerProgressChannel.asset).")]
        private PlayerProgressChannel _progress;

        [SerializeField, Tooltip("Runtime match session (MatchSession.asset); the badge only shows in online matches.")]
        private MatchSession _session;

        [SerializeField, Tooltip("Text while the save is in flight.")]
        private LocalizedString _saving = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.StatsSaving);

        [SerializeField, Tooltip("Text once the cloud confirmed the save.")]
        private LocalizedString _saved = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.StatsSaved);

        [SerializeField, Tooltip("Text after the save failed (it is retried).")]
        private LocalizedString _failed = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.StatsSaveFailed);

        [Header("References")]
        [SerializeField, Tooltip("Badge root toggled on and off. Must not contain this component.")]
        private GameObject _root;

        [SerializeField, Tooltip("Badge text (no LocalizedText on it).")]
        private TMP_Text _label;

        private LocalizedStringBinding _savingBinding;
        private LocalizedStringBinding _savedBinding;
        private LocalizedStringBinding _failedBinding;

        private void Awake()
        {
            _savingBinding = new LocalizedStringBinding(_saving, Redraw);
            _savedBinding = new LocalizedStringBinding(_saved, Redraw);
            _failedBinding = new LocalizedStringBinding(_failed, Redraw);
        }

        private void OnEnable()
        {
            _savingBinding.Enable();
            _savedBinding.Enable();
            _failedBinding.Enable();
            if (_progress != null)
            {
                _progress.Changed += Redraw;
            }

            Redraw();
        }

        private void OnDisable()
        {
            _savingBinding.Disable();
            _savedBinding.Disable();
            _failedBinding.Disable();
            if (_progress != null)
            {
                _progress.Changed -= Redraw;
            }
        }

        private void Redraw()
        {
            bool online = _session != null && _session.Mode == GameMode.Online;
            PlayerProgressSaveStatus status = _progress != null ? _progress.SaveStatus : PlayerProgressSaveStatus.Idle;
            bool visible = online && status != PlayerProgressSaveStatus.Idle;

            if (visible && _label != null)
            {
                string text = GetText(status);
                if (text != null)
                {
                    _label.SetText(text);
                }
            }

            if (_root != null && _root.activeSelf != visible)
            {
                _root.SetActive(visible);
            }
        }

        private string GetText(PlayerProgressSaveStatus status)
        {
            switch (status)
            {
                case PlayerProgressSaveStatus.Saving:
                    return _savingBinding.Value;
                case PlayerProgressSaveStatus.Saved:
                    return _savedBinding.Value;
                case PlayerProgressSaveStatus.Failed:
                    return _failedBinding.Value;
                default:
                    return null;
            }
        }

        private void OnValidate()
        {
            if (_saving == null || _saving.IsEmpty || _saved == null || _saved.IsEmpty || _failed == null || _failed.IsEmpty)
            {
                Debug.LogWarning($"[{nameof(SaveStatusBadgeView)}] Empty localized string on '{name}'.", this);
            }

            if (_progress == null || _session == null)
            {
                Debug.LogWarning($"[{nameof(SaveStatusBadgeView)}] Missing PlayerProgressChannel or MatchSession on '{name}'.", this);
            }

            if (_root == null || _label == null)
            {
                Debug.LogWarning($"[{nameof(SaveStatusBadgeView)}] Missing root or label on '{name}'.", this);
            }
            else if (_root == gameObject || transform.IsChildOf(_root.transform))
            {
                Debug.LogWarning($"[{nameof(SaveStatusBadgeView)}] Root on '{name}' contains this view; hiding it would stop listening to the channel.", this);
            }
        }
    }
}
