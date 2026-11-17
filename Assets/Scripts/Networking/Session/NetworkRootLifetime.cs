using System;
using System.Threading.Tasks;
using AlienFarms.Data;
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
    /// <para>A host with remote clients shuts down gracefully (<see cref="ShutdownSessionAsync"/>): it sends the leaving
    /// notice, keeps the connections open for <see cref="NetworkSettings.HostLeaveNoticeSeconds"/>, disconnects the
    /// clients with <see cref="DisconnectReasons.HostDisconnected"/> and only then stops NGO (see
    /// <see cref="HostShutdownSequence"/>). Meanwhile a teardown keeps the root alive (unpublished, in DontDestroyOnLoad)
    /// and runs the caller's continuation (loading the next scene) once NGO stopped. Anything else shuts down at once.</para>
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class NetworkRootLifetime : MonoBehaviour
    {
        // Notice time used when no NetworkSettings is assigned.
        private const float DefaultHostLeaveNoticeSeconds = 0.5f;

        [Header("Config")]
        [SerializeField, Tooltip("Runtime anchor this root publishes itself on.")]
        private NetworkRuntimeAnchor _anchor;

        [SerializeField, Tooltip("Network settings: how long a leaving host keeps its clients connected after the notice (0.5 s when missing).")]
        private NetworkSettings _settings;

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

        private readonly HostShutdownSequence _shutdown = new HostShutdownSequence();
        private TaskCompletionSource<bool> _shutdownCompletion;
        private Action _onTornDown;
        private bool _isPublished;
        private bool _isTearingDown;

        /// <summary>True while a graceful host shutdown is running (notice sent, NGO not stopped yet).</summary>
        public bool IsShutdownPending => _shutdown.IsRunning;

        /// <summary>
        /// Server: a graceful host shutdown just started (the clients got the leaving notice). The gameplay scene freezes
        /// the match here so nothing ends or scores during the notice time (<see cref="AlienFarms.Networking.Match.ReconnectGraceCoordinator"/>).
        /// </summary>
        public event Action HostLeaveStarted;

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

            ReleaseShutdownOnDestroy();
        }

        // Destroyed mid-shutdown (Play Mode exited, application quit): NGO cleans itself up in its own OnDestroy.
        // Releases the awaiters; the teardown continuation is dropped (no scene to load while quitting).
        internal void ReleaseShutdownOnDestroy()
        {
            _onTornDown = null;
            HostLeaveStarted = null;
            if (_shutdown.IsRunning)
            {
                _shutdown.Reset();
                CompleteShutdownTask();
            }
        }

        // Only steps a pending graceful shutdown (a cheap check otherwise).
        private void Update()
        {
            if (_shutdown.IsRunning)
            {
                StepShutdown(Time.realtimeSinceStartupAsDouble);
            }
        }

        internal void StepShutdown(double now)
        {
            if (!_shutdown.IsRunning)
            {
                return;
            }

            bool isListening = _manager != null && _manager.IsListening;
            bool shutdownInProgress = _manager != null && _manager.ShutdownInProgress;
            HostShutdownStep step;
            try
            {
                step = _shutdown.Step(now, isListening, shutdownInProgress, isListening && HasRemoteClients());
                switch (step)
                {
                    case HostShutdownStep.DisconnectClients:
                        DisconnectRemoteClients();
                        return;
                    case HostShutdownStep.Shutdown:
                        if (_manager != null)
                        {
                            _manager.Shutdown();
                        }

                        return;
                }
            }
            catch (Exception exception)
            {
                // A step threw: stop NGO at once and finish as if the sequence completed (never a hanging root).
                AbortShutdown(exception);
                step = HostShutdownStep.Complete;
            }

            if (step != HostShutdownStep.Complete)
            {
                return;
            }

            CompleteShutdownTask();
            if (_isTearingDown)
            {
                FinishTeardown();
            }
        }

        /// <summary>
        /// Shuts the session down (if running) and destroys this root, then runs <paramref name="onTornDown"/> (the caller
        /// loads the next scene there). Synchronous unless a host still has remote clients: then the root stays alive,
        /// unpublished, until its graceful shutdown ends. A second call while tearing down is ignored.
        /// </summary>
        public void Teardown(Action onTornDown = null)
        {
            if (_isTearingDown)
            {
                Debug.LogWarning($"[{nameof(NetworkRootLifetime)}] Teardown already in progress on '{name}'; ignored.", this);
                return;
            }

            _isTearingDown = true;
            _onTornDown = onTornDown;

            // Unpublish first: callbacks fired by the shutdown already see no session (no re-entrant teardown).
            if (_isPublished && _anchor != null)
            {
                _anchor.Clear(this);
                _isPublished = false;
            }

            try
            {
                _ = ShutdownSessionAsync();
            }
            catch (Exception exception)
            {
                // Never leave the root hanging: stop NGO as before and let the caller load its scene.
                AbortShutdown(exception);
            }

            if (!_shutdown.IsRunning)
            {
                FinishTeardown();
            }
        }

        /// <summary>Stops the session, if running, without destroying the root (see <see cref="ShutdownSessionAsync"/>).</summary>
        public void ShutdownSession()
        {
            _ = ShutdownSessionAsync();
        }

        /// <summary>
        /// Stops the session, if running, without destroying the root. A host with remote clients first sends them the
        /// leaving notice and waits for it to get through, then disconnects them with
        /// <see cref="DisconnectReasons.HostDisconnected"/> and stops NGO: the task completes once NGO stopped (or the root
        /// was destroyed meanwhile). Anything else requests the NGO shutdown at once and returns a completed task.
        /// Calling it again while a graceful shutdown runs returns that same task.
        /// </summary>
        public Task ShutdownSessionAsync()
        {
            if (_shutdown.IsRunning)
            {
                return _shutdownCompletion.Task;
            }

            if (_manager == null || !_manager.IsListening || _manager.ShutdownInProgress)
            {
                return Task.CompletedTask;
            }

            // From here on the server's disconnections are the shutdown, not drops: no grace, no lobby member removal.
            if (_manager.IsServer && _approval != null)
            {
                _approval.BeginServerStop();
            }

            // Whatever fails from here, NGO is still stopped: every BeginServerStop ends in OnServerStopped (no server left
            // listening without grace and refusing joins).
            try
            {
                float noticeSeconds = _settings != null ? _settings.HostLeaveNoticeSeconds : DefaultHostLeaveNoticeSeconds;
                int notified = _manager.IsServer && _leaveNotice != null && noticeSeconds > 0f ? _leaveNotice.SendHostLeavingNotice() : 0;
                if (!HostShutdownSequence.ShouldDefer(_manager.IsServer, notified, noticeSeconds))
                {
                    if (_manager.IsServer)
                    {
                        DisconnectRemoteClients();
                    }

                    _manager.Shutdown();
                    return Task.CompletedTask;
                }

                Debug.Log($"[{nameof(NetworkRootLifetime)}] Host leaving: {notified} client(s) notified; disconnecting them in {noticeSeconds:0.##}s.", this);
                return BeginHostShutdown(Time.realtimeSinceStartupAsDouble, noticeSeconds);
            }
            catch (Exception exception)
            {
                return AbortShutdown(exception);
            }
        }

        // A shutdown step threw: drop the graceful sequence, release its awaiters and stop NGO at once.
        internal Task AbortShutdown(Exception exception)
        {
            Debug.LogException(exception, this);
            _shutdown.Reset();
            CompleteShutdownTask();
            if (_manager != null && _manager.IsListening && !_manager.ShutdownInProgress)
            {
                _manager.Shutdown();
            }

            return Task.CompletedTask;
        }

        // Starts the graceful sequence (the notice is already sent) and tells the gameplay scene to freeze the match.
        internal Task BeginHostShutdown(double now, float noticeSeconds)
        {
            _shutdownCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _shutdown.Begin(now, noticeSeconds);
            Task task = _shutdownCompletion.Task;
            try
            {
                HostLeaveStarted?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }

            return task;
        }

        /// <summary>
        /// Reason of this pure client's drop as a known key: NGO's reason, or <see cref="DisconnectReasons.HostDisconnected"/>
        /// for a drop without one after the host announced it was leaving (see <see cref="DisconnectReasons.Resolve"/>).
        /// </summary>
        public string ResolveDisconnectReason()
        {
            string reason = _manager != null ? _manager.DisconnectReason : null;
            return DisconnectReasons.Resolve(reason, _leaveNotice != null && _leaveNotice.HostAnnouncedLeaving);
        }

        private bool HasRemoteClients()
        {
            var clients = _manager.ConnectedClientsIds;
            for (int i = 0; i < clients.Count; i++)
            {
                if (clients[i] != NetworkManager.ServerClientId)
                {
                    return true;
                }
            }

            return false;
        }

        // NGO sends the reason and closes each connection on its next late update.
        private void DisconnectRemoteClients()
        {
            if (_manager == null || !_manager.IsListening || !_manager.IsServer)
            {
                return;
            }

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

        private void CompleteShutdownTask()
        {
            TaskCompletionSource<bool> completion = _shutdownCompletion;
            _shutdownCompletion = null;
            if (completion != null)
            {
                completion.TrySetResult(true);
            }
        }

        private void FinishTeardown()
        {
            Action onTornDown = _onTornDown;
            _onTornDown = null;
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                // Edit Mode (tests): Destroy is not allowed there.
                DestroyImmediate(gameObject);
            }

            if (onTornDown == null)
            {
                return;
            }

            try
            {
                onTornDown();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private void OnValidate()
        {
            if (_anchor == null)
            {
                Debug.LogWarning($"[{nameof(NetworkRootLifetime)}] Missing NetworkRuntimeAnchor on '{name}'.", this);
            }

            if (_settings == null)
            {
                Debug.LogWarning($"[{nameof(NetworkRootLifetime)}] Missing NetworkSettings on '{name}'; a leaving host waits {DefaultHostLeaveNoticeSeconds}s after its notice.", this);
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
