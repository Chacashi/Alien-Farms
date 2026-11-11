using AlienFarms.Animals;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Items;
using AlienFarms.Match;
using AlienFarms.Networking.Data;
using AlienFarms.Networking.Session;
using AlienFarms.Zones;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Match
{
    /// <summary>
    /// Online setup of the shared match objects.
    /// <list type="bullet">
    /// <item>Pure client: the match is the server's, so MatchManager is disabled and the spawners and obstacle field
    /// become Replicas (state arrives from the network). On the server everything stays Authority.</item>
    /// <item>Every instance: the match exit buttons use the <see cref="OnlineMatchExitHandler"/>.</item>
    /// <item>Pure client: when this client is dropped (host left, kicked) the NetworkRoot is torn down and the main menu
    /// loaded on the next frame, leaving the reason on the anchor for the menu.</item>
    /// </list>
    /// Lives under OnlineModeRoot: the GameModeRouter activates it inside its own Awake, so this Awake runs before the
    /// obstacle field's Awake and the spawners' Start.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OnlineMatchBootstrap : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Runtime anchor of the NetworkRoot (tells server from client).")]
        private NetworkRuntimeAnchor _anchor;

        [SerializeField, Tooltip("Runtime session; cleared when leaving for the title screen.")]
        private MatchSession _session;

        [Header("References")]
        [SerializeField, Tooltip("Match state machine (server only).")]
        private MatchManager _match;

        [SerializeField, Tooltip("Exit buttons (rematch / title); get the online handler.")]
        private MatchExit _exit;

        [SerializeField, Tooltip("Animal spawner (Replica on clients).")]
        private AnimalSpawner _animalSpawner;

        [SerializeField, Tooltip("Optional bomb spawner (Replica on clients).")]
        private BombSpawner _bombSpawner;

        [SerializeField, Tooltip("Obstacle layout (Replica on clients).")]
        private ObstacleField _obstacles;

        private SimulationRole _appliedRole = SimulationRole.Authority;
        private NetworkManager _watchedManager;
        private bool _dropped;

        /// <summary>Role applied to the shared simulation in Awake.</summary>
        public SimulationRole AppliedRole => _appliedRole;

        private void Awake()
        {
            if (_exit != null)
            {
                _exit.SetHandler(new OnlineMatchExitHandler(_anchor, _session));
            }

            if (_anchor == null || !_anchor.HasRoot)
            {
                Debug.LogError($"[{nameof(OnlineMatchBootstrap)}] No NetworkRoot published; online mode without a session. Everything stays Authority.", this);
                return;
            }

            if (!_anchor.IsPureClient)
            {
                return;
            }

            _appliedRole = SimulationRole.Replica;

            if (_match != null)
            {
                _match.enabled = false;
            }

            if (_animalSpawner != null)
            {
                _animalSpawner.SetSimulationRole(SimulationRole.Replica);
            }

            if (_bombSpawner != null)
            {
                _bombSpawner.SetSimulationRole(SimulationRole.Replica);
            }

            if (_obstacles != null)
            {
                _obstacles.SetSimulationRole(SimulationRole.Replica);
            }
        }

        private void OnEnable()
        {
            if (_anchor != null && _anchor.IsPureClient)
            {
                _watchedManager = _anchor.Manager;
                _watchedManager.OnClientDisconnectCallback += HandleClientDisconnected;
            }
        }

        private void OnDisable()
        {
            if (_watchedManager != null)
            {
                _watchedManager.OnClientDisconnectCallback -= HandleClientDisconnected;
                _watchedManager = null;
            }
        }

        private void Update()
        {
            if (!_dropped)
            {
                return;
            }

            // Out of the NGO callback: tear down and leave on a regular frame.
            _dropped = false;
            if (_session != null)
            {
                _session.Clear();
            }

            NetworkRootLifetime root = _anchor != null ? _anchor.Root : null;
            if (root != null)
            {
                root.Teardown();
            }

            SceneFlow.LoadMainMenu();
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            NetworkManager manager = _watchedManager;

            // On a client NGO reports its own drop with LocalClientId. A teardown already in progress unpublished the root.
            if (manager == null || clientId != manager.LocalClientId || _anchor == null || !_anchor.HasRoot)
            {
                return;
            }

            // NGO puts raw transport text here when the server gave no reason: only known keys reach the UI.
            _anchor.SetDisconnectReason(DisconnectReasons.Normalize(manager.DisconnectReason));
            _dropped = true;
        }

        private void OnValidate()
        {
            if (_anchor == null)
            {
                Debug.LogWarning($"[{nameof(OnlineMatchBootstrap)}] Missing NetworkRuntimeAnchor on '{name}'.", this);
            }

            if (_session == null)
            {
                Debug.LogWarning($"[{nameof(OnlineMatchBootstrap)}] Missing MatchSession on '{name}'.", this);
            }

            if (_match == null)
            {
                Debug.LogWarning($"[{nameof(OnlineMatchBootstrap)}] Missing MatchManager on '{name}'.", this);
            }

            if (_exit == null)
            {
                Debug.LogWarning($"[{nameof(OnlineMatchBootstrap)}] Missing MatchExit on '{name}'; exit buttons use the local handler.", this);
            }

            if (_animalSpawner == null)
            {
                Debug.LogWarning($"[{nameof(OnlineMatchBootstrap)}] Missing AnimalSpawner on '{name}'.", this);
            }

            if (_obstacles == null)
            {
                Debug.LogWarning($"[{nameof(OnlineMatchBootstrap)}] Missing ObstacleField on '{name}'.", this);
            }
        }
    }
}
