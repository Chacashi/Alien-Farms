using UnityEngine;

namespace AlienFarms.UI
{
    /// <summary>
    /// A sub-menu opened from a parent menu (Options, How to play...). Lives on the panel root itself: showing or hiding
    /// it toggles this GameObject. The parent controller scopes the owner's event system to <see cref="MenuRoot"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SubMenuPanel : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField, Tooltip("Selectable focused when the panel opens (child of this panel).")]
        private GameObject _firstSelected;

        [SerializeField, Tooltip("Button of the parent menu focused again when the panel closes (the one that opened it). Optional.")]
        private GameObject _returnSelection;

        /// <summary>Root the owner's event system is scoped to while the panel is open.</summary>
        public GameObject MenuRoot => gameObject;

        public GameObject FirstSelected => _firstSelected;

        /// <summary>Parent menu selectable to focus on close (null = the parent's default).</summary>
        public GameObject ReturnSelection => _returnSelection;

        public void Show()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnValidate()
        {
            if (_firstSelected == null)
            {
                Debug.LogWarning($"[{nameof(SubMenuPanel)}] Missing first selected on '{name}'.", this);
            }
            else if (!_firstSelected.transform.IsChildOf(transform))
            {
                Debug.LogWarning($"[{nameof(SubMenuPanel)}] First selected on '{name}' is not under this panel.", this);
            }

            if (_returnSelection != null && _returnSelection.transform.IsChildOf(transform))
            {
                Debug.LogWarning($"[{nameof(SubMenuPanel)}] Return selection on '{name}' is inside this panel; it must be a parent menu button.", this);
            }
        }
    }
}
