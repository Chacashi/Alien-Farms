using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AlienFarms.Input
{
    /// <summary>
    /// Reads gameplay input from this player's own <see cref="PlayerInput"/>.
    /// The serialized <see cref="InputActionReference"/>s only identify actions; the live actions are
    /// resolved by id on <see cref="PlayerInput.actions"/>, which is a per-player clone under PlayerInputManager.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    public sealed class PlayerInputHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("PlayerInput that owns this player's devices and action asset.")]
        private PlayerInput _playerInput;

        [Header("Actions (identifiers only, resolved on the PlayerInput clone)")]
        [SerializeField] private InputActionReference _move;
        [SerializeField] private InputActionReference _look;
        [SerializeField] private InputActionReference _sprint;
        [SerializeField] private InputActionReference _grab;
        [SerializeField, Tooltip("Pause button; held state is read by PlayerPauseInput (hold-to-pause).")]
        private InputActionReference _pause;

        [SerializeField] private InputActionReference _aim;

        [Header("Cursor")]
        [SerializeField, Tooltip("Lock and hide the cursor while this player uses keyboard and mouse.")]
        private bool _lockCursorForMouse = true;

        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _sprintAction;
        private InputAction _grabAction;
        private InputAction _pauseAction;
        private InputAction _aimAction;

        // Cursor.lockState is global: only the handler that locked it may release it.
        private bool _ownsCursorLock;
        private bool _cursorLockSuppressed;
        private bool _started;

        public event Action GrabStarted;
        public event Action GrabCanceled;

        public Vector2 Move => _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 Look => _lookAction != null ? _lookAction.ReadValue<Vector2>() : Vector2.zero;
        public bool IsSprintHeld => _sprintAction != null && _sprintAction.IsPressed();
        public bool IsGrabHeld => _grabAction != null && _grabAction.IsPressed();
        public bool IsAimHeld => _aimAction != null && _aimAction.IsPressed();
        public bool IsPauseHeld => _pauseAction != null && _pauseAction.IsPressed();

        /// <summary>True when Look is currently driven by a pointer delta (mouse) rather than a stick rate.</summary>
        public bool IsLookPointerDelta => _lookAction != null && _lookAction.activeControl?.device is Pointer;

        private void Awake()
        {
            if (_playerInput == null)
            {
                _playerInput = GetComponent<PlayerInput>();
            }

            _moveAction = Resolve(_move);
            _lookAction = Resolve(_look);
            _sprintAction = Resolve(_sprint);
            _grabAction = Resolve(_grab);
            _pauseAction = Resolve(_pause);
            _aimAction = Resolve(_aim);
        }

        private void OnEnable()
        {
            if (_grabAction != null)
            {
                _grabAction.started += HandleGrabStarted;
                _grabAction.canceled += HandleGrabCanceled;
            }

            if (_playerInput != null)
            {
                _playerInput.onControlsChanged += HandleControlsChanged;
            }

            // On first enable PlayerInput may not have paired devices yet; Start handles that case.
            if (_started)
            {
                RefreshCursorLock();
            }
        }

        private void Start()
        {
            // All OnEnable calls (including PlayerInput's device pairing) have run by now.
            _started = true;
            RefreshCursorLock();
        }

        private void OnDisable()
        {
            if (_grabAction != null)
            {
                _grabAction.started -= HandleGrabStarted;
                _grabAction.canceled -= HandleGrabCanceled;
            }

            if (_playerInput != null)
            {
                _playerInput.onControlsChanged -= HandleControlsChanged;
            }

            ReleaseCursorLock();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && _ownsCursorLock)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        private InputAction Resolve(InputActionReference reference)
        {
            if (reference == null || reference.action == null || _playerInput == null || _playerInput.actions == null)
            {
                return null;
            }

            InputAction action = _playerInput.actions.FindAction(reference.action.id);
            if (action == null)
            {
                Debug.LogError($"[{nameof(PlayerInputHandler)}] Action '{reference.action.name}' not found in '{_playerInput.actions.name}'.", this);
            }

            return action;
        }

        private void HandleGrabStarted(InputAction.CallbackContext context) => GrabStarted?.Invoke();

        private void HandleGrabCanceled(InputAction.CallbackContext context) => GrabCanceled?.Invoke();

        private void HandleControlsChanged(PlayerInput playerInput) => RefreshCursorLock();

        /// <summary>
        /// While suppressed (this player navigates a menu), the cursor is never locked so the mouse can point at buttons.
        /// </summary>
        public void SetCursorLockSuppressed(bool suppressed)
        {
            if (_cursorLockSuppressed == suppressed)
            {
                return;
            }

            _cursorLockSuppressed = suppressed;
            RefreshCursorLock();
        }

        private void RefreshCursorLock()
        {
            bool usesKeyboardMouse = _playerInput != null && _playerInput.currentControlScheme == InputSchemeNames.KeyboardMouse;

            if (usesKeyboardMouse && _lockCursorForMouse && !_cursorLockSuppressed)
            {
                _ownsCursorLock = true;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                ReleaseCursorLock();
            }
        }

        private void ReleaseCursorLock()
        {
            if (!_ownsCursorLock)
            {
                return;
            }

            _ownsCursorLock = false;
            Cursor.lockState = CursorLockMode.None;
        }

        private void OnValidate()
        {
            if (_playerInput == null)
            {
                Debug.LogWarning($"[{nameof(PlayerInputHandler)}] Missing PlayerInput reference on '{name}'.", this);
            }

            if (_move == null || _look == null || _sprint == null || _grab == null || _pause == null || _aim == null)
            {
                Debug.LogWarning($"[{nameof(PlayerInputHandler)}] One or more InputActionReferences are not assigned on '{name}'.", this);
            }
        }
    }
}
