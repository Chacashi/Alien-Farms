using System;
using AlienFarms.Core;
using AlienFarms.Data;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace AlienFarms.Input
{
    /// <summary>
    /// Per-player menu navigation. <see cref="EnterMenu"/> switches this player's <see cref="PlayerInput"/> to the UI map
    /// and scopes its own <see cref="MultiplayerEventSystem"/> to a menu root; <see cref="ExitMenu"/> restores gameplay.
    /// <see cref="SetBlocked"/> deactivates all of this player's input (used for the player who does not own a menu).
    /// Also exposes discrete navigation steps and Submit/Cancel for menus driven by code instead of Selectables.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    public sealed class PlayerMenuInput : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Navigation step threshold.")]
        private MenuSettings _settings;

        [Header("References")]
        [SerializeField, Tooltip("PlayerInput that owns this player's devices and action asset.")]
        private PlayerInput _playerInput;

        [SerializeField, Tooltip("Identity used as payload for global events.")]
        private PlayerIdentity _identity;

        [SerializeField, Tooltip("This player's own event system (child UIInput). Enabled only while this player navigates a menu.")]
        private MultiplayerEventSystem _eventSystem;

        [SerializeField, Tooltip("Optional: gameplay input handler, so the cursor is unlocked while in a menu.")]
        private PlayerInputHandler _gameplayInput;

        [Header("UI actions (identifiers only, resolved on the PlayerInput clone)")]
        [SerializeField] private InputActionReference _navigate;
        [SerializeField] private InputActionReference _submit;
        [SerializeField] private InputActionReference _cancel;

        private readonly AxisStepDetector _horizontal = new AxisStepDetector();
        private readonly AxisStepDetector _vertical = new AxisStepDetector();
        private readonly DeferredDisableGate _eventSystemGate = new DeferredDisableGate();
        private InputAction _navigateAction;
        private InputAction _submitAction;
        private InputAction _cancelAction;
        private GameObject _pendingSelection;
        private bool _isInMenu;
        private bool _isBlocked;

        // Submit/Cancel are latched in the input callbacks and dispatched in Update: listeners may switch the action
        // map, which must never happen while the Input System is still iterating that action's callbacks.
        private bool _submitQueued;
        private bool _cancelQueued;

        /// <summary>Discrete horizontal navigation step (-1 left, +1 right) while in a menu.</summary>
        public event Action<int> HorizontalStep;

        /// <summary>Discrete vertical navigation step (-1 down, +1 up) while in a menu.</summary>
        public event Action<int> VerticalStep;

        public event Action Submitted;
        public event Action Canceled;

        public PlayerId Id => _identity != null ? _identity.Id : PlayerId.None;
        public bool IsInMenu => _isInMenu;
        public bool IsBlocked => _isBlocked;

        private void Awake()
        {
            if (_playerInput == null)
            {
                _playerInput = GetComponent<PlayerInput>();
            }

            _navigateAction = Resolve(_navigate);
            _submitAction = Resolve(_submit);
            _cancelAction = Resolve(_cancel);
            RefreshEventSystem();
            ApplyPendingEventSystemDisable();
        }

        private void OnEnable()
        {
            if (_submitAction != null)
            {
                _submitAction.performed += HandleSubmitPerformed;
            }

            if (_cancelAction != null)
            {
                _cancelAction.performed += HandleCancelPerformed;
            }

            if (_playerInput != null)
            {
                _playerInput.onDeviceLost += HandleDeviceLost;
                _playerInput.onDeviceRegained += HandleDeviceRegained;
            }
        }

        private void OnDisable()
        {
            if (_submitAction != null)
            {
                _submitAction.performed -= HandleSubmitPerformed;
            }

            if (_cancelAction != null)
            {
                _cancelAction.performed -= HandleCancelPerformed;
            }

            if (_playerInput != null)
            {
                _playerInput.onDeviceLost -= HandleDeviceLost;
                _playerInput.onDeviceRegained -= HandleDeviceRegained;
            }

            ClearQueuedCommands();
            ApplyPendingEventSystemDisable();
        }

        private void Update()
        {
            DispatchQueuedCommands();
            if (!_isInMenu || _isBlocked)
            {
                return;
            }

            ApplyPendingSelection();
            if (_navigateAction == null)
            {
                return;
            }

            float threshold = _settings != null ? _settings.NavigateStepThreshold : 0.5f;
            Vector2 value = _navigateAction.ReadValue<Vector2>();

            int horizontal = _horizontal.Update(value.x, threshold);
            if (horizontal != 0)
            {
                HorizontalStep?.Invoke(horizontal);
            }

            int vertical = _vertical.Update(value.y, threshold);
            if (vertical != 0)
            {
                VerticalStep?.Invoke(vertical);
            }
        }

        private void LateUpdate()
        {
            // Deferred so a menu closed from a UI click (inside MultiplayerEventSystem.Update) never disables
            // the event system that is currently updating.
            ApplyPendingEventSystemDisable();
        }

        /// <summary>
        /// Gives this player a menu: UI action map, event system scoped to <paramref name="root"/> and
        /// <paramref name="firstSelected"/> focused (may be null for code-driven menus). Unblocks the player if needed.
        /// </summary>
        public void EnterMenu(GameObject root, GameObject firstSelected)
        {
            if (_playerInput == null)
            {
                return;
            }

            if (_isBlocked)
            {
                _isBlocked = false;
                _playerInput.ActivateInput();
            }

            _isInMenu = true;
            ClearQueuedCommands();
            _horizontal.Reset();
            _vertical.Reset();
            InputActionMap currentMap = _playerInput.currentActionMap;
            if (currentMap == null || currentMap.name != InputMapNames.UI)
            {
                _playerInput.SwitchCurrentActionMap(InputMapNames.UI);
            }

            if (_gameplayInput != null)
            {
                _gameplayInput.SetCursorLockSuppressed(true);
            }

            RefreshEventSystem();
            if (_eventSystem != null)
            {
                _eventSystem.playerRoot = root;
                _eventSystem.SetSelectedGameObject(null);
            }

            // Selected once it is active: an inactive Selectable never receives OnSelect (no highlight).
            _pendingSelection = firstSelected;
            ApplyPendingSelection();
        }

        /// <summary>Leaves the menu: back to the Player action map, selection cleared, cursor lock restored.</summary>
        public void ExitMenu()
        {
            if (!_isInMenu)
            {
                return;
            }

            _isInMenu = false;
            ClearQueuedCommands();
            _pendingSelection = null;
            if (_eventSystem != null)
            {
                _eventSystem.SetSelectedGameObject(null);
                _eventSystem.playerRoot = null;
            }

            RefreshEventSystem();

            if (_playerInput != null)
            {
                _playerInput.SwitchCurrentActionMap(InputMapNames.Player);
            }

            if (_gameplayInput != null)
            {
                _gameplayInput.SetCursorLockSuppressed(false);
            }
        }

        /// <summary>Blocks (deactivates) or restores all of this player's input, e.g. while the other player owns a menu.</summary>
        public void SetBlocked(bool blocked)
        {
            if (_isBlocked == blocked || _playerInput == null)
            {
                return;
            }

            _isBlocked = blocked;
            if (blocked)
            {
                _playerInput.DeactivateInput();
            }
            else
            {
                _playerInput.ActivateInput();
            }

            RefreshEventSystem();
        }

        private void HandleSubmitPerformed(InputAction.CallbackContext context)
        {
            if (_isInMenu && !_isBlocked)
            {
                _submitQueued = true;
            }
        }

        private void HandleCancelPerformed(InputAction.CallbackContext context)
        {
            // The UI input module keeps its actions enabled during gameplay too: only report Cancel inside a menu.
            if (_isInMenu && !_isBlocked)
            {
                _cancelQueued = true;
            }
        }

        /// <summary>Raises latched Submit/Cancel outside any input callback, if this player is still in a menu.</summary>
        private void DispatchQueuedCommands()
        {
            bool submit = _submitQueued;
            bool cancel = _cancelQueued;
            ClearQueuedCommands();
            if (!_isInMenu || _isBlocked)
            {
                return;
            }

            if (submit)
            {
                Submitted?.Invoke();
            }

            // A Submit listener may already have closed the menu.
            if (cancel && _isInMenu && !_isBlocked)
            {
                Canceled?.Invoke();
                GameEvents.RaiseMenuCancelPressed(Id);
            }
        }

        private void ClearQueuedCommands()
        {
            _submitQueued = false;
            _cancelQueued = false;
        }

        private void ApplyPendingSelection()
        {
            if (_pendingSelection == null || !_pendingSelection.activeInHierarchy || _eventSystem == null || !_eventSystem.enabled)
            {
                return;
            }

            _eventSystem.SetSelectedGameObject(_pendingSelection);
            _pendingSelection = null;
        }

        private void HandleDeviceLost(PlayerInput playerInput) => GameEvents.RaisePlayerDeviceLost(Id);

        private void HandleDeviceRegained(PlayerInput playerInput) => GameEvents.RaisePlayerDeviceRegained(Id);

        /// <summary>
        /// The event system only runs while this player navigates a menu, so a blocked or playing player's mouse
        /// can never click another player's menu. Enabling is immediate; disabling is deferred to LateUpdate.
        /// </summary>
        private void RefreshEventSystem()
        {
            if (_eventSystem == null)
            {
                return;
            }

            if (_eventSystemGate.Request(_isInMenu && !_isBlocked, _eventSystem.enabled))
            {
                _eventSystem.enabled = true;
            }
        }

        private void ApplyPendingEventSystemDisable()
        {
            if (_eventSystemGate.ConsumePendingDisable() && _eventSystem != null)
            {
                _eventSystem.enabled = false;
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
                Debug.LogError($"[{nameof(PlayerMenuInput)}] Action '{reference.action.name}' not found in '{_playerInput.actions.name}'.", this);
            }

            return action;
        }

        private void OnValidate()
        {
            if (_playerInput == null)
            {
                TryGetComponent(out _playerInput);
            }

            if (_identity == null)
            {
                TryGetComponent(out _identity);
            }

            if (_gameplayInput == null)
            {
                TryGetComponent(out _gameplayInput);
            }

            if (_settings == null)
            {
                Debug.LogWarning($"[{nameof(PlayerMenuInput)}] Missing MenuSettings on '{name}'.", this);
            }

            if (_eventSystem == null)
            {
                Debug.LogWarning($"[{nameof(PlayerMenuInput)}] Missing MultiplayerEventSystem on '{name}' (child UIInput).", this);
            }

            if (_navigate == null || _submit == null || _cancel == null)
            {
                Debug.LogWarning($"[{nameof(PlayerMenuInput)}] One or more UI InputActionReferences are not assigned on '{name}'.", this);
            }
        }
    }
}
