using AlienFarms.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AlienFarms.Input
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    [DefaultExecutionOrder(10)]
    public sealed class PlayerVoiceHotkey : MonoBehaviour
    {
        [SerializeField] private PlayerInput _playerInput;
        [SerializeField] private InputActionReference _toggleMic;
        [SerializeField] private VoiceChatChannel _voice;

        private InputAction _action;
        private InputActionAsset _resolvedAsset;
        private bool _ready;

        private void OnEnable()
        {
            _ready = _playerInput != null && _toggleMic != null && _toggleMic.action != null && _voice != null;
            if (!_ready)
            {
                return;
            }

            _playerInput.onControlsChanged += Resolve;
            TryResolve();
        }

        // Online, NetworkPlayer enables PlayerInput only after ownership is applied, and PlayerInput clones its
        // actions on enable; re-resolve whenever the live asset changes (a reference compare, no allocation).
        private void Update()
        {
            if (_ready && _resolvedAsset != _playerInput.actions)
            {
                TryResolve();
            }
        }

        private void TryResolve()
        {
            if (_playerInput.isActiveAndEnabled)
            {
                Resolve(_playerInput);
            }
        }

        private void OnDisable()
        {
            if (_playerInput != null)
            {
                _playerInput.onControlsChanged -= Resolve;
            }

            if (_action != null)
            {
                _action.performed -= HandleToggleMic;
                _action = null;
            }

            _resolvedAsset = null;
        }

        private void OnValidate()
        {
            if (_playerInput == null || _toggleMic == null || _voice == null)
            {
                Debug.LogWarning($"[{nameof(PlayerVoiceHotkey)}] Missing PlayerInput, ToggleMic or VoiceChatChannel reference on '{name}'.", this);
            }
        }

        private void Resolve(PlayerInput input)
        {
            _resolvedAsset = input.actions;
            InputAction action = _resolvedAsset != null ? _resolvedAsset.FindAction(_toggleMic.action.id) : null;
            if (ReferenceEquals(action, _action))
            {
                return;
            }

            if (_action != null)
            {
                _action.performed -= HandleToggleMic;
            }

            _action = action;
            if (_action != null)
            {
                _action.performed += HandleToggleMic;
            }
        }

        private void HandleToggleMic(InputAction.CallbackContext context)
        {
            if (_playerInput.isActiveAndEnabled && _playerInput.inputIsActive)
            {
                _voice.RequestToggleMic();
            }
        }
    }
}
