using AlienFarms.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AlienFarms.Player
{
    /// <summary>
    /// Scene composition root for local multiplayer joining: listens to <see cref="PlayerInputManager"/>
    /// and configures each spawned player (identity, spawn point, camera yaw, color) through its <see cref="PlayerRig"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerJoinCoordinator : MonoBehaviour
    {
        // Matches the PlayerId enum (Player1, Player2): index i maps to (PlayerId)(i + 1).
        private const int RequiredPlayers = 2;

        [Header("References")]
        [SerializeField, Tooltip("Manager that spawns the Player prefab (Notification Behavior: Invoke C Sharp Events).")]
        private PlayerInputManager _manager;

        [SerializeField, Tooltip("Spawn points by player index (0 = Player 1, 1 = Player 2).")]
        private Transform[] _spawnPoints = new Transform[RequiredPlayers];

        [SerializeField, Tooltip("Body materials by player index (0 = Player 1, 1 = Player 2).")]
        private Material[] _playerMaterials = new Material[RequiredPlayers];

        [SerializeField, Tooltip("Overview camera rendered while no player has joined.")]
        private Camera _lobbyCamera;

        private bool _isQuitting;

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
            if (!TryGetJoinData(index, playerInput, out PlayerRig rig, out Transform spawn, out Material material))
            {
                // Never leave a half-configured player alive.
                Destroy(playerInput.gameObject);
                return;
            }

            PlayerId id = (PlayerId)(index + 1);
            rig.Configure(id, spawn, material);

            SetLobbyCameraActive(false);

            // Stops the manager from logging "maximum number of players reached" on every extra device press.
            if (_manager.playerCount >= _manager.maxPlayerCount)
            {
                _manager.DisableJoining();
            }

            GameEvents.RaisePlayerJoined(id);
        }

        private void HandlePlayerLeft(PlayerInput playerInput)
        {
            if (IsTearingDown())
            {
                return;
            }

            if (playerInput.TryGetComponent(out PlayerRig rig) && rig.Id != PlayerId.None)
            {
                GameEvents.RaisePlayerLeft(rig.Id);
            }

            _manager.EnableJoining();

            // playerCount is already updated when onPlayerLeft fires.
            if (_manager.playerCount == 0)
            {
                SetLobbyCameraActive(true);
            }
            else if (_manager.playerCount == 1)
            {
                // The manager keeps the remaining player's split-screen slot (e.g. right half if P1 left),
                // so give it the full screen until the next join recomputes the layout.
                ExpandRemainingPlayerCamera(playerInput);
            }
        }

        private static void ExpandRemainingPlayerCamera(PlayerInput leavingPlayer)
        {
            foreach (PlayerInput player in PlayerInput.all)
            {
                if (player != leavingPlayer && player.camera != null)
                {
                    player.camera.rect = new Rect(0f, 0f, 1f, 1f);
                }
            }
        }

        private bool TryGetJoinData(int index, PlayerInput playerInput, out PlayerRig rig, out Transform spawn, out Material material)
        {
            rig = null;
            spawn = null;
            material = null;

            if (index < 0 || index >= RequiredPlayers)
            {
                Debug.LogError($"[{nameof(PlayerJoinCoordinator)}] Player index {index} has no PlayerId (max {RequiredPlayers} players).", this);
                return false;
            }

            if (index >= _spawnPoints.Length || _spawnPoints[index] == null)
            {
                Debug.LogError($"[{nameof(PlayerJoinCoordinator)}] No spawn point for player index {index}.", this);
                return false;
            }

            if (index >= _playerMaterials.Length || _playerMaterials[index] == null)
            {
                Debug.LogError($"[{nameof(PlayerJoinCoordinator)}] No material for player index {index}.", this);
                return false;
            }

            if (!playerInput.TryGetComponent(out rig))
            {
                Debug.LogError($"[{nameof(PlayerJoinCoordinator)}] Joined player '{playerInput.name}' has no {nameof(PlayerRig)}.", this);
                return false;
            }

            spawn = _spawnPoints[index];
            material = _playerMaterials[index];
            return true;
        }

        private bool IsTearingDown()
        {
            return _isQuitting || !gameObject.scene.isLoaded;
        }

        private void SetLobbyCameraActive(bool active)
        {
            // Unity null check also covers an already destroyed camera.
            if (_lobbyCamera != null)
            {
                _lobbyCamera.gameObject.SetActive(active);
            }
        }

        private void OnValidate()
        {
            if (_manager == null)
            {
                Debug.LogWarning($"[{nameof(PlayerJoinCoordinator)}] Missing PlayerInputManager on '{name}'.", this);
            }
            else if (_manager.maxPlayerCount != RequiredPlayers)
            {
                Debug.LogWarning($"[{nameof(PlayerJoinCoordinator)}] PlayerInputManager Max Player Count should be {RequiredPlayers} on '{_manager.name}'.", this);
            }

            if (_lobbyCamera == null)
            {
                Debug.LogWarning($"[{nameof(PlayerJoinCoordinator)}] Missing lobby camera on '{name}'.", this);
            }

            ValidateArray(_spawnPoints, "spawn point");
            ValidateArray(_playerMaterials, "player material");
        }

        private void ValidateArray<T>(T[] array, string label) where T : Object
        {
            if (array == null || array.Length < RequiredPlayers)
            {
                Debug.LogWarning($"[{nameof(PlayerJoinCoordinator)}] '{name}' needs at least {RequiredPlayers} {label} entries.", this);
                return;
            }

            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == null)
                {
                    Debug.LogWarning($"[{nameof(PlayerJoinCoordinator)}] {label} {i} is empty on '{name}'.", this);
                }
            }
        }
    }
}
