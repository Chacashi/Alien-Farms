using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Networking.Data;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Online session service on the persistent NetworkRoot: answers the <see cref="OnlineSessionChannel"/> requests
    /// (sign in, create, join by code, leave) through <see cref="UgsBootstrapper"/> and <see cref="RelaySessionConnection"/>,
    /// and publishes status, code, participants (two players + spectator) and error keys on the channel. NGO is started
    /// with the same <see cref="ConnectionPayload"/> and approval as the direct connection. Leaving shuts NGO down through
    /// the root first (clients get <see cref="DisconnectReasons.HostDisconnected"/>), then leaves / deletes the session.
    /// A Leave (or the root going away) while creating / joining cancels that start; whatever it still created is left
    /// as that very instance, and the channel stays Leaving until then.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OnlineSessionService : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Environment name, protocol version and session size.")]
        private NetworkSettings _settings;

        [SerializeField, Tooltip("Channel the online menu talks to.")]
        private OnlineSessionChannel _channel;

        [SerializeField, Tooltip("Optional: Mode is set to Online while in a session.")]
        private MatchSession _session;

        [SerializeField, Tooltip("Last-resort name (truncated to 29 bytes): used when the player typed none on the channel and no UGS name is available. Tagged per MPPM instance so clones differ.")]
        private string _displayName = DisplayNameResolver.DefaultName;

        [SerializeField, Tooltip("Sign in as soon as the root starts (otherwise on the first sign-in request).")]
        private bool _signInOnStart;

        [Header("References")]
        [SerializeField, Tooltip("Root lifetime: shuts NGO down (with disconnect reasons) before leaving the session.")]
        private NetworkRootLifetime _root;

        [SerializeField, Tooltip("NetworkManager on this root.")]
        private NetworkManager _manager;

        [SerializeField, Tooltip("Sessions + Relay connection on this root.")]
        private RelaySessionConnection _connection;

        private readonly OnlineParticipant[] _participantBuffer = new OnlineParticipant[OnlineSessionChannel.MaxParticipants];

        private UgsBootstrapper _bootstrapper;
        private ISession _subscribedSession;
        private IAuthenticationService _authentication;
        private CancellationTokenSource _startCancellation;
        private int _operation;

        // Error key of this pure client being dropped by the host while in a session (null otherwise). NGO reports the
        // drop before it stops, so a root torn down in between (OnlineMatchBootstrap) still knows the host left.
        private string _droppedErrorKey;

        // How a session that ended without a Leave request is let go.
        private enum SessionExit
        {
            // Leave (client) or delete (host) the session.
            Leave,

            // The host ended it: wait for the SDK to drop it (never LeaveAsync on a deleted lobby).
            AwaitHostEnd,

            // The SDK already removed it (Deleted / RemovedFromSession): just forget it.
            Forget
        }

        /// <summary>UGS initialization and sign-in of this process.</summary>
        public UgsBootstrapper Bootstrapper => _bootstrapper;

        /// <summary>Session this machine is in (null when none).</summary>
        public ISession CurrentSession => _connection != null ? _connection.Session : null;

        private void Awake()
        {
            string environment = _settings != null ? _settings.EnvironmentName : null;
            _bootstrapper = new UgsBootstrapper(environment, UgsBootstrapper.ResolveProcessProfile());
        }

        private void OnEnable()
        {
            if (_channel == null || _manager == null || _connection == null || _root == null || _settings == null)
            {
                Debug.LogError($"[{nameof(OnlineSessionService)}] Missing channel, NetworkManager, RelaySessionConnection, NetworkRootLifetime or NetworkSettings on '{name}'; online sessions disabled.", this);
                return;
            }

            _channel.SignInRequested += HandleSignInRequested;
            _channel.CreateRequested += HandleCreateRequested;
            _channel.JoinRequested += HandleJoinRequested;
            _channel.LeaveRequested += HandleLeaveRequested;
            _channel.RejoinRequested += HandleRejoinRequested;
            _manager.OnClientStopped += HandleClientStopped;
            _manager.OnClientDisconnectCallback += HandleClientDisconnected;
            SubscribeAuthentication();

            // A root recreated after returning to the menu: the services are still signed in.
            if (_bootstrapper.IsSignedIn && _channel.Status == OnlineSessionStatus.SignedOut)
            {
                PublishSignedIn();
            }
        }

        private void Start()
        {
            if (_signInOnStart && _channel != null)
            {
                _channel.RequestSignIn();
            }
        }

        private void OnDisable()
        {
            if (_channel != null)
            {
                _channel.SignInRequested -= HandleSignInRequested;
                _channel.CreateRequested -= HandleCreateRequested;
                _channel.JoinRequested -= HandleJoinRequested;
                _channel.LeaveRequested -= HandleLeaveRequested;
                _channel.RejoinRequested -= HandleRejoinRequested;
            }

            if (_manager != null)
            {
                _manager.OnClientStopped -= HandleClientStopped;
                _manager.OnClientDisconnectCallback -= HandleClientDisconnected;
            }

            UnsubscribeAuthentication();
            UnsubscribeSession();
            _operation++;

            // Root torn down while creating / joining: the pending start sees the cancellation and leaves what it created.
            CancelStart();

            // A pure client torn down right after the host dropped it (before NGO reported it stopped).
            string droppedKey = _channel != null && _channel.Status == OnlineSessionStatus.InSession && !_channel.IsHost
                ? _droppedErrorKey
                : null;
            _droppedErrorKey = null;

            // Root torn down while in a session: end it (fire and forget; the root already stopped NGO). A dropped client
            // must not LeaveAsync the host's deleted lobby (SDK 2.4.1 would keep it registered): it waits for the SDK to
            // drop it. The next root may get a transient 'already in a session' until this completes; it waits for it.
            if (_connection != null && _connection.HasSession)
            {
                // A lost connection keeps the lobby membership (no LeaveAsync), so the player can come back from the menu.
                TryArmRejoinOffer(droppedKey);
                _ = droppedKey != null ? _connection.EndAfterHostLeftAsync() : _connection.LeaveAsync();
            }

            if (_channel != null)
            {
                if (droppedKey != null)
                {
                    // The menu shows why this client is back (the channel survives the root).
                    _channel.SetError(droppedKey);
                }

                MoveToIdle();
            }

            SetMode(GameMode.Local);
        }

        private async void HandleSignInRequested()
        {
            if (!_channel.TrySetStatus(OnlineSessionStatus.SigningIn))
            {
                return;
            }

            bool signedIn;
            try
            {
                signedIn = await _bootstrapper.EnsureSignedInAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                signedIn = false;
            }

            if (this == null || _channel.Status != OnlineSessionStatus.SigningIn)
            {
                return;
            }

            if (signedIn)
            {
                PublishSignedIn();
                return;
            }

            _channel.SetError(_bootstrapper.LastErrorKey ?? OnlineSessionErrorKeys.SignInFailed);
            _channel.TrySetStatus(OnlineSessionStatus.SignedOut);
        }

        private async void HandleCreateRequested()
        {
            if (!BeginStart(OnlineSessionStatus.Creating, out int operation, out CancellationTokenSource cancellation))
            {
                return;
            }

            RelaySessionStart start;
            try
            {
                start = await _connection.CreateAsync(_manager, cancellation.Token);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                start = RelaySessionStart.Failed(OnlineSessionErrorKeys.ConnectionFailed);
            }

            await CompleteStartAsync(operation, cancellation, start, true);
        }

        private async void HandleJoinRequested(string code)
        {
            if (!BeginStart(OnlineSessionStatus.Joining, out int operation, out CancellationTokenSource cancellation))
            {
                return;
            }

            RelaySessionStart start;
            try
            {
                start = await _connection.JoinAsync(_manager, code, cancellation.Token);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                start = RelaySessionStart.Failed(OnlineSessionErrorKeys.ConnectionFailed);
            }

            await CompleteStartAsync(operation, cancellation, start, false);
        }

        private async void HandleLeaveRequested()
        {
            OnlineSessionStatus previous = _channel.Status;
            if (!_channel.TrySetStatus(OnlineSessionStatus.Leaving))
            {
                return;
            }

            int operation = ++_operation;
            UnsubscribeSession();

            // Leaving on purpose gives up any seat still held for this player.
            ClearRejoinOffer();

            if (previous == OnlineSessionStatus.Creating || previous == OnlineSessionStatus.Joining)
            {
                // Stays Leaving: the pending start ends (cancelled or not), drops what it created and moves to Ready.
                if (_startCancellation != null)
                {
                    _startCancellation.Cancel();
                }

                return;
            }

            // Host: disconnects every client with a known reason before the session is deleted.
            _root.ShutdownSession();
            try
            {
                await _connection.LeaveAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }

            if (this == null || operation != _operation)
            {
                return;
            }

            SetMode(GameMode.Local);
            _channel.TrySetStatus(OnlineSessionStatus.Ready);
        }

        private bool BeginStart(OnlineSessionStatus status, out int operation, out CancellationTokenSource cancellation)
        {
            operation = 0;
            cancellation = null;
            if (!_bootstrapper.IsSignedIn)
            {
                // Lost the sign-in (expired, signed out elsewhere): back to SignedOut so the menu can sign in again.
                _channel.SetError(OnlineSessionErrorKeys.SignInFailed);
                _channel.TrySetStatus(OnlineSessionStatus.SignedOut);
                return false;
            }

            if (!_channel.TrySetStatus(status))
            {
                return false;
            }

            operation = ++_operation;
            _droppedErrorKey = null;
            cancellation = new CancellationTokenSource();
            _startCancellation = cancellation;
            string displayName = ResolveDisplayName();
            _connection.SetDisplayName(displayName);
            _manager.NetworkConfig.ConnectionData = BuildPayload(displayName).ToBytes();
            return true;
        }

        private async void HandleRejoinRequested()
        {
            // Read before BeginStart: the offer stays until the rejoin succeeds or a final failure drops it.
            string code = _channel.RejoinCode;
            NetworkRuntimeAnchor anchor = _root.Anchor;
            string sessionId = anchor != null ? anchor.RejoinSessionId : null;
            if (string.IsNullOrEmpty(code) || !BeginStart(OnlineSessionStatus.Joining, out int operation, out CancellationTokenSource cancellation))
            {
                return;
            }

            Debug.Log($"[{nameof(OnlineSessionService)}] Rejoining session {sessionId ?? "(unknown id)"} / {code}.", this);
            RelaySessionStart start;
            try
            {
                start = await _connection.RejoinAsync(_manager, sessionId, code, cancellation.Token);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                start = RelaySessionStart.Failed(OnlineSessionErrorKeys.ConnectionFailed);
            }

            // A cancellation (null key) or a transient failure keeps the offer for another try while it lasts.
            if (!start.IsSuccess && start.ErrorKey != null && !RejoinOfferRules.KeepsOfferAfterFailure(start.ErrorKey))
            {
                ClearRejoinOffer(anchor);
            }

            await CompleteStartAsync(operation, cancellation, start, false);
        }

        /// <summary>Drops this machine's rejoin offer (the player left on purpose or it expired).</summary>
        public void ClearRejoinOffer()
        {
            ClearRejoinOffer(_root != null ? _root.Anchor : null);
        }

        /// <summary>Host, best-effort: removes a member from the current session (see <see cref="RelaySessionConnection.TryRemovePlayerAsync"/>).</summary>
        public Task TryRemovePlayerAsync(string ugsPlayerId)
        {
            return _connection != null ? _connection.TryRemovePlayerAsync(ugsPlayerId) : Task.CompletedTask;
        }

        private void ClearRejoinOffer(NetworkRuntimeAnchor anchor)
        {
            if (anchor != null)
            {
                anchor.ClearRejoinSession();
            }

            if (_channel != null && _channel.HasRejoinOffer)
            {
                _channel.ClearRejoinOffer();
                Debug.Log($"[{nameof(OnlineSessionService)}] Rejoin offer cleared.", this);
            }
        }

        // Pure client dropped by a lost connection: remember its session so the menu can offer "Rejoin" for the grace.
        private void TryArmRejoinOffer(string errorKey)
        {
            if (_channel == null || _root == null || _settings == null || _connection == null)
            {
                return;
            }

            ISession session = _connection.Session;
            string sessionId = session != null ? session.Id : null;
            string code = session != null ? session.Code : null;
            if (!RejoinOfferRules.ShouldOffer(_channel.IsHost, errorKey, sessionId, code))
            {
                return;
            }

            // Ends RejoinOfferRules.ExpiryMarginSeconds before the server's grace, so the button never outlives the seat.
            double now = Time.realtimeSinceStartupAsDouble;
            double expiresAt = RejoinOfferRules.ExpiresAt(now, _settings.ReconnectGraceSeconds);
            NetworkRuntimeAnchor anchor = _root.Anchor;
            if (anchor != null)
            {
                anchor.SetRejoinSession(sessionId);
            }

            _channel.SetRejoinOffer(code, expiresAt);
            Debug.Log($"[{nameof(OnlineSessionService)}] Connection lost; rejoin offered for session {sessionId} / {code} ({expiresAt - now:0.#}s of {_settings.ReconnectGraceSeconds:0.#}s grace).", this);
        }

        // Only a cheap check while an offer exists.
        private void Update()
        {
            if (_channel != null && _channel.HasRejoinOffer
                && RejoinOfferRules.IsExpired(_channel.RejoinExpiresAt, Time.realtimeSinceStartupAsDouble))
            {
                ClearRejoinOffer();
            }
        }

        // Typed name, else the generated UGS name, else the serialized one tagged with this MPPM instance.
        private string ResolveDisplayName()
        {
            return DisplayNameResolver.Resolve(_channel.DisplayName, _bootstrapper.PlayerName, _displayName, _bootstrapper.Profile);
        }

        // Runs even when the root was destroyed meanwhile (this == null): a start that succeeded anyway is still left.
        private async Task CompleteStartAsync(int operation, CancellationTokenSource cancellation, RelaySessionStart start, bool asHost)
        {
            // Still the start this service waits on (a Leave while starting keeps it; OnDisable / sign-out release it).
            bool isPending = _startCancellation == cancellation;
            if (isPending)
            {
                _startCancellation = null;
            }

            bool cancelled = cancellation.IsCancellationRequested;
            cancellation.Dispose();

            if (this == null || cancelled || operation != _operation)
            {
                if (start.IsSuccess)
                {
                    await DropStartAsync(start);
                }

                if (isPending && this != null && _channel.Status == OnlineSessionStatus.Leaving)
                {
                    SetMode(GameMode.Local);
                    _channel.TrySetStatus(OnlineSessionStatus.Ready);
                }

                return;
            }

            if (!start.IsSuccess)
            {
                _channel.SetError(start.ErrorKey ?? OnlineSessionErrorKeys.ConnectionFailed);
                _channel.TrySetStatus(OnlineSessionStatus.Ready);
                return;
            }

            try
            {
                _connection.Adopt(start);
                ISession session = start.Session;

                // In a session again (rejoined or another one): an older seat offer no longer applies.
                ClearRejoinOffer();
                SetMode(GameMode.Online);
                SubscribeSession(session);
                _channel.SetSession(session.Code, session.IsHost);
                RefreshParticipants();
                _channel.TrySetStatus(OnlineSessionStatus.InSession);
                Debug.Log($"[{nameof(OnlineSessionService)}] {(asHost ? "Created" : "Joined")} session {session.Code} ({session.PlayerCount}/{session.MaxPlayers}).", this);
            }
            catch (Exception exception)
            {
                // E.g. a Changed listener threw: never keep a session the channel does not show.
                Debug.LogException(exception, this);
                await AbortStartedSessionAsync(start);
            }
        }

        // A start that finished after it was abandoned: shut down only its own NGO run, then leave that session.
        private async Task DropStartAsync(RelaySessionStart start)
        {
            if (_root != null && start.Handler != null && start.Handler.Started)
            {
                _root.ShutdownSession();
            }

            await RelaySessionConnection.LeaveAsync(start);
        }

        private async Task AbortStartedSessionAsync(RelaySessionStart start)
        {
            int operation = ++_operation;
            UnsubscribeSession();
            _connection.Release(start);
            _root.ShutdownSession();
            await RelaySessionConnection.LeaveAsync(start);

            if (this == null || operation != _operation)
            {
                return;
            }

            SetMode(GameMode.Local);
            try
            {
                _channel.SetError(OnlineSessionErrorKeys.Unknown);
                _channel.TrySetStatus(OnlineSessionStatus.Ready);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        // NGO stopped while in a session without a Leave request: a pure client was dropped (host left, kicked, network
        // lost) or the host's NGO was shut down elsewhere (dev panel). Either way the session is left.
        private void HandleClientStopped(bool wasHost)
        {
            if (_channel.Status != OnlineSessionStatus.InSession)
            {
                return;
            }

            if (wasHost || _channel.IsHost)
            {
                EndSessionRemotely(null, SessionExit.Leave);
                return;
            }

            // A pure client only stops on its own through a Leave (status Leaving) or a root teardown (unsubscribed):
            // here the host ended the session (left, kicked this client) or the connection was lost.
            string key = _droppedErrorKey ?? MapDisconnectReason();
            _droppedErrorKey = null;

            // Lost connection: the lobby membership is kept (AwaitHostEnd never calls LeaveAsync), so it can rejoin.
            TryArmRejoinOffer(key);
            EndSessionRemotely(key, SessionExit.AwaitHostEnd);
        }

        // NGO reports the local client's drop by the server before shutting down; a self-initiated shutdown does not.
        private void HandleClientDisconnected(ulong clientId)
        {
            if (_manager.IsServer || clientId != _manager.LocalClientId || _manager.ShutdownInProgress
                || _channel.Status != OnlineSessionStatus.InSession || _channel.IsHost)
            {
                return;
            }

            _droppedErrorKey = MapDisconnectReason();
        }

        private string MapDisconnectReason()
        {
            // A drop without a game reason normalizes to connection_lost; an explicit host shutdown sends host_disconnected.
            return OnlineSessionErrorMapper.FromDisconnectReason(DisconnectReasons.Normalize(_manager.DisconnectReason))
                ?? OnlineSessionErrorKeys.ConnectionLost;
        }

        private void HandleSessionRemoved()
        {
            EndSessionRemotely(OnlineSessionErrorKeys.HostLeft, SessionExit.Forget);
        }

        private void HandleSessionChanged()
        {
            ISession session = _subscribedSession;
            if (session == null)
            {
                return;
            }

            _channel.SetSession(session.Code, session.IsHost);
            RefreshParticipants();
        }

        private void HandleHostChanged(string hostId)
        {
            HandleSessionChanged();
        }

        private void HandlePlayerChanged(string playerId)
        {
            RefreshParticipants();
        }

        // Host: a member that left the session no longer gets its reserved seat back (backup of the leave notice).
        private void HandlePlayerLeft(string playerId)
        {
            RefreshParticipants();
            ConnectionApprovalHandler approval = _root.Approval;
            if (_channel.IsHost && approval != null)
            {
                approval.ReleaseLeftMember(playerId);
            }
        }

        private void HandlePlayerPropertiesChanged()
        {
            RefreshParticipants();
        }

        // Signed out or the session token expired: the session cannot be kept; the menu has to sign in again.
        private void HandleSignedOut()
        {
            if (_channel.Status == OnlineSessionStatus.SignedOut)
            {
                return;
            }

            _operation++;
            CancelStart();
            UnsubscribeSession();
            SetMode(GameMode.Local);
            _channel.SetError(OnlineSessionErrorKeys.SignInFailed);
            _channel.TrySetStatus(OnlineSessionStatus.SignedOut);

            if (_connection.HasSession)
            {
                _root.ShutdownSession();
                _ = _connection.LeaveAsync();
            }
        }

        private async void EndSessionRemotely(string errorKey, SessionExit exit)
        {
            if (_channel.Status != OnlineSessionStatus.InSession)
            {
                return;
            }

            int operation = ++_operation;
            _droppedErrorKey = null;
            UnsubscribeSession();
            if (errorKey != null)
            {
                _channel.SetError(errorKey);
            }

            _channel.TrySetStatus(OnlineSessionStatus.Leaving);
            _root.ShutdownSession();

            switch (exit)
            {
                case SessionExit.Leave:
                    await _connection.LeaveAsync();
                    break;
                case SessionExit.AwaitHostEnd:
                    await _connection.EndAfterHostLeftAsync();
                    break;
                default:
                    _connection.ForgetSession();
                    break;
            }

            if (this == null || operation != _operation)
            {
                return;
            }

            SetMode(GameMode.Local);
            _channel.TrySetStatus(OnlineSessionStatus.Ready);
        }

        private void RefreshParticipants()
        {
            ISession session = _subscribedSession;
            if (session == null)
            {
                _channel.SetParticipants(_participantBuffer, 0);
                return;
            }

            IReadOnlyList<IReadOnlyPlayer> players = session.Players;
            string localId = _bootstrapper.PlayerId;
            int count = 0;

            // Host first (seat P1), then the rest in join order; past both seats is a spectator.
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < players.Count && count < _participantBuffer.Length; i++)
                {
                    IReadOnlyPlayer player = players[i];
                    bool isHost = string.Equals(player.Id, session.Host, StringComparison.Ordinal);
                    if (isHost != (pass == 0))
                    {
                        continue;
                    }

                    _participantBuffer[count] = new OnlineParticipant(
                        player.Id,
                        ReadDisplayName(player),
                        isHost,
                        string.Equals(player.Id, localId, StringComparison.Ordinal),
                        count >= ServerSlotDirectory.Capacity);
                    count++;
                }
            }

            _channel.SetParticipants(_participantBuffer, count);
        }

        private static string ReadDisplayName(IReadOnlyPlayer player)
        {
            if (player.Properties != null
                && player.Properties.TryGetValue(RelaySessionConnection.DisplayNameProperty, out PlayerProperty property)
                && property != null
                && !string.IsNullOrWhiteSpace(property.Value))
            {
                // Any member writes its own property: clean it before the menu shows it (rich text, control chars).
                string name = DisplayNameSanitizer.Sanitize(property.Value);
                if (name.Length > 0)
                {
                    return name;
                }
            }

            return player.Id;
        }

        private void PublishSignedIn()
        {
            if (_channel.Status == OnlineSessionStatus.SignedOut)
            {
                _channel.TrySetStatus(OnlineSessionStatus.SigningIn);
            }

            SubscribeAuthentication();
            _channel.SetLocalPlayerId(_bootstrapper.PlayerId);
            _channel.TrySetStatus(OnlineSessionStatus.Ready);
        }

        // Ready when still signed in, SignedOut otherwise: the channel is never left Ready without a sign-in.
        private void MoveToIdle()
        {
            OnlineSessionStatus idle = _bootstrapper != null && _bootstrapper.IsSignedIn
                ? OnlineSessionStatus.Ready
                : OnlineSessionStatus.SignedOut;
            if (_channel.Status != idle)
            {
                _channel.TrySetStatus(idle);
            }
        }

        private void CancelStart()
        {
            CancellationTokenSource cancellation = _startCancellation;
            _startCancellation = null;
            if (cancellation != null)
            {
                cancellation.Cancel();
            }
        }

        // The character is picked inside the room (NetworkLobbyState); the payload only identifies the player.
        private ConnectionPayload BuildPayload(string displayName)
        {
            return ConnectionPayload.Create(_settings.ProtocolVersion, displayName, _bootstrapper.PlayerId);
        }

        private void SubscribeAuthentication()
        {
            if (_authentication != null || _bootstrapper == null)
            {
                return;
            }

            _authentication = _bootstrapper.Authentication;
            if (_authentication == null)
            {
                return;
            }

            _authentication.SignedOut += HandleSignedOut;
            _authentication.Expired += HandleSignedOut;
        }

        private void UnsubscribeAuthentication()
        {
            IAuthenticationService authentication = _authentication;
            _authentication = null;
            if (authentication == null)
            {
                return;
            }

            authentication.SignedOut -= HandleSignedOut;
            authentication.Expired -= HandleSignedOut;
        }

        private void SubscribeSession(ISession session)
        {
            UnsubscribeSession();
            _subscribedSession = session;
            if (session == null)
            {
                return;
            }

            session.Changed += HandleSessionChanged;
            session.SessionHostChanged += HandleHostChanged;
            session.PlayerPropertiesChanged += HandlePlayerPropertiesChanged;
            session.PlayerJoined += HandlePlayerChanged;
            session.PlayerHasLeft += HandlePlayerLeft;
            session.RemovedFromSession += HandleSessionRemoved;
            session.Deleted += HandleSessionRemoved;
        }

        private void UnsubscribeSession()
        {
            ISession session = _subscribedSession;
            _subscribedSession = null;
            if (session == null)
            {
                return;
            }

            session.Changed -= HandleSessionChanged;
            session.SessionHostChanged -= HandleHostChanged;
            session.PlayerPropertiesChanged -= HandlePlayerPropertiesChanged;
            session.PlayerJoined -= HandlePlayerChanged;
            session.PlayerHasLeft -= HandlePlayerLeft;
            session.RemovedFromSession -= HandleSessionRemoved;
            session.Deleted -= HandleSessionRemoved;
        }

        private void SetMode(GameMode mode)
        {
            if (_session != null)
            {
                _session.SetMode(mode);
            }
        }

        private void OnValidate()
        {
            if (_manager == null)
            {
                TryGetComponent(out _manager);
            }

            if (_root == null)
            {
                TryGetComponent(out _root);
            }

            if (_connection == null)
            {
                TryGetComponent(out _connection);
            }

            if (_settings == null)
            {
                Debug.LogWarning($"[{nameof(OnlineSessionService)}] Missing NetworkSettings on '{name}'.", this);
            }

            if (_channel == null)
            {
                Debug.LogWarning($"[{nameof(OnlineSessionService)}] Missing OnlineSessionChannel on '{name}'.", this);
            }
        }
    }
}
