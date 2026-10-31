using UnityEngine;

namespace AlienFarms.UI
{
    /// <summary>
    /// Title screen panel: shows the "press to start" prompt until the first player joins, then the menu buttons.
    /// Lives on an always-active object and toggles <see cref="_panelRoot"/>. Buttons are wired in the scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TitleScreenView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Title panel (logo, prompt, buttons). Also the menu root the owner's event system is scoped to. Must not contain this component.")]
        private GameObject _panelRoot;

        [SerializeField, Tooltip("'Press X / Enter to start' prompt, shown while nobody has joined.")]
        private GameObject _promptRoot;

        [SerializeField, Tooltip("Buttons container, shown once a player has joined.")]
        private GameObject _buttonsRoot;

        [SerializeField, Tooltip("Button focused when the title menu opens (BTN_Play).")]
        private GameObject _firstSelected;

        /// <summary>Root the title menu owner navigates under.</summary>
        public GameObject MenuRoot => _panelRoot;

        public GameObject FirstSelected => _firstSelected;

        public void Show() => SetActive(_panelRoot, true);

        public void Hide() => SetActive(_panelRoot, false);

        public void SetPlayerJoined(bool joined)
        {
            SetActive(_promptRoot, !joined);
            SetActive(_buttonsRoot, joined);
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
            if (_panelRoot == null || _promptRoot == null || _buttonsRoot == null || _firstSelected == null)
            {
                Debug.LogWarning($"[{nameof(TitleScreenView)}] Missing panel, prompt, buttons or first selected reference on '{name}'.", this);
                return;
            }

            if (_panelRoot == gameObject || transform.IsChildOf(_panelRoot.transform))
            {
                Debug.LogWarning($"[{nameof(TitleScreenView)}] Panel root on '{name}' contains this view.", this);
            }

            if (!_firstSelected.transform.IsChildOf(_buttonsRoot.transform))
            {
                Debug.LogWarning($"[{nameof(TitleScreenView)}] First selected button on '{name}' is not under the buttons root.", this);
            }
        }
    }
}
