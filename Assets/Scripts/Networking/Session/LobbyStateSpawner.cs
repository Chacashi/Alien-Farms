using System.Collections;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Networking.Data;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// MainMenu-only: spawns the networked room (<see cref="NetworkLobbyState"/>) once the host is in a Sessions + Relay
    /// session. Lives in the scene and not on the NetworkRoot, so a direct host from the dev panel never creates a room.
    /// Never despawns it: NGO's shutdown or the room itself (when the match starts) does.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LobbyStateSpawner : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Runtime anchor of the NetworkRoot.")]
        private NetworkRuntimeAnchor _anchor;

        [SerializeField, Tooltip("Runtime session channel written by the root's OnlineSessionService.")]
        private OnlineSessionChannel _session;

        [SerializeField, Tooltip("Optional: lobby channel set back to Unavailable while there is no session (clears a stale phase).")]
        private OnlineLobbyChannel _lobbyChannel;

        [Header("References")]
        [SerializeField, Tooltip("LobbyState prefab (NetworkObject + NetworkLobbyState), registered in the NetworkPrefabs list.")]
        private NetworkObject _lobbyPrefab;

        private NetworkObject _instance;
        private Coroutine _pendingRoutine;
        private bool _spawnedThisSession;
        private bool _wasInSession;
        private bool _pending;

        private void OnEnable()
        {
            if (_session == null)
            {
                return;
            }

            _session.Changed += HandleSessionChanged;
            HandleSessionChanged();
        }

        private void OnDisable()
        {
            if (_session != null)
            {
                _session.Changed -= HandleSessionChanged;
            }

            // Disabling stops the coroutine too.
            _pending = false;
            _pendingRoutine = null;
        }

        private void HandleSessionChanged()
        {
            OnlineSessionStatus status = _session.Status;
            if (status != OnlineSessionStatus.InSession)
            {
                // Clear the room only when a session ended (or there is none). Never while signing in, creating or
                // joining: a client may already have the room published before its session reports InSession.
                bool clear = _wasInSession || status == OnlineSessionStatus.Ready || status == OnlineSessionStatus.SignedOut;
                _wasInSession = false;
                _spawnedThisSession = false;
                _pending = false;
                if (clear && _lobbyChannel != null)
                {
                    _lobbyChannel.SetPhase(OnlineLobbyPhase.Unavailable);
                }

                return;
            }

            _wasInSession = true;
            _pending = _session.IsHost && !_spawnedThisSession;
            if (_pending)
            {
                TrySpawn();
            }

            if (_pending && _pendingRoutine == null)
            {
                _pendingRoutine = StartCoroutine(RetrySpawn());
            }
        }

        // Only while the session reports InSession as host before NGO's server is listening (rare ordering): polls
        // once per frame until the spawn happens or the session ends, instead of an always-on Update.
        private IEnumerator RetrySpawn()
        {
            while (_pending)
            {
                yield return null;
                if (_pending)
                {
                    TrySpawn();
                }
            }

            _pendingRoutine = null;
        }

        private void TrySpawn()
        {
            if (_lobbyPrefab == null || _anchor == null || !_anchor.IsServer)
            {
                return;
            }

            _pending = false;

            // The room despawns itself when the match starts: never bring it back while that scene load runs.
            NetworkSceneLoader loader = _anchor.SceneLoader;
            if (_instance != null || (loader != null && loader.IsLoading))
            {
                _spawnedThisSession = true;
                return;
            }

            // One instantiation per session (not a hot path); destroyed with MainMenu when the match loads.
            _instance = Instantiate(_lobbyPrefab);
            _instance.Spawn(true);
            _spawnedThisSession = true;
            Debug.Log($"[{nameof(LobbyStateSpawner)}] Online room spawned.", this);
        }

        private void OnValidate()
        {
            if (_anchor == null)
            {
                Debug.LogWarning($"[{nameof(LobbyStateSpawner)}] Missing NetworkRuntimeAnchor on '{name}'.", this);
            }

            if (_session == null)
            {
                Debug.LogWarning($"[{nameof(LobbyStateSpawner)}] Missing OnlineSessionChannel on '{name}'.", this);
            }

            if (_lobbyPrefab == null)
            {
                Debug.LogWarning($"[{nameof(LobbyStateSpawner)}] Missing LobbyState prefab on '{name}'.", this);
            }
        }
    }
}
