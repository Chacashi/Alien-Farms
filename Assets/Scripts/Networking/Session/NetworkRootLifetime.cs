using AlienFarms.Networking.Data;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Lifetime of the persistent NetworkRoot: the first instance survives scene loads and publishes its services on the
    /// <see cref="NetworkRuntimeAnchor"/>; any later copy (MainMenu reloaded) deactivates and destroys itself in Awake,
    /// before its NetworkManager can claim the singleton. <see cref="Teardown"/> ends the session and removes the root.
    /// Runs first so the duplicate guard wins over NetworkManager.OnEnable.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class NetworkRootLifetime : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Runtime anchor this root publishes itself on.")]
        private NetworkRuntimeAnchor _anchor;

        [Header("References")]
        [SerializeField, Tooltip("NetworkManager on this root.")]
        private NetworkManager _manager;

        [SerializeField, Tooltip("Connection approval on this root.")]
        private ConnectionApprovalHandler _approval;

        [SerializeField, Tooltip("Networked scene loader on this root.")]
        private NetworkSceneLoader _sceneLoader;

        [SerializeField, Tooltip("Connection strategy on this root: a component implementing IConnectionStrategy (DirectConnection).")]
        private MonoBehaviour _connection;

        [SerializeField, Tooltip("Voluntary-leave notice on this root (the match exit sends it before the teardown).")]
        private LeaveNoticeMessenger _leaveNotice;

        [SerializeField, Tooltip("Online session service on this root (the match exit clears its rejoin offer).")]
        private OnlineSessionService _sessionService;

        private bool _isPublished;

        public NetworkManager Manager => _manager;

        public ConnectionApprovalHandler Approval => _approval;

        public NetworkSceneLoader SceneLoader => _sceneLoader;

        public IConnectionStrategy Connection => _connection as IConnectionStrategy;

        public LeaveNoticeMessenger LeaveNotice => _leaveNotice;

        public OnlineSessionService SessionService => _sessionService;

        /// <summary>Runtime anchor this root publishes itself on (outlives the root: e.g. the rejoin session id).</summary>
        public NetworkRuntimeAnchor Anchor => _anchor;

        private void Awake()
        {
            if (_anchor == null)
            {
                Debug.LogError($"[{nameof(NetworkRootLifetime)}] Missing NetworkRuntimeAnchor on '{name}'; the root cannot be reached.", this);
                return;
            }

            NetworkRootLifetime existing = _anchor.Root;
            if (existing != null && existing != this)
            {
                // Inactive first: the remaining components of this copy never get Awake/OnEnable.
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }

            if (transform.parent != null)
            {
                transform.SetParent(null, false);
            }

            DontDestroyOnLoad(gameObject);
            _anchor.Publish(this);
            _isPublished = true;

            // A previous root may still be registered (destroyed at the end of this frame): its NetworkManager.OnEnable
            // would then skip the singleton and its OnDestroy leave it null. Claim it now; the old one only clears
            // the singleton when it still points at itself.
            if (_manager != null && NetworkManager.Singleton != _manager)
            {
                _manager.SetSingleton();
            }
        }

        private void OnDestroy()
        {
            if (_isPublished && _anchor != null)
            {
                _anchor.Clear(this);
            }
        }

        /// <summary>Shuts the session down (if running) and destroys this root. The caller loads the next scene.</summary>
        public void Teardown()
        {
            // Unpublish first: callbacks fired by the shutdown already see no session (no re-entrant teardown).
            if (_isPublished && _anchor != null)
            {
                _anchor.Clear(this);
                _isPublished = false;
            }

            ShutdownSession();
            Destroy(gameObject);
        }

        /// <summary>
        /// Stops the session, if running, without destroying the root. A host first disconnects every remote client
        /// with <see cref="DisconnectReasons.HostDisconnected"/> so they show a known reason.
        /// </summary>
        public void ShutdownSession()
        {
            if (_manager == null || !_manager.IsListening || _manager.ShutdownInProgress)
            {
                return;
            }

            if (_manager.IsServer)
            {
                // Backwards: disconnecting may remove the id from the list.
                var clients = _manager.ConnectedClientsIds;
                for (int i = clients.Count - 1; i >= 0; i--)
                {
                    ulong clientId = clients[i];
                    if (clientId != NetworkManager.ServerClientId)
                    {
                        _manager.DisconnectClient(clientId, DisconnectReasons.HostDisconnected);
                    }
                }
            }

            _manager.Shutdown();
        }

        private void OnValidate()
        {
            if (_anchor == null)
            {
                Debug.LogWarning($"[{nameof(NetworkRootLifetime)}] Missing NetworkRuntimeAnchor on '{name}'.", this);
            }

            if (_manager == null)
            {
                TryGetComponent(out _manager);
            }

            if (_approval == null)
            {
                TryGetComponent(out _approval);
            }

            if (_sceneLoader == null)
            {
                TryGetComponent(out _sceneLoader);
            }

            if (_leaveNotice == null)
            {
                TryGetComponent(out _leaveNotice);
            }

            if (_sessionService == null)
            {
                TryGetComponent(out _sessionService);
            }

            if (_connection == null)
            {
                Debug.LogWarning($"[{nameof(NetworkRootLifetime)}] Missing connection strategy on '{name}'.", this);
            }
            else if (!(_connection is IConnectionStrategy))
            {
                Debug.LogWarning($"[{nameof(NetworkRootLifetime)}] Connection '{_connection.GetType().Name}' on '{name}' does not implement {nameof(IConnectionStrategy)}.", this);
            }
        }
    }
}
