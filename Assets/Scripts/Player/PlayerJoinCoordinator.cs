using AlienFarms.Core;
using AlienFarms.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AlienFarms.Player
{
    /// <summary>
    /// Scene composition root for local multiplayer joining: listens to <see cref="PlayerInputManager"/>
    /// and configures each spawned player (identity, spawn point, camera yaw, character) through its
    /// <see cref="PlayerRig"/>. Keeps a registry of joined rigs for the match (count, respawn, freeze).
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

        [SerializeField, Tooltip("Characters used when no character was picked beforehand, by player index (0 = Player 1, 1 = Player 2).")]
        private CharacterDefinition[] _fallbackCharacters = new CharacterDefinition[RequiredPlayers];

        [SerializeField, Tooltip("Optional: choices made in the MainMenu. When ready, players join with their devices and characters at Start.")]
        private MatchSession _session;

        [SerializeField, Tooltip("Overview camera rendered while no player has joined.")]
        private Camera _lobbyCamera;

        // Index = (int)PlayerId - 1. Updated before PlayerJoined/PlayerLeft are raised.
        private readonly PlayerRig[] _rigs = new PlayerRig[RequiredPlayers];

        // Characters picked before joining (character select session). Null slot -> fallback character.
        private readonly CharacterDefinition[] _pendingCharacters = new CharacterDefinition[RequiredPlayers];
        private bool _controlsEnabled = true;
        private bool _isQuitting;

        /// <summary>Players currently registered (joined and configured).</summary>
        public int JoinedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _rigs.Length; i++)
                {
                    if (_rigs[i] != null)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool TryGetRig(PlayerId id, out PlayerRig rig)
        {
            int index = (int)id - 1;
            rig = index >= 0 && index < _rigs.Length ? _rigs[index] : null;
            return rig != null;
        }

        /// <summary>Drops carried animals and moves every joined player back to its spawn point.</summary>
        public void RespawnAll()
        {
            for (int i = 0; i < _rigs.Length; i++)
            {
                PlayerRig rig = _rigs[i];
                if (rig != null && i < _spawnPoints.Length)
                {
                    rig.Respawn(_spawnPoints[i]);
                }
            }
        }

        /// <summary>Freezes or unfreezes every joined player; also applied to players that join later.</summary>
        public void SetControlsEnabled(bool enabled)
        {
            _controlsEnabled = enabled;
            for (int i = 0; i < _rigs.Length; i++)
            {
                if (_rigs[i] != null)
                {
                    _rigs[i].SetControlsEnabled(enabled);
                }
            }
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

        private void Start()
        {
            if (_session != null && _session.IsReady)
            {
                JoinFromSession();
            }
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        private void HandlePlayerJoined(PlayerInput playerInput)
        {
            int index = playerInput.playerIndex;
            if (!TryGetJoinData(index, playerInput, out PlayerRig rig, out Transform spawn))
            {
                // Never leave a half-configured player alive.
                Destroy(playerInput.gameObject);
                return;
            }

            PlayerId id = (PlayerId)(index + 1);
            CharacterDefinition character = ResolveCharacter(index);
            rig.Configure(id, spawn, character);
            _rigs[index] = rig;
            rig.SetControlsEnabled(_controlsEnabled);

            SetLobbyCameraActive(false);

            // Stops the manager from logging "maximum number of players reached" on every extra device press.
            if (_manager.playerCount >= _manager.maxPlayerCount)
            {
                _manager.DisableJoining();
            }

            // Character first: HUD/results listeners know who the player is when PlayerJoined arrives.
            GameEvents.RaisePlayerCharacterAssigned(id, character);
            GameEvents.RaisePlayerJoined(id);
        }

        /// <summary>
        /// Joins every session player with the devices and scheme it used in the MainMenu (P1 left, P2 right).
        /// A slot whose devices are gone falls back to press-to-join with its fallback character.
        /// </summary>
        private void JoinFromSession()
        {
            if (_manager == null)
            {
                return;
            }

            for (int i = 0; i < RequiredPlayers; i++)
            {
                PlayerId id = (PlayerId)(i + 1);
                if (!_session.TryGetSlot(id, out SessionSlot slot))
                {
                    continue;
                }

                // Kept even if the join fails: a manual join of this slot still uses the picked character.
                _pendingCharacters[i] = slot.Character;
                if (!AreDevicesAvailable(slot))
                {
                    Debug.LogWarning($"[{nameof(PlayerJoinCoordinator)}] {id} has no available session devices; it can join by pressing a button.", this);
                    continue;
                }

                if (_manager.JoinPlayer(i, i, slot.ControlScheme, slot.Devices) == null)
                {
                    Debug.LogWarning($"[{nameof(PlayerJoinCoordinator)}] Could not join {id} from the session; it can join by pressing a button.", this);
                }
            }

            if (_manager.playerCount >= _manager.maxPlayerCount)
            {
                _manager.DisableJoining();
            }
            else
            {
                _manager.EnableJoining();
            }
        }

        private static bool AreDevicesAvailable(SessionSlot slot)
        {
            InputDevice[] devices = slot.Devices;
            if (devices == null || devices.Length == 0 || string.IsNullOrEmpty(slot.ControlScheme))
            {
                return false;
            }

            for (int i = 0; i < devices.Length; i++)
            {
                if (devices[i] == null || !devices[i].added)
                {
                    return false;
                }
            }

            return true;
        }

        private CharacterDefinition ResolveCharacter(int index)
        {
            CharacterDefinition pending = _pendingCharacters[index];
            if (pending != null)
            {
                return pending;
            }

            return index < _fallbackCharacters.Length ? _fallbackCharacters[index] : null;
        }

        private void HandlePlayerLeft(PlayerInput playerInput)
        {
            bool hasRig = playerInput.TryGetComponent(out PlayerRig rig);
            if (hasRig)
            {
                // Unregister before PlayerLeft so listeners already see the updated count (also on teardown).
                Unregister(rig);
            }

            if (IsTearingDown())
            {
                return;
            }

            if (hasRig && rig.Id != PlayerId.None)
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

        private void Unregister(PlayerRig rig)
        {
            for (int i = 0; i < _rigs.Length; i++)
            {
                if (_rigs[i] == rig)
                {
                    _rigs[i] = null;
                }
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

        private bool TryGetJoinData(int index, PlayerInput playerInput, out PlayerRig rig, out Transform spawn)
        {
            rig = null;
            spawn = null;

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

            if (!playerInput.TryGetComponent(out rig))
            {
                Debug.LogError($"[{nameof(PlayerJoinCoordinator)}] Joined player '{playerInput.name}' has no {nameof(PlayerRig)}.", this);
                return false;
            }

            spawn = _spawnPoints[index];
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
            ValidateArray(_fallbackCharacters, "fallback character");
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
