using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AlienFarms.UI
{
    /// <summary>
    /// Focus feedback for a menu button: shows a focus frame and scales up while selected. Hovering with the mouse
    /// selects the button (if interactable), so mouse and gamepad/keyboard share one focus. Animates with unscaled time
    /// so it works while the game is paused.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SelectionHighlight : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
    {
        private const float ScaleEpsilon = 0.0005f;

        [Header("Config")]
        [SerializeField, Min(1f), Tooltip("Scale while focused.")]
        private float _selectedScale = 1.08f;

        [SerializeField, Min(0.01f), Tooltip("Scale change per second towards the target (unscaled).")]
        private float _scaleSpeed = 1f;

        [Header("References")]
        [SerializeField, Tooltip("Child frame shown while focused (inactive by default).")]
        private GameObject _focusFrame;

        [SerializeField, Tooltip("Transform scaled while focused (defaults to this one).")]
        private RectTransform _target;

        private Selectable _selectable;
        private bool _isSelected;
        private float _currentScale = 1f;

        private void Awake()
        {
            if (_target == null)
            {
                _target = (RectTransform)transform;
            }

            TryGetComponent(out _selectable);
        }

        private void OnDisable()
        {
            // A panel hidden while focused must come back in its rest state.
            _isSelected = false;
            _currentScale = 1f;
            ApplyScale();
            RefreshFrame();
        }

        private void Update()
        {
            float target = _isSelected ? _selectedScale : 1f;
            if (Mathf.Abs(_currentScale - target) < ScaleEpsilon)
            {
                return;
            }

            _currentScale = Mathf.MoveTowards(_currentScale, target, _scaleSpeed * Time.unscaledDeltaTime);
            ApplyScale();
        }

        public void OnSelect(BaseEventData eventData)
        {
            _isSelected = true;
            RefreshFrame();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _isSelected = false;
            RefreshFrame();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Selected through the event system that sent the hover (the pointing player's own one).
            if (_selectable == null || _selectable.IsInteractable())
            {
                eventData.selectedObject = gameObject;
            }
        }

        private void ApplyScale()
        {
            if (_target != null)
            {
                _target.localScale = new Vector3(_currentScale, _currentScale, 1f);
            }
        }

        private void RefreshFrame()
        {
            if (_focusFrame != null && _focusFrame.activeSelf != _isSelected)
            {
                _focusFrame.SetActive(_isSelected);
            }
        }

        private void OnValidate()
        {
            if (_focusFrame == null)
            {
                Debug.LogWarning($"[{nameof(SelectionHighlight)}] Missing focus frame on '{name}'.", this);
            }
            else if (_focusFrame == gameObject)
            {
                Debug.LogWarning($"[{nameof(SelectionHighlight)}] Focus frame on '{name}' must be a child, not the button itself.", this);
            }
        }
    }
}
