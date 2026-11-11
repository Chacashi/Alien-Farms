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
            int characterIndex = slot.CharacterIndex != ConnectionPayload.NoCharacter ? slot.CharacterIndex : index;
            CharacterDefinition character = _characters != null ? _characters.Get(characterIndex) : null;

            // One instantiation per join (not a hot path); the instance becomes the client's player object.
            NetworkObject instance = Instantiate(_playerPrefab, spawn.position, spawn.rotation);
            if (!instance.TryGetComponent(out NetworkPlayer player) || !instance.TryGetComponent(out PlayerRig rig))
            {
                Debug.LogError($"[{nameof(NetworkPlayerRoster)}] Player prefab '{_playerPrefab.name}' needs NetworkPlayer and PlayerRig.", this);
                Destroy(instance.gameObject);
                return;
            }

            player.ServerInitialize(slot.Id, characterIndex, slot.DisplayName);
            instance.SpawnAsPlayerObject(clientId, true);

            rig.Configure(slot.Id, spawn, character);
            rig.SetControlsEnabled(_controlsEnabled);
            if (player.Motion != null)
            {
                player.Motion.MarkTeleport();
            }

            _rigs[index] = rig;
            _players[index] = player;
            _clientIds[index] = clientId;

            Debug.Log($"[{nameof(NetworkPlayerRoster)}] Spawned {slot.Id} for client {clientId}.", this);

            // Character first: HUD listeners know who the player is when PlayerJoined arrives.
            GameEvents.RaisePlayerCharacterAssigned(slot.Id, character);
            GameEvents.RaisePlayerJoined(slot.Id);
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
