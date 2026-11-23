using UnityEngine;

namespace AlienFarms.UI
{
    /// <summary>
    /// Applies title group visibility and input settings from the intro director.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TitleScreenView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Title panel (logo, prompt, buttons). Also the menu root the owner's event system is scoped to. Must not contain this component.")]
        private GameObject _panelRoot;

        [SerializeField, Tooltip("Attract root with the centered logo and prompt. Always non-interactable.")]
        private CanvasGroup _attractGroup;

        [SerializeField, Tooltip("Prompt CanvasGroup under Attract. Its alpha pulses.")]
        private CanvasGroup _promptGroup;

        [SerializeField, Tooltip("Menu root with the logo and buttons in the right column.")]
        private CanvasGroup _menuGroup;

        [SerializeField, Tooltip("Button focused when the title menu opens (BTN_Play).")]
        private GameObject _firstSelected;

        /// <summary>Root the title menu owner navigates under.</summary>
        public GameObject MenuRoot => _panelRoot;

        public GameObject FirstSelected => _firstSelected;

        public void Show() => SetActive(_panelRoot, true);

        public void Hide() => SetActive(_panelRoot, false);

        private void Awake()
        {
            SetInteractable(_attractGroup, false);
        }

        public void SetAttractAlpha(float alpha) => SetAlpha(_attractGroup, alpha);

        public void SetPromptAlpha(float alpha) => SetAlpha(_promptGroup, alpha);

        public void SetMenuAlpha(float alpha) => SetAlpha(_menuGroup, alpha);

        public void SetMenuInteractable(bool interactable) => SetInteractable(_menuGroup, interactable);

        private static void SetAlpha(CanvasGroup group, float alpha)
        {
            if (group != null && group.alpha != alpha)
            {
                group.alpha = alpha;
            }
        }

        private static void SetInteractable(CanvasGroup group, bool interactable)
        {
            if (group == null)
            {
                return;
            }

            if (group.interactable != interactable)
            {
                group.interactable = interactable;
            }

            if (group.blocksRaycasts != interactable)
            {
                group.blocksRaycasts = interactable;
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
            if (_panelRoot == null || _attractGroup == null || _promptGroup == null
                || _menuGroup == null || _firstSelected == null)
            {
                Debug.LogWarning($"[{nameof(TitleScreenView)}] Assign the panel, three canvas groups and first selected button on '{name}'.", this);
                return;
            }

            if (_panelRoot == gameObject || transform.IsChildOf(_panelRoot.transform))
            {
                Debug.LogWarning($"[{nameof(TitleScreenView)}] Panel root on '{name}' contains this view.", this);
            }

            if (!_firstSelected.transform.IsChildOf(_menuGroup.transform))
            {
                Debug.LogWarning($"[{nameof(TitleScreenView)}] Place the first selected button under the menu group on '{name}'.", this);
            }

            if (_promptGroup == _attractGroup || !_promptGroup.transform.IsChildOf(_attractGroup.transform))
            {
                Debug.LogWarning($"[{nameof(TitleScreenView)}] Place the prompt group below the attract group on '{name}'.", this);
            }

            if (_attractGroup.gameObject == _panelRoot || _menuGroup.gameObject == _panelRoot
                || !_attractGroup.transform.IsChildOf(_panelRoot.transform)
                || !_menuGroup.transform.IsChildOf(_panelRoot.transform))
            {
                Debug.LogWarning($"[{nameof(TitleScreenView)}] Place the attract and menu groups below the panel root on '{name}'.", this);
            }
        }
    }
}
