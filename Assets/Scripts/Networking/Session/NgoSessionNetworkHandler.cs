using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Sessions network handler for this project's NetworkManager: replaces the SDK's default NGO integration (which
    /// uses <c>NetworkManager.Singleton</c> and warns when NGO is shut down outside the session) so the injected root's
    /// manager and transport are used, the connection payload/approval written by the caller stay in charge, and the
    /// root can shut NGO down itself (host disconnect reasons). One instance per session start; it only ever shuts down
    /// an NGO run it started itself, so a stale handler never stops a newer session.
    /// <para>
    /// Single use: once a start failed, was cancelled, or NGO was stopped, the handler is <see cref="Retired"/> and any
    /// later <see cref="StartAsync(NetworkConfiguration, CancellationToken)"/> returns without starting NGO. SDK 2.x
    /// (NetworkModule) clears its network property when a client join fails and, on the next <c>ISession.Changed</c>
    /// raised while the failed session is torn down, joins the network again with this same handler from an
    /// <c>async void</c>: that start must neither run (ghost connection to the host) nor throw (unobserved exception).
    /// </para>
    /// </summary>
    internal sealed class NgoSessionNetworkHandler : INetworkHandler
    {
        private readonly NetworkManager _manager;
        private readonly UnityTransport _transport;
        private readonly float _connectTimeoutSeconds;

        private string _disconnectReason;
        private bool _started;
        private bool _retired;
        private bool _retiredStartLogged;

        public NgoSessionNetworkHandler(NetworkManager manager, UnityTransport transport, float connectTimeoutSeconds)
        {
            _manager = manager;
            _transport = transport;
            _connectTimeoutSeconds = connectTimeoutSeconds;
        }

        /// <summary>NGO reason received when a client start was rejected or dropped while connecting (null when none).</summary>
        public string DisconnectReason => _disconnectReason;

        /// <summary>True from a successful StartHost / StartClient of this handler until it shuts NGO down.</summary>
        public bool Started => _started;

        /// <summary>True once this handler's start failed or was cancelled, or its NGO run was stopped: it never starts NGO again.</summary>
        public bool Retired => _retired;

        public Task StartAsync(NetworkConfiguration configuration)
        {
            return StartAsync(configuration, CancellationToken.None);
        }

        public async Task StartAsync(NetworkConfiguration configuration, CancellationToken cancellationToken)
        {
            if (_retired)
            {
                // Completes without error: a throw here would end in the SDK's async void (unobserved exception).
                LogRetiredStartOnce(configuration);
                return;
            }

            try
            {
                await StartCoreAsync(configuration, cancellationToken);
            }
            catch
            {
                Retire();
                throw;
            }
        }

        /// <summary>Marks the handler as used up: later starts are ignored (see <see cref="Retired"/>). Idempotent.</summary>
        public void Retire()
        {
            _retired = true;
        }

        private async Task StartCoreAsync(NetworkConfiguration configuration, CancellationToken cancellationToken)
        {
            if (_manager == null || _transport == null)
            {
                throw new SessionException("No NetworkManager or UnityTransport for the session.", SessionError.NetworkManagerNotInitialized, null);
            }

            if (configuration.Type != NetworkType.Relay)
            {
                throw new SessionException($"Unsupported session network type {configuration.Type}.", SessionError.InvalidNetworkConfig, null);
            }

            if (_manager.IsListening || _manager.ShutdownInProgress)
            {
                throw new SessionException("NetworkManager is already running or shutting down.", SessionError.NetworkManagerStartFailed, null);
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Relay data is delivered in RelayServerData for both roles (the client's join allocation included).
            _transport.SetRelayServerData(configuration.RelayServerData);
            _disconnectReason = null;

            // A relay session is always hosted by a player: a server role is started as host as well.
            if (configuration.Role != NetworkRole.Client)
            {
                if (!_manager.StartHost())
                {
                    throw new SessionException("NetworkManager could not start as host.", SessionError.NetworkManagerStartFailed, null);
                }

                MarkStarted();
                return;
            }

            _manager.OnClientDisconnectCallback += HandleClientDisconnected;
            try
            {
                if (!_manager.StartClient())
                {
                    throw new SessionException("NetworkManager could not start as client.", SessionError.NetworkManagerStartFailed, null);
                }

                MarkStarted();
                await WaitForClientConnectionAsync(cancellationToken);
            }
            finally
            {
                _manager.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }

        public async Task StopAsync()
        {
            // Shutdown retires the handler even when it never started NGO (the SDK resets the network after a failure).
            if (!Shutdown())
            {
                return;
            }

            // Shutdown completes over the next network updates.
            float deadline = Time.realtimeSinceStartup + _connectTimeoutSeconds;
            while (_manager != null && _manager.IsListening && Application.isPlaying && Time.realtimeSinceStartup < deadline)
            {
                await Task.Yield();
            }
        }

        /// <summary>
        /// Shuts NGO down if this handler started it (synchronous part of <see cref="StopAsync"/>). False when this
        /// handler never started NGO (or already stopped it): the manager is then left alone. Always retires the handler.
        /// </summary>
        public bool Shutdown()
        {
            Retire();
            if (!_started)
            {
                return false;
            }

            MarkStopped();
            if (_manager == null)
            {
                return false;
            }

            if (_manager.IsListening && !_manager.ShutdownInProgress)
            {
                _manager.Shutdown();
            }

            return true;
        }

        // Completes once the client is connected and synchronized (approval passed); throws when it stops or times out.
        private async Task WaitForClientConnectionAsync(CancellationToken cancellationToken)
        {
            float deadline = Time.realtimeSinceStartup + _connectTimeoutSeconds;
            while (true)
            {
                if (_manager == null || !Application.isPlaying)
                {
                    throw new OperationCanceledException("Play mode ended while connecting.");
                }

                if (_manager.IsConnectedClient)
                {
                    return;
                }

                if (!_manager.IsListening)
                {
                    // The disconnect callback may not have run (stopped before connecting): read the reason directly.
                    _disconnectReason ??= _manager.DisconnectReason;
                    MarkStopped();
                    throw new SessionException($"Client connection ended: {_disconnectReason ?? "no reason"}.", SessionError.NetworkManagerStartFailed, null);
                }

                if (cancellationToken.IsCancellationRequested || Time.realtimeSinceStartup >= deadline)
                {
                    Shutdown();
                    cancellationToken.ThrowIfCancellationRequested();
                    throw new SessionException("Timed out connecting to the host.", SessionError.NetworkManagerStartFailed, null);
                }

                await Task.Yield();
            }
        }

        private void MarkStarted()
        {
            _started = true;
            _manager.OnClientStopped += HandleClientStopped;
        }

        private void MarkStopped()
        {
            _started = false;
            if (_manager != null)
            {
                _manager.OnClientStopped -= HandleClientStopped;
            }
        }

        // NGO stopped from outside (root shutdown, dropped client): this run is over, so a later Stop leaves NGO alone
        // and a later start of the SDK is ignored.
        private void HandleClientStopped(bool wasHost)
        {
            MarkStopped();
            Retire();
        }

        private void LogRetiredStartOnce(NetworkConfiguration configuration)
        {
            if (_retiredStartLogged)
            {
                return;
            }

            _retiredStartLogged = true;
            string role = configuration != null ? configuration.Role.ToString() : "unknown";
            Debug.Log($"[{nameof(NgoSessionNetworkHandler)}] Ignoring a network start ({role}) of a session whose start already failed or ended.");
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (_manager != null && clientId == _manager.LocalClientId)
            {
                _disconnectReason = _manager.DisconnectReason;
            }
        }
    }
}
