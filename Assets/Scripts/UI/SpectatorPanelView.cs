using UnityEngine;
using UnityEngine.EventSystems;

namespace AlienFarms.UI
{
    /// <summary>
    /// Online spectator overlay: "You are watching" and a Leave button (its OnClick calls MatchExit.ReturnToTitle in the
    /// scene). Texts are static LocalizedText in the scene. Pure view toggled by the SpectatorModeController; lives on an
    /// always-active object and toggles <see cref="_root"/>. On show it focuses Leave in the spectator's event system so
    /// a gamepad can press it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpectatorPanelView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Panel root (UI_SpectatorPanel's content). Must not contain this component.")]
        private GameObject _root;

        [SerializeField, Tooltip("BTN_Leave: focused when the panel shows.")]
        private GameObject _firstSelected;

        [SerializeField, Tooltip("Event system of the spectator rig (optional: without it nothing is focused).")]
        private EventSystem _eventSystem;

        /// <summary>True while the panel is shown.</summary>
        public bool IsShown => _root != null && _root.activeSelf;

        public void Show()
        {
            SetActive(true);
            if (_eventSystem != null && _firstSelected != null && _eventSystem.isActiveAndEnabled)
            {
                _eventSystem.SetSelectedGameObject(_firstSelected);
            }
        }

        public void Hide()
        {
            SetActive(false);
        }

        private void SetActive(bool active)
        {
            if (_root != null && _root.activeSelf != active)
            {
                _root.SetActive(active);
            }
        }

        private void OnValidate()
        {
            if (_root == null)
            {
                Debug.LogWarning($"[{nameof(SpectatorPanelView)}] Missing panel root on '{name}'.", this);
            }
            else if (_root == gameObject || transform.IsChildOf(_root.transform))
            {
                Debug.LogWarning($"[{nameof(SpectatorPanelView)}] Panel root on '{name}' contains this view: it could never show again.", this);
            }

            if (_firstSelected == null)
            {
                Debug.LogWarning($"[{nameof(SpectatorPanelView)}] Missing first selected (BTN_Leave) on '{name}'.", this);
            }
        }
    }
}
