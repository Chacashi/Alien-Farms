using System;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Networking.Data;
using AlienFarms.Networking.Session;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Diagnostics
{
    /// <summary>
    /// Development connection panel (logic only; the Canvas lives in the scene). Host / Client / Shutdown are wired to
    /// buttons' OnClick. Host and Client switch the session to online and start the root's connection strategy (direct
    /// IP); the host then loads the gameplay scene for everyone. The status label is rebuilt only when the state changes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DevConnectionPanel : MonoBehaviour
    {
        private enum Status
        {
            NoRoot = 0,
            Offline = 1,
            Starting = 2,
            Hosting = 3,
            Connecting = 4,
            Connected = 5
        }

        [Header("Config")]
        [SerializeField, Tooltip("Runtime anchor of the NetworkRoot.")]
        private NetworkRuntimeAnchor _anchor;

        [SerializeField, Tooltip("Runtime session: Mode is set to Online here.")]
        private MatchSession _session;

        [SerializeField, Tooltip("Protocol version written in the connection payload.")]
        private NetworkSettings _settings;

        [SerializeField, Tooltip("Name sent in the payload (truncated to 29 bytes).")]
        private string _displayName = "Player";

        [Header("References")]
        [SerializeField, Tooltip("Status label.")]
        private TMP_Text _statusLabel;

        [SerializeField, Tooltip("Optional address field read by Client (empty = the DirectConnection default).")]
        private TMP_InputField _addressField;

        [Header("Online session (Sessions + Relay)")]
        [SerializeField, Tooltip("Optional: channel of the root's OnlineSessionService; enables Sign in / Create / Join / Leave / Start match.")]
        private OnlineSessionChannel _sessionChannel;

        [SerializeField, Tooltip("Optional join code field read by JoinSession.")]
        private TMP_InputField _codeField;

        [SerializeField, Tooltip("Optional: online room channel. When a room is listening, Start match goes through it (picks validated, characters carried to the match).")]
        private OnlineLobbyChannel _lobbyChannel;

        private NetworkManager _subscribedManager;
        private Status _shownStatus = (Status)(-1);
        private int _shownPlayers = -1;
        private string _lastError;
        private bool _errorDirty;
        private bool _busy;

        private void OnEnable()
        {
            // Why the last session dropped this machine (host left, rejected...), shown once.
            string reason = _anchor != null ? _anchor.ConsumeDisconnectReason() : null;
            if (!string.IsNullOrEmpty(reason))
            {
                _lastError = $"Disconnected: {reason}";
                _errorDirty = true;
            }

            if (_sessionChannel != null)
            {
                _sessionChannel.Changed += HandleSessionChannelChanged;
                _errorDirty = true;
            }
        }

        private void OnDisable()
        {
            SubscribeTo(null);
            if (_sessionChannel != null)
            {
                _sessionChannel.Changed -= HandleSessionChannelChanged;
            }
        }

        /// <summary>Button: signs in to Unity Gaming Services (anonymous, one profile per MPPM instance).</summary>
        public void SignIn()
        {
            if (_sessionChannel != null)
            {
                _sessionChannel.RequestSignIn();
            }
        }

        /// <summary>Button: creates a Sessions + Relay session; its code appears in the status label.</summary>
        public void CreateSession()
        {
            if (_sessionChannel != null)
            {
                _sessionChannel.RequestCreate();
            }
        }

        /// <summary>Button: joins the session whose code is typed in the code field.</summary>
        public void JoinSession()
        {
            if (_sessionChannel != null)
            {
                _sessionChannel.RequestJoin(_codeField != null ? _codeField.text : null);
            }
        }

        /// <summary>Button: leaves (client) or closes (host) the current session.</summary>
        public void LeaveSession()
        {
            if (_sessionChannel != null)
            {
                _sessionChannel.RequestLeave();
            }
        }

        /// <summary>
        /// Button (session host): starts the match through the online room when one is listening (it validates the
        /// picks and loads the gameplay scene); otherwise loads the gameplay scene for everyone directly.
        /// </summary>
        public void StartSessionMatch()
        {
            if (_sessionChannel == null || !_sessionChannel.IsHost || _sessionChannel.Status != OnlineSessionStatus.InSession)
            {
                ShowError("Not hosting a session.");
                return;
            }

            if (_lobbyChannel != null)
            {
                if (_lobbyChannel.RequestStart())
                {
                    if (_lobbyChannel.HasReject)
                    {
                        ShowError($"Room refused to start: {_lobbyChannel.LastRejectKey}");
                    }

                    return;
                }

                // No room listening (e.g. a scene without LobbyStateSpawner): fall back to the direct load.
                _lobbyChannel.ClearReject();
            }

            NetworkSceneLoader loader = _anchor != null ? _anchor.SceneLoader : null;
            if (loader == null || !loader.LoadGameplay())
            {
                ShowError("The gameplay scene could not be loaded.");
            }
        }

        private void HandleSessionChannelChanged()
        {
            _errorDirty = true;
        }

        private void Update()
        {
            NetworkManager manager = _anchor != null ? _anchor.Manager : null;
            if (manager != _subscribedManager)
            {
                SubscribeTo(manager);
            }

            Status status = Evaluate(manager, out int players);
            if (status != _shownStatus || players != _shownPlayers || _errorDirty)
            {
                _shownStatus = status;
                _shownPlayers = players;
                _errorDirty = false;
                RefreshLabel();
            }
        }

        /// <summary>Button: start as host (direct connection) and load the gameplay scene.</summary>
        public async void Host()
        {
            if (!TryBegin(out NetworkManager manager, out IConnectionStrategy connection))
            {
                return;
            }

            try
            {
                bool started = await connection.StartHostAsync(manager);
                if (!started)
                {
                    Fail("Could not start the host.");
                    return;
                }

                NetworkSceneLoader loader = _anchor.SceneLoader;
                if (loader == null || !loader.LoadGameplay())
                {
                    // Never leave a host running in the menu without a match.
                    manager.Shutdown();
                    Fail("Host started but the gameplay scene could not be loaded; host stopped.");
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                Fail(exception.Message);
            }
            finally
            {
                _busy = false;
            }
        }

        /// <summary>Button: connect as client (direct connection); the host's scene loads on synchronization.</summary>
        public async void Client()
        {
            if (!TryBegin(out NetworkManager manager, out IConnectionStrategy connection))
            {
                return;
            }

            if (_addressField != null && connection is DirectConnection direct)
            {
                direct.SetAddress(_addressField.text);
            }

            try
            {
                if (!await connection.StartClientAsync(manager))
                {
                    Fail("Could not start the client.");
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                Fail(exception.Message);
            }
            finally
            {
                _busy = false;
            }
        }

        /// <summary>
        /// Button: ends the session. In the gameplay scene the NetworkRoot is torn down and the MainMenu loaded (its own
        /// root comes back with it); in the menu only the NetworkManager stops.
        /// </summary>
        public void Shutdown()
        {
            if (_session != null)
            {
                _session.SetMode(GameMode.Local);
            }

            NetworkRootLifetime root = _anchor != null ? _anchor.Root : null;
            if (root == null)
            {
                return;
            }

            if (gameObject.scene.name == SceneNames.MainMenu)
            {
                root.ShutdownSession();
                return;
            }

            root.Teardown();
            SceneFlow.LoadMainMenu();
        }

        private bool TryBegin(out NetworkManager manager, out IConnectionStrategy connection)
        {
            manager = _anchor != null ? _anchor.Manager : null;
            connection = _anchor != null ? _anchor.Connection : null;

            if (_busy)
            {
                return false;
            }

            if (manager == null || connection == null || _settings == null)
            {
                Fail("No NetworkRoot, connection strategy or NetworkSettings.");
                return false;
            }

            if (manager.IsListening || manager.ShutdownInProgress)
            {
                Fail("Already running; shut down first.");
                return false;
            }

            _busy = true;
            _lastError = null;
            _errorDirty = true;

            // A failure of an earlier online attempt no longer applies to this direct start.
            if (_sessionChannel != null && _sessionChannel.HasError)
            {
                _sessionChannel.ClearError();
            }

            if (_session != null)
            {
                _session.SetMode(GameMode.Online);
            }

            manager.NetworkConfig.ConnectionData = BuildPayload().ToBytes();
            return true;
        }

        // Direct dev connection: no UGS player id (no reconnection by id); the roster gives each seat its default character.
        private ConnectionPayload BuildPayload()
        {
            return ConnectionPayload.Create(_settings.ProtocolVersion, _displayName);
        }

        private void Fail(string message)
        {
            if (_session != null)
            {
                _session.SetMode(GameMode.Local);
            }

            ShowError(message);
        }

        // Shows a failure without touching the session mode (the online session owns it while in one).
        private void ShowError(string message)
        {
            _lastError = message;
            _errorDirty = true;
            Debug.LogWarning($"[{nameof(DevConnectionPanel)}] {message}", this);
        }

        private Status Evaluate(NetworkManager manager, out int players)
        {
            players = 0;
            if (manager == null)
            {
                return Status.NoRoot;
            }

            if (_busy)
            {
                return Status.Starting;
            }

            if (!manager.IsListening)
            {
                return Status.Offline;
            }

            if (manager.IsServer)
            {
                players = manager.ConnectedClientsIds.Count;
                return Status.Hosting;
            }

            return manager.IsConnectedClient ? Status.Connected : Status.Connecting;
        }

        private void RefreshLabel()
        {
            if (_statusLabel == null)
            {
                return;
            }

            string text;
            switch (_shownStatus)
            {
                case Status.NoRoot:
                    text = "No NetworkRoot";
                    break;
                case Status.Starting:
                    text = "Starting...";
                    break;
                case Status.Hosting:
                    text = $"Host · {_shownPlayers} connected";
                    break;
                case Status.Connecting:
                    text = "Connecting...";
                    break;
                case Status.Connected:
                    text = "Connected as client";
                    break;
                default:
                    text = "Offline";
                    break;
            }

            if (!string.IsNullOrEmpty(_lastError))
            {
                text = $"{text}\n{_lastError}";
            }

            if (_sessionChannel != null)
            {
                text = $"{text}\n{BuildSessionLine()}";
            }

            _statusLabel.text = text;
        }

        // Rebuilt only on change (see HandleSessionChannelChanged).
        private string BuildSessionLine()
        {
            var builder = new System.Text.StringBuilder(128);
            builder.Append("Online: ").Append(_sessionChannel.Status);
            if (!string.IsNullOrEmpty(_sessionChannel.LocalPlayerId))
            {
                builder.Append(" · id ").Append(_sessionChannel.LocalPlayerId);
            }

            if (!string.IsNullOrEmpty(_sessionChannel.Code))
            {
                builder.Append(_sessionChannel.IsHost ? "\nHosting code " : "\nJoined code ").Append(_sessionChannel.Code);
            }

            for (int i = 0; i < _sessionChannel.ParticipantCount; i++)
            {
                OnlineParticipant participant = _sessionChannel.GetParticipant(i);
                builder.Append("\n - ").Append(participant.DisplayName);
                if (participant.IsHost)
                {
                    builder.Append(" (host)");
                }

                if (participant.IsSpectator)
                {
                    builder.Append(" (spectator)");
                }

                if (participant.IsLocal)
                {
                    builder.Append(" (you)");
                }
            }

            if (_sessionChannel.HasError)
            {
                builder.Append('\n').Append(_sessionChannel.LastErrorKey);
            }

            return builder.ToString();
        }

        private void SubscribeTo(NetworkManager manager)
        {
            if (_subscribedManager != null)
            {
                _subscribedManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            }

            _subscribedManager = manager;

            if (_subscribedManager != null)
            {
                _subscribedManager.OnClientDisconnectCallback += HandleClientDisconnected;
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            NetworkManager manager = _subscribedManager;
            if (manager == null || manager.IsServer || clientId != manager.LocalClientId)
            {
                return;
            }

            // Pure client dropped or rejected: show why (approval reasons are stable keys).
            // NGO puts raw transport text here when the server gave no reason: only known keys are shown.
            _lastError = $"Disconnected: {DisconnectReasons.Normalize(manager.DisconnectReason)}";
            _errorDirty = true;
            if (_session != null)
            {
                _session.SetMode(GameMode.Local);
            }
        }

        private void OnValidate()
        {
            if (_anchor == null)
            {
                Debug.LogWarning($"[{nameof(DevConnectionPanel)}] Missing NetworkRuntimeAnchor on '{name}'.", this);
            }

            if (_session == null)
            {
                Debug.LogWarning($"[{nameof(DevConnectionPanel)}] Missing MatchSession on '{name}'.", this);
            }

            if (_settings == null)
            {
                Debug.LogWarning($"[{nameof(DevConnectionPanel)}] Missing NetworkSettings on '{name}'.", this);
            }

            if (_statusLabel == null)
            {
                Debug.LogWarning($"[{nameof(DevConnectionPanel)}] Missing status label on '{name}'.", this);
            }
        }
    }
}
