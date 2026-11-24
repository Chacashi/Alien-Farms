using System;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Networking.Data;
using AlienFarms.Networking.Session;
using AlienFarms.Player;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Player
{
    /// <summary>
    /// Online <see cref="IPlayerRoster"/> (server only): spawns a Player_Net as the player object of every seated client
    /// at its seat's spawn point, keeps the registry the match drives (respawn, freeze) and raises
    /// <see cref="GameEvents.OnPlayerCharacterAssigned"/> / <see cref="GameEvents.OnPlayerJoined"/> /
    /// <see cref="GameEvents.OnPlayerLeft"/> on the server. Inert on clients (they raise those from NetworkPlayer).
    /// Lives under the gameplay scene's OnlineModeRoot; the GameModeRouter binds it to the match.
    /// Reconnection grace: a dropped player's pose is saved in its seat before its avatar despawns, and the avatar of
    /// the returning client is placed there (<see cref="PlayerSpawned"/> with restored = true).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkPlayerRoster : MonoBehaviour, IPlayerRoster
    {
        private const int MaxPlayers = ServerSlotDirectory.Capacity;

        [Header("Config")]
        [SerializeField, Tooltip("Runtime anchor of the NetworkRoot (NetworkManager, seats, scene loader).")]
        private NetworkRuntimeAnchor _anchor;

        [SerializeField, Tooltip("Characters by index; a seat without a pick uses the character at its own index (P1 = 0, P2 = 1).")]
        private CharacterRoster _characters;

        [Header("References")]
        [SerializeField, Tooltip("Player_Net prefab (registered in the NetworkPrefabs list).")]
        private NetworkObject _playerPrefab;

        [SerializeField, Tooltip("Spawn points by seat (0 = Player 1, 1 = Player 2); the same ones the local mode uses.")]
        private Transform[] _spawnPoints = new Transform[MaxPlayers];

        // Index = (int)PlayerId - 1.
        private readonly PlayerRig[] _rigs = new PlayerRig[MaxPlayers];
        private readonly NetworkPlayer[] _players = new NetworkPlayer[MaxPlayers];
        private readonly ulong[] _clientIds = new ulong[MaxPlayers];
        private NetworkManager _manager;
        private NetworkSceneLoader _sceneLoader;
        private bool _controlsEnabled = true;
        private bool _active;

        /// <summary>Server: an avatar spawned for a seat; true when it was restored at the pose a reservation saved.</summary>
        public event Action<PlayerId, bool> PlayerSpawned;

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

        public bool TryGetSpawnPosition(PlayerId id, out Vector3 position)
        {
            int index = (int)id - 1;
            Transform spawn = _spawnPoints != null && index >= 0 && index < _spawnPoints.Length
                ? _spawnPoints[index]
                : null;
            position = spawn != null ? spawn.position : default;
            return spawn != null;
        }

        public bool TryGetRig(PlayerId id, out PlayerRig rig)
        {
            int index = (int)id - 1;
            rig = index >= 0 && index < _rigs.Length ? _rigs[index] : null;
            return rig != null;
        }

        /// <summary>Drops carried objects and moves every player to its spawn; clients snap (Teleport) instead of sliding.</summary>
        public void RespawnAll()
        {
            for (int i = 0; i < _rigs.Length; i++)
            {
                if (_rigs[i] == null || i >= _spawnPoints.Length)
                {
                    continue;
                }

                _rigs[i].Respawn(_spawnPoints[i]);
                if (_players[i] != null && _players[i].Motion != null)
                {
                    _players[i].Motion.MarkTeleport();
                }
            }
        }

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

        private void Start()
        {
            _manager = _anchor != null ? _anchor.Manager : null;
            if (_manager == null || !_anchor.IsServer)
            {
                // Clients (and a scene opened without a session) have nothing to spawn.
                return;
            }

            if (_playerPrefab == null)
            {
                Debug.LogError($"[{nameof(NetworkPlayerRoster)}] Missing Player_Net prefab on '{name}'; no player will spawn.", this);
                return;
            }

            _active = true;
            _manager.OnClientConnectedCallback += HandleClientConnected;
            _manager.OnClientDisconnectCallback += HandleClientDisconnected;

            _sceneLoader = _anchor.SceneLoader;
            if (_sceneLoader != null && _sceneLoader.IsLoading)
            {
                // Clients still loading this scene: spawn once every one of them has it.
                _sceneLoader.LoadCompleted += HandleSceneLoadCompleted;
                return;
            }

            SpawnAllConnected();
        }

        private void OnDestroy()
        {
            if (_sceneLoader != null)
            {
                _sceneLoader.LoadCompleted -= HandleSceneLoadCompleted;
            }

            if (_active && _manager != null)
            {
                _manager.OnClientConnectedCallback -= HandleClientConnected;
                _manager.OnClientDisconnectCallback -= HandleClientDisconnected;
            }

            _active = false;
        }

        private void HandleSceneLoadCompleted(string sceneName)
        {
            _sceneLoader.LoadCompleted -= HandleSceneLoadCompleted;
            SpawnAllConnected();
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (_sceneLoader != null && _sceneLoader.IsLoading)
            {
                return;
            }

            TrySpawn(clientId);
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            // The host's own client only "disconnects" while the session shuts down: not a player leaving.
            if (_manager == null || _manager.ShutdownInProgress || clientId == NetworkManager.ServerClientId)
            {
                return;
            }

            for (int i = 0; i < _rigs.Length; i++)
            {
                if (_players[i] == null || _clientIds[i] != clientId)
                {
                    continue;
                }

                PlayerId id = (PlayerId)(i + 1);
                NetworkPlayer player = _players[i];

                // Before anything moves it: a reserved seat restores its player here (no-op for a released seat).
                SavePose(_rigs[i], id);
                DropCarried(_rigs[i], id, clientId);

                // Its avatar is the server's now (DontDestroyWithOwner): with two avatars owned by client 0, client
                // lookups must not see the leaving one (host == P1 invariant).
                if (player != null && _anchor != null)
                {
                    _anchor.UnregisterPlayer(player);
                }

                // Unregister before PlayerLeft so listeners already see the updated count.
                _rigs[i] = null;
                _players[i] = null;

                if (player != null && player.IsSpawned)
                {
                    player.NetworkObject.Despawn(true);
                }

                GameEvents.RaisePlayerLeft(id);
            }
        }

        // Stores where the leaving player stood and faced in its seat. The approval handler runs first (it subscribed
        // earlier, on the NetworkRoot): a released seat is already empty and TrySavePose refuses it.
        private void SavePose(PlayerRig rig, PlayerId id)
        {
            ServerSlotDirectory slots = _anchor != null ? _anchor.Slots : null;
            if (slots == null || rig == null)
            {
                return;
            }

            Vector3 facing = rig.Movement != null ? rig.Movement.Facing : rig.transform.forward;
            float yaw = Mathf.Atan2(facing.x, facing.z) * Mathf.Rad2Deg;
            slots.TrySavePose(id, rig.transform.position, yaw);
        }

        /// <summary>
        /// NGO already gave the leaving client's objects (DontDestroyWithOwner) to the server, the avatar included, and
        /// the NetworkRigidbody already made the carried body non-kinematic. The avatar is still spawned, so dropping now
        /// takes the normal release path (unparented, teleported, server holder cleared) before it despawns. This only
        /// works because it runs synchronously in NGO's disconnect stack: do not defer it to another frame.
        /// </summary>
        private void DropCarried(PlayerRig rig, PlayerId id, ulong clientId)
        {
            PlayerGrabber grabber = rig != null ? rig.Grabber : null;
            if (grabber == null || !grabber.IsHolding)
            {
                return;
            }

            string heldName = grabber.Held is Component held && held != null ? held.name : "object";
            grabber.ForceDrop(DropReason.Forced, Vector3.zero);
            Debug.Log($"[{nameof(NetworkPlayerRoster)}] {CarriableOwnerRules.SeatTag(id)} (client {clientId}) disconnected while carrying '{heldName}': dropped, owned by the server again.", this);
        }

        private void SpawnAllConnected()
        {
            var clients = _manager.ConnectedClientsIds;
            for (int i = 0; i < clients.Count; i++)
            {
                TrySpawn(clients[i]);
            }
        }

        private void TrySpawn(ulong clientId)
        {
            ServerSlotDirectory slots = _anchor.Slots;
            if (slots == null || !slots.TryGetByClient(clientId, out PlayerSlot slot))
            {
                Debug.LogWarning($"[{nameof(NetworkPlayerRoster)}] Client {clientId} has no seat; no player spawned.", this);
                return;
            }

            int index = (int)slot.Id - 1;
            if (index < 0 || index >= MaxPlayers || _players[index] != null)
            {
                return;
            }

            if (index >= _spawnPoints.Length || _spawnPoints[index] == null)
            {
                Debug.LogError($"[{nameof(NetworkPlayerRoster)}] No spawn point for {slot.Id} on '{name}'.", this);
                return;
            }

            Transform spawn = _spawnPoints[index];
            int characterIndex = ResolveCharacter(slots, slot, index);
            CharacterDefinition character = _characters != null ? _characters.Get(characterIndex) : null;

            // A seat given back after a drop keeps the pose saved when its avatar despawned.
            bool restored = slot.HasSavedPose;
            Vector3 position = restored ? slot.SavedPosition : spawn.position;
            Quaternion rotation = restored ? Quaternion.Euler(0f, slot.SavedFacingYaw, 0f) : spawn.rotation;

            // One instantiation per join (not a hot path); the instance becomes the client's player object.
            NetworkObject instance = Instantiate(_playerPrefab, position, rotation);
            if (!instance.TryGetComponent(out NetworkPlayer player) || !instance.TryGetComponent(out PlayerRig rig))
            {
                Debug.LogError($"[{nameof(NetworkPlayerRoster)}] Player prefab '{_playerPrefab.name}' needs NetworkPlayer and PlayerRig.", this);
                Destroy(instance.gameObject);
                return;
            }

            player.ServerInitialize(slot.Id, characterIndex, slot.DisplayName);
            instance.SpawnAsPlayerObject(clientId, true);

            rig.Configure(slot.Id, spawn, character);
            if (restored)
            {
                rig.PlaceAt(slot.SavedPosition, slot.SavedFacingYaw);
                slots.ClearSavedPose(slot.Id);
            }

            rig.SetControlsEnabled(_controlsEnabled);
            if (player.Motion != null)
            {
                player.Motion.MarkTeleport();
            }

            _rigs[index] = rig;
            _players[index] = player;
            _clientIds[index] = clientId;

            Debug.Log($"[{nameof(NetworkPlayerRoster)}] Spawned {slot.Id} for client {clientId}{(restored ? " (restored at its saved pose)" : string.Empty)}.", this);

            // Character first: HUD listeners know who the player is when PlayerJoined arrives.
            GameEvents.RaisePlayerCharacterAssigned(slot.Id, character);
            GameEvents.RaisePlayerJoined(slot.Id);
            PlayerSpawned?.Invoke(slot.Id, restored);
        }

        // The seat's pick; without one, CharacterPickRules (its own index, or the next one when the other seat uses it).
        // The default is stored in the seat so a reconnection gets the same character back.
        private int ResolveCharacter(ServerSlotDirectory slots, in PlayerSlot slot, int index)
        {
            if (slot.HasCharacter)
            {
                return slot.CharacterIndex;
            }

            int other = OtherSeatCharacter(slots, index);
            int count = _characters != null ? _characters.Count : 0;
            int pick = CharacterPickRules.Pick(index, other, count);
            if (pick < PlayerSlot.NoCharacter)
            {
                slots.TrySetCharacter(slot.Id, (byte)pick);
            }

            return pick;
        }

        // Character the other seat uses: its pick, else its spawned avatar's; -1 when none.
        private int OtherSeatCharacter(ServerSlotDirectory slots, int index)
        {
            int otherIndex = index == 0 ? 1 : 0;
            if (slots.TryGet((PlayerId)(otherIndex + 1), out PlayerSlot other) && other.HasCharacter)
            {
                return other.CharacterIndex;
            }

            NetworkPlayer otherPlayer = _players[otherIndex];
            return otherPlayer != null ? otherPlayer.CharacterIndex : -1;
        }

        private void OnValidate()
        {
            if (_anchor == null)
            {
                Debug.LogWarning($"[{nameof(NetworkPlayerRoster)}] Missing NetworkRuntimeAnchor on '{name}'.", this);
            }

            if (_characters == null)
            {
                Debug.LogWarning($"[{nameof(NetworkPlayerRoster)}] Missing CharacterRoster on '{name}'.", this);
            }

            if (_playerPrefab == null)
            {
                Debug.LogWarning($"[{nameof(NetworkPlayerRoster)}] Missing Player_Net prefab on '{name}'.", this);
            }

            if (_spawnPoints == null || _spawnPoints.Length < MaxPlayers)
            {
                Debug.LogWarning($"[{nameof(NetworkPlayerRoster)}] '{name}' needs {MaxPlayers} spawn points.", this);
                return;
            }

            for (int i = 0; i < _spawnPoints.Length; i++)
            {
                if (_spawnPoints[i] == null)
                {
                    Debug.LogWarning($"[{nameof(NetworkPlayerRoster)}] Spawn point {i} is empty on '{name}'.", this);
                }
            }
        }
    }
}
