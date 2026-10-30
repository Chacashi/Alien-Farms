using System;
using AlienFarms.Core;
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
    [RequireComponent(typeof(PlayerIdentity))]
    public sealed class PlayerInputHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("PlayerInput that owns this player's devices and action asset.")]
        private PlayerInput _playerInput;

        [SerializeField, Tooltip("Identity used as payload for global events.")]
        private PlayerIdentity _identity;

        [Header("Actions (identifiers only, resolved on the PlayerInput clone)")]
        [SerializeField] private InputActionReference _move;
        [SerializeField] private InputActionReference _look;
        [SerializeField] private InputActionReference _sprint;
        [SerializeField] private InputActionReference _grab;
        [SerializeField] private InputActionReference _pause;
        [SerializeField] private InputActionReference _confirm;
        [SerializeField] private InputActionReference _aim;

        [Header("Cursor")]
        [SerializeField, Tooltip("Lock and hide the cursor while this player uses keyboard and mouse.")]
        private bool _lockCursorForMouse = true;

        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _sprintAction;
        private InputAction _grabAction;
        private InputAction _pauseAction;
        private InputAction _confirmAction;
        private InputAction _aimAction;

        // Cursor.lockState is global: only the handler that locked it may release it.
        private bool _ownsCursorLock;
        private bool _started;

        public event Action GrabStarted;
        public event Action GrabCanceled;

        public Vector2 Move => _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 Look => _lookAction != null ? _lookAction.ReadValue<Vector2>() : Vector2.zero;
        public bool IsSprintHeld => _sprintAction != null && _sprintAction.IsPressed();
        public bool IsGrabHeld => _grabAction != null && _grabAction.IsPressed();
        public bool IsAimHeld => _aimAction != null && _aimAction.IsPressed();

        /// <summary>True when Look is currently driven by a pointer delta (mouse) rather than a stick rate.</summary>
        public bool IsLookPointerDelta => _lookAction != null && _lookAction.activeControl?.device is Pointer;

        private void Awake()
        {
            if (_playerInput == null)
            {
                _playerInput = GetComponent<PlayerInput>();
            }

            if (_identity == null)
            {
                Debug.LogError($"[{nameof(PlayerInputHandler)}] PlayerIdentity is not assigned on '{name}'; pause/confirm requests will be ignored.", this);
            }

            _moveAction = Resolve(_move);
            _lookAction = Resolve(_look);
            _sprintAction = Resolve(_sprint);
            _grabAction = Resolve(_grab);
            _pauseAction = Resolve(_pause);
            _confirmAction = Resolve(_confirm);
            _aimAction = Resolve(_aim);
        }

        private void OnEnable()
        {
            if (_grabAction != null)
            {
                _grabAction.started += HandleGrabStarted;
                _grabAction.canceled += HandleGrabCanceled;
            }

            if (_pauseAction != null)
            {
                _pauseAction.performed += HandlePausePerformed;
            }

            if (_confirmAction != null)
            {
                _confirmAction.performed += HandleConfirmPerformed;
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

            if (_pauseAction != null)
            {
                _pauseAction.performed -= HandlePausePerformed;
            }

            if (_confirmAction != null)
            {
                _confirmAction.performed -= HandleConfirmPerformed;
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

        private void HandlePausePerformed(InputAction.CallbackContext context)
        {
            if (_identity != null)
            {
                GameEvents.RaisePauseRequested(_identity.Id);
            }
        }

        private void HandleConfirmPerformed(InputAction.CallbackContext context)
        {
            if (_identity != null)
            {
                GameEvents.RaiseConfirmPressed(_identity.Id);
            }
        }

        private void HandleControlsChanged(PlayerInput playerInput) => RefreshCursorLock();

        private void RefreshCursorLock()
        {
            bool usesKeyboardMouse = _playerInput != null && _playerInput.currentControlScheme == InputSchemeNames.KeyboardMouse;

            if (usesKeyboardMouse && _lockCursorForMouse)
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

            if (_identity == null)
            {
                Debug.LogWarning($"[{nameof(PlayerInputHandler)}] Missing PlayerIdentity reference on '{name}'.", this);
            }

            if (_move == null || _look == null || _sprint == null || _grab == null || _pause == null || _confirm == null || _aim == null)
            {
                Debug.LogWarning($"[{nameof(PlayerInputHandler)}] One or more InputActionReferences are not assigned on '{name}'.", this);
            }
        }
    }
}
