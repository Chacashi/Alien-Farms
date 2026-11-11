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
    /// </summary>
    internal sealed class NgoSessionNetworkHandler : INetworkHandler
    {
        private readonly NetworkManager _manager;
        private readonly UnityTransport _transport;
        private readonly float _connectTimeoutSeconds;

        private string _disconnectReason;
        private bool _started;

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

        public Task StartAsync(NetworkConfiguration configuration)
        {
            return StartAsync(configuration, CancellationToken.None);
        }

        public async Task StartAsync(NetworkConfiguration configuration, CancellationToken cancellationToken)
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
        /// handler never started NGO (or already stopped it): the manager is then left alone.
        /// </summary>
        public bool Shutdown()
        {
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

        // NGO stopped from outside (root shutdown, dropped client): this run is over, so a later Stop leaves NGO alone.
        private void HandleClientStopped(bool wasHost)
        {
            MarkStopped();
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
