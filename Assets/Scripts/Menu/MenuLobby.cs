using System;
using AlienFarms.Core;
using AlienFarms.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AlienFarms.Menu
{
    /// <summary>
    /// MainMenu player registry: listens to <see cref="PlayerInputManager"/> (MenuPlayer prefab), assigns each joined
    /// player its <see cref="PlayerId"/> from the player index and keeps its <see cref="PlayerMenuInput"/> and
    /// <see cref="PlayerInput"/> (devices and control scheme for the match session).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MenuLobby : MonoBehaviour
    {
        private const int MaxPlayers = 2;

        [Header("References")]
        [SerializeField, Tooltip("Manager that spawns MenuPlayer (Notification Behavior: Invoke C Sharp Events, Max Player Count 2).")]
        private PlayerInputManager _manager;

        // Index = (int)PlayerId - 1.
        private readonly PlayerMenuInput[] _menus = new PlayerMenuInput[MaxPlayers];
        private readonly PlayerInput[] _inputs = new PlayerInput[MaxPlayers];
        private bool _joiningAllowed = true;
        private bool _isQuitting;

        /// <summary>A player joined and was identified. Raised from the manager's join callback: defer input changes.</summary>
        public event Action<PlayerMenuInput> Joined;

        /// <summary>A player left (not raised while the scene is being torn down).</summary>
        public event Action<PlayerId> Left;

        public int JoinedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _menus.Length; i++)
                {
                    if (_menus[i] != null)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool TryGet(PlayerId id, out PlayerMenuInput menu)
        {
            int index = (int)id - 1;
            menu = index >= 0 && index < MaxPlayers ? _menus[index] : null;
            return menu != null;
        }

        public bool TryGetInput(PlayerId id, out PlayerInput input)
        {
            int index = (int)id - 1;
            input = index >= 0 && index < MaxPlayers ? _inputs[index] : null;
            return input != null;
        }

        /// <summary>Allows or stops new players joining (still capped by the max player count).</summary>
        public void SetJoiningEnabled(bool enabled)
        {
            _joiningAllowed = enabled;
            RefreshJoining();
        }

        private void OnEnable()
        {
            if (_manager == null)
            {
                return;
            }

            _manager.onPlayerJoined += HandlePlayerJoined;
            _manager.onPlayerLeft += HandlePlayerLeft;
        }

        private void OnDisable()
        {
            if (_manager == null)
            {
                return;
            }

            _manager.onPlayerJoined -= HandlePlayerJoined;
            _manager.onPlayerLeft -= HandlePlayerLeft;
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        private void HandlePlayerJoined(PlayerInput playerInput)
        {
            int index = playerInput.playerIndex;
            if (index < 0 || index >= MaxPlayers
                || !playerInput.TryGetComponent(out PlayerMenuInput menu)
                || !playerInput.TryGetComponent(out PlayerIdentity identity))
            {
                Debug.LogError($"[{nameof(MenuLobby)}] Joined player '{playerInput.name}' (index {index}) is invalid: needs PlayerMenuInput and PlayerIdentity, max {MaxPlayers} players.", this);
                Destroy(playerInput.gameObject);
                return;
            }

            identity.Initialize((PlayerId)(index + 1));
            _menus[index] = menu;
            _inputs[index] = playerInput;
            RefreshJoining();
            Joined?.Invoke(menu);
        }

        private void HandlePlayerLeft(PlayerInput playerInput)
        {
            int index = Array.IndexOf(_inputs, playerInput);
            if (index < 0)
            {
                return;
            }

            _menus[index] = null;
            _inputs[index] = null;
            if (_isQuitting || !gameObject.scene.isLoaded)
            {
                return;
            }

            RefreshJoining();
            Left?.Invoke((PlayerId)(index + 1));
        }

        private void RefreshJoining()
        {
            if (_manager == null)
            {
                return;
            }

            // Disabled when full so the manager does not log "maximum number of players reached" on extra presses.
            if (_joiningAllowed && _manager.playerCount < _manager.maxPlayerCount)
            {
                _manager.EnableJoining();
            }
            else
            {
                _manager.DisableJoining();
            }
        }

        private void OnValidate()
        {
            if (_manager == null)
            {
                Debug.LogWarning($"[{nameof(MenuLobby)}] Missing PlayerInputManager on '{name}'.", this);
            }
            else if (_manager.maxPlayerCount != MaxPlayers)
            {
                Debug.LogWarning($"[{nameof(MenuLobby)}] PlayerInputManager Max Player Count should be {MaxPlayers} on '{_manager.name}'.", this);
            }
        }
    }
}
