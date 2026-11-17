using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AlienFarms.Core;
using AlienFarms.Data;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Connection through Unity Sessions + Relay: the host creates a private session with a Relay network (join code
    /// shown to the other player) and the client joins it by code. NGO is started by <see cref="NgoSessionNetworkHandler"/>
    /// on the root's own NetworkManager, after the caller wrote <see cref="NetworkConfig.ConnectionData"/>, so payload
    /// and approval work exactly as with <see cref="DirectConnection"/>. Requires a signed-in player
    /// (<see cref="UgsBootstrapper"/>). Each create / join returns its own <see cref="RelaySessionStart"/>; the caller
    /// <see cref="Adopt"/>s it as the current session or leaves that instance with <see cref="LeaveAsync(RelaySessionStart)"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RelaySessionConnection : MonoBehaviour, IConnectionStrategy
    {
        /// <summary>Player property holding the display name shown in the participant list (member-visible).</summary>
        public const string DisplayNameProperty = "displayName";

        // One session at a time: a constant type makes the SDK refuse a second create/join instead of leaking one.
        private const string SessionType = "alien-farms-match";

        // Bounded waits of the cleanup paths (a previous end still in flight, the SDK unregistering an orphan).
        private const float PendingEndWaitSeconds = 5f;
        private const float OrphanReleaseTimeoutSeconds = 5f;

        // Rejoin: how long a session handed back by ReconnectToSessionAsync may take to start NGO at all (R1), and how
        // long the fallback join waits for NGO to finish stopping after a failed attempt.
        private const float ReconnectStartWaitSeconds = 3f;
        private const float ManagerStopWaitSeconds = 3f;

        // The connect wait of a started reconnection always outlasts NGO's own pending-client timeout by this much.
        private const float ReconnectBufferMarginSeconds = 2f;

        // Leaves / remote ends still running, by session: a later start waits for them before freeing the type.
        private static readonly Dictionary<ISession, Task> s_pendingEnds = new Dictionary<ISession, Task>();

        // Creates / joins still running on any root (main thread only): a session they registered is not an orphan.
        private static int s_startsInFlight;

        [Header("Config")]
        [SerializeField, Tooltip("Max players (session size) and spectator rule.")]
        private NetworkSettings _settings;

        [Header("References")]
        [SerializeField, Tooltip("UnityTransport on this root (configured with the Relay allocation).")]
        private UnityTransport _transport;

        [Header("Relay")]
        [SerializeField, Tooltip("Relay region (empty = automatic, best latency).")]
        private string _region = string.Empty;

        [SerializeField, Range(5f, 60f), Tooltip("Seconds a client waits for NGO approval and synchronization before giving up.")]
        private float _connectTimeoutSeconds = 20f;

        [SerializeField, Range(0f, 5f), Tooltip("Retry once a create/join refused as 'already in a session': a previous leave still in flight is awaited and an orphan left by the SDK released; this delay is used when nothing is registered anymore. 0 = no retry.")]
        private float _alreadyInSessionRetrySeconds = 1f;

        [SerializeField, Range(0f, 10f), Tooltip("Seconds a client dropped by the host waits for the session's Deleted / RemovedFromSession before releasing it locally (it never calls LeaveAsync on a deleted lobby).")]
        private float _hostLeftWaitSeconds = 3f;

        [SerializeField, Range(5f, 60f), Tooltip("Seconds a reconnection whose NGO client already started may take to be approved and synchronized. At runtime it is never below the NetworkManager's Client Connection Buffer Timeout plus a margin, so NGO's own answer arrives first.")]
        private float _reconnectConnectWaitSeconds = 15f;

        private string _joinCode;
        private string _displayName = string.Empty;
        private RelaySessionStart _current;
        private int _startsInFlight;
        private Task _refresh;

        /// <summary>Session this machine is in (null when none).</summary>
        public ISession Session => _current?.Session;

        /// <summary>Error key (<see cref="OnlineSessionErrorKeys"/>) of the last failed <see cref="IConnectionStrategy"/> start; null after a success.</summary>
        public string LastErrorKey { get; private set; }

        public bool HasSession => _current != null;

        /// <summary>
        /// In a UGS session, still creating / joining one, or running NGO over Relay. Between a host's start and its
        /// adoption (<see cref="Session"/> still null) clients may already connect through its Relay allocation, so a
        /// Relay transport alone counts as online (a direct connection sets the plain protocol again).
        /// </summary>
        public bool IsOnline => _current != null
            || _startsInFlight > 0
            || (_transport != null && _transport.Protocol == UnityTransport.ProtocolType.RelayUnityTransport);

        /// <summary>
        /// <paramref name="ugsPlayerId"/> is in the current session's player list, as last received by this machine (it
        /// may lag behind a join: see <see cref="RefreshSessionAsync"/>). False without a session.
        /// </summary>
        public bool IsSessionMember(string ugsPlayerId)
        {
            ISession session = Session;
            if (session == null || string.IsNullOrEmpty(ugsPlayerId))
            {
                return false;
            }

            IReadOnlyList<IReadOnlyPlayer> players = session.Players;
            if (players == null)
            {
                return false;
            }

            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] != null && string.Equals(players[i].Id, ugsPlayerId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Fetches the current session from the service (its player list included). Concurrent calls share one request
        /// (the lobby read is rate limited). Never throws: a failure is logged and the local copy stays as it was.
        /// </summary>
        public Task RefreshSessionAsync()
        {
            if (_refresh != null && !_refresh.IsCompleted)
            {
                return _refresh;
            }

            ISession session = Session;
            if (session == null)
            {
                return Task.CompletedTask;
            }

            _refresh = RefreshCoreAsync(session);
            return _refresh;
        }

        private static async Task RefreshCoreAsync(ISession session)
        {
            try
            {
                await session.RefreshAsync();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[{nameof(RelaySessionConnection)}] Session refresh failed: {exception.Message}");
            }
        }

        /// <summary>Code the next <see cref="StartClientAsync"/> joins (normalized by the caller).</summary>
        public void SetJoinCode(string code)
        {
            _joinCode = code;
        }

        /// <summary>Name published as a member-visible player property of the next create/join.</summary>
        public void SetDisplayName(string displayName)
        {
            _displayName = displayName ?? string.Empty;
        }

        /// <summary>Creates a private Relay session hosted by this machine. Never throws; cancellation returns a failure without error key.</summary>
        internal Task<RelaySessionStart> CreateAsync(NetworkManager manager, CancellationToken cancellationToken)
        {
            return StartAsync(manager, null, cancellationToken);
        }

        /// <summary>Joins the session with <paramref name="code"/>. Never throws; cancellation returns a failure without error key.</summary>
        internal Task<RelaySessionStart> JoinAsync(NetworkManager manager, string code, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(code))
            {
                return Task.FromResult(RelaySessionStart.Failed(OnlineSessionErrorKeys.InvalidCode));
            }

            return StartAsync(manager, code, cancellationToken);
        }

        /// <summary>Id of the current session (null when none): what a dropped client reconnects to.</summary>
        public string SessionId => Session?.Id;

        /// <summary>
        /// Reconnects a dropped client to its session. First <c>ReconnectToSessionAsync(sessionId)</c> (still a member of
        /// the lobby; the SDK starts the Relay network through a fresh <see cref="NgoSessionNetworkHandler"/>); when that
        /// fails, or returns without NGO connected (see the 2.7 design, R1), the session is joined again by
        /// <paramref name="code"/>, with yet another handler (handlers are single use). An NGO rejection of the
        /// reconnection (seat refused, room full, already connected) is returned as is. Never throws; cancellation
        /// returns a failure without error key.
        /// </summary>
        internal async Task<RelaySessionStart> RejoinAsync(NetworkManager manager, string sessionId, string code, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return await JoinAsync(manager, code, cancellationToken);
            }

            string invalidKey = Validate(manager);
            if (invalidKey != null)
            {
                return RelaySessionStart.Failed(invalidKey);
            }

            s_startsInFlight++;
            _startsInFlight++;
            try
            {
                // The dropped session is usually still registered locally (its end after the drop is in flight, or the
                // SDK kept it): reconnecting over it fails on the lobby events (SDK error 23000, join_failed). Free the
                // type first (awaits that end, releases an orphan) and let NGO finish stopping.
                if (SessionSdkWorkarounds.GetRegistered(SessionType) != null)
                {
                    Debug.Log($"[{nameof(RelaySessionConnection)}] A previous session is still registered; freeing it before rejoining.");
                    if (!await FreeSessionTypeAsync(cancellationToken))
                    {
                        Debug.LogWarning($"[{nameof(RelaySessionConnection)}] The previous session could not be freed yet; rejoining anyway.");
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return RelaySessionStart.Failed(null);
                }

                if (!await WaitForManagerStoppedAsync(manager, cancellationToken))
                {
                    return RelaySessionStart.Failed(cancellationToken.IsCancellationRequested ? null : OnlineSessionErrorKeys.Busy);
                }

                RelaySessionStart reconnected = await ReconnectCoreAsync(manager, sessionId, cancellationToken);

                // The dropped session may still be registered (its end is in flight): free the type and retry once.
                if (reconnected.ErrorKey == OnlineSessionErrorKeys.AlreadyInSession && _alreadyInSessionRetrySeconds > 0f
                    && await FreeSessionTypeAsync(cancellationToken) && await WaitForManagerStoppedAsync(manager, cancellationToken))
                {
                    reconnected = await ReconnectCoreAsync(manager, sessionId, cancellationToken);
                }

                if (reconnected.IsSuccess || cancellationToken.IsCancellationRequested || IsNgoRejection(reconnected.ErrorKey))
                {
                    return reconnected;
                }

                if (string.IsNullOrEmpty(code))
                {
                    return reconnected;
                }

                // No network / service right now: the join by code would fail too, with a key that drops the offer
                // (join_failed / session_not_found). Return a key that keeps it, so the next (auto) attempt can retry.
                if (!RejoinOfferRules.FallsBackToCodeJoin(reconnected.ErrorKey))
                {
                    return reconnected;
                }

                // Only a hint on Windows: virtual adapters (VPN, Hyper-V, VirtualBox) can still report LAN with the wifi
                // down, so a reachable answer does not prove there is internet; NotReachable is trusted.
                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    Debug.Log($"[{nameof(RelaySessionConnection)}] Reconnection to session {sessionId} failed ({reconnected.ErrorKey ?? "no network"}) with no network reachable; not joining by code yet.");
                    return RelaySessionStart.Failed(OnlineSessionErrorKeys.NoConnection);
                }

                // NGO may still be shutting down after the failed attempt; the join needs it stopped.
                if (!await WaitForManagerStoppedAsync(manager, cancellationToken))
                {
                    return RelaySessionStart.Failed(cancellationToken.IsCancellationRequested ? null : OnlineSessionErrorKeys.Busy);
                }

                Debug.Log($"[{nameof(RelaySessionConnection)}] Reconnection to session {sessionId} failed ({reconnected.ErrorKey ?? "no network"}); joining again by code.");

                // The lobby may still list this player: the reclaim path reconnects to that membership instead of failing.
                return await JoinWithReclaimCoreAsync(manager, code, cancellationToken);
            }
            finally
            {
                if (s_startsInFlight > 0)
                {
                    s_startsInFlight--;
                }

                if (_startsInFlight > 0)
                {
                    _startsInFlight--;
                }
            }
        }

        /// <summary>
        /// Host, best-effort: removes member <paramref name="ugsPlayerId"/> from the current session (e.g. its reserved
        /// seat timed out), so it no longer counts against the session size. Never throws; does nothing on a client,
        /// without a session or when the id is not listed.
        /// </summary>
        public async Task TryRemovePlayerAsync(string ugsPlayerId)
        {
            ISession session = Session;
            if (session == null || !session.IsHost || !IsSessionMember(ugsPlayerId))
            {
                return;
            }

            try
            {
                await session.AsHost().RemovePlayerAsync(ugsPlayerId);
                Debug.Log($"[{nameof(RelaySessionConnection)}] Removed member {ugsPlayerId} from the session.");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[{nameof(RelaySessionConnection)}] Removing member {ugsPlayerId} failed: {exception.Message}");
            }
        }

        private async Task<RelaySessionStart> ReconnectCoreAsync(NetworkManager manager, string sessionId, CancellationToken cancellationToken)
        {
            var handler = new NgoSessionNetworkHandler(manager, _transport, _connectTimeoutSeconds);
            ISession session;
            try
            {
                var options = new ReconnectSessionOptions { Type = SessionType }.WithNetworkHandler(handler);
                session = await MultiplayerService.Instance.ReconnectToSessionAsync(sessionId, options, cancellationToken);
            }
            catch (Exception exception)
            {
                return RelaySessionStart.Failed(Fail(exception, handler, true, cancellationToken));
            }

            var start = RelaySessionStart.Succeeded(session, handler);

            // R1: the SDK may hand the session back without (re)starting the network; give a late start a moment. Once
            // started, the approval may take up to NGO's pending-client timeout (the host waits for lobby membership).
            float now = Time.realtimeSinceStartup;
            float startDeadline = now + ReconnectStartWaitSeconds;
            float connectDeadline = now + Mathf.Max(_reconnectConnectWaitSeconds,
                manager.NetworkConfig.ClientConnectionBufferTimeout + ReconnectBufferMarginSeconds);
            while (!(handler.Started && manager.IsConnectedClient))
            {
                now = Time.realtimeSinceStartup;
                bool timedOut = handler.Started ? now >= connectDeadline : now >= startDeadline;
                if (cancellationToken.IsCancellationRequested || !Application.isPlaying || handler.Retired || timedOut)
                {
                    string key = OnlineSessionErrorMapper.FromDisconnectReason(handler.DisconnectReason);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        // The player gave up the rejoin: leaving the lobby releases the reserved seat on the host.
                        await LeaveAsync(start);
                        return RelaySessionStart.Failed(null);
                    }

                    Debug.LogWarning($"[{nameof(RelaySessionConnection)}] Reconnected to session {sessionId} but NGO did not connect (started: {handler.Started}); detaching it locally to join by code.");
                    await DetachAsync(start);
                    return RelaySessionStart.Failed(key);
                }

                await Task.Yield();
            }

            Debug.Log($"[{nameof(RelaySessionConnection)}] Reconnected to session {sessionId} as client {manager.LocalClientId}.");
            return start;
        }

        private static async Task<bool> WaitForManagerStoppedAsync(NetworkManager manager, CancellationToken cancellationToken)
        {
            float deadline = Time.realtimeSinceStartup + ManagerStopWaitSeconds;
            while (manager != null && (manager.IsListening || manager.ShutdownInProgress))
            {
                if (cancellationToken.IsCancellationRequested || !Application.isPlaying || Time.realtimeSinceStartup >= deadline)
                {
                    return false;
                }

                await Task.Yield();
            }

            return manager != null && !cancellationToken.IsCancellationRequested;
        }

        // The host answered the reconnection itself: joining again by code would get the same answer.
        private static bool IsNgoRejection(string errorKey)
        {
            return errorKey == OnlineSessionErrorKeys.ReconnectDenied
                || errorKey == OnlineSessionErrorKeys.AlreadyConnected
                || errorKey == OnlineSessionErrorKeys.SessionFull
                || errorKey == OnlineSessionErrorKeys.ProtocolMismatch
                || errorKey == OnlineSessionErrorKeys.NotSessionMember;
        }

        /// <summary>Makes a successful start the current session (<see cref="Session"/>, <see cref="LeaveAsync()"/>).</summary>
        internal void Adopt(RelaySessionStart start)
        {
            if (start != null && start.IsSuccess)
            {
                _current = start;
            }
        }

        /// <summary>Stops tracking <paramref name="start"/> as the current session, if it is (no service call).</summary>
        internal void Release(RelaySessionStart start)
        {
            if (_current == start)
            {
                _current = null;
            }
        }

        public async Task<bool> StartHostAsync(NetworkManager manager)
        {
            return Complete(await CreateAsync(manager, CancellationToken.None));
        }

        public async Task<bool> StartClientAsync(NetworkManager manager)
        {
            return Complete(await JoinAsync(manager, _joinCode, CancellationToken.None));
        }

        /// <summary>Leaves the current session (see <see cref="LeaveAsync(RelaySessionStart)"/>). Never throws.</summary>
        public Task LeaveAsync()
        {
            RelaySessionStart current = _current;
            _current = null;
            return LeaveAsync(current);
        }

        /// <summary>Forgets the current session without contacting the service (it was deleted or this player removed).</summary>
        public void ForgetSession()
        {
            _current = null;
        }

        /// <summary>
        /// Ends the current session of a client dropped from it: its host left or its connection was lost (see
        /// <see cref="EndAfterDropAsync(RelaySessionStart, float)"/>). The lobby membership is kept (no LeaveAsync).
        /// Never throws.
        /// </summary>
        public Task EndAfterDropAsync()
        {
            RelaySessionStart current = _current;
            _current = null;
            return EndAfterDropAsync(current, _hostLeftWaitSeconds);
        }

        /// <summary>
        /// Client side of a session it was dropped from. Host left: the lobby is deleted (or about to be), so
        /// <c>ISession.LeaveAsync</c> would hit a 404 and leave the session registered in the SDK (see
        /// <see cref="SessionSdkWorkarounds"/>). Connection lost: leaving would give up the reserved seat it may rejoin.
        /// Waits up to <paramref name="waitSeconds"/> for the SDK's own Deleted / RemovedFromSession to unregister it,
        /// otherwise releases it locally; then stops that start's NGO run if still running. Touches no component
        /// state, so it also works after the root was destroyed. Never throws.
        /// </summary>
        internal static Task EndAfterDropAsync(RelaySessionStart start, float waitSeconds)
        {
            if (start == null || !start.IsSuccess)
            {
                return Task.CompletedTask;
            }

            return TrackEndAsync(start.Session, EndAfterDropCoreAsync(start, waitSeconds));
        }

        /// <summary>
        /// Leaves the session of <paramref name="start"/> only: the host deletes it (no host migration, members are
        /// removed), a client leaves; then that start's handler stops NGO if it is still running its own run. Touches no
        /// component state, so it also works after the root was destroyed. Never throws.
        /// </summary>
        internal static Task LeaveAsync(RelaySessionStart start)
        {
            if (start == null || !start.IsSuccess)
            {
                return Task.CompletedTask;
            }

            return TrackEndAsync(start.Session, LeaveCoreAsync(start));
        }

        private static async Task LeaveCoreAsync(RelaySessionStart start)
        {
            ISession session = start.Session;
            try
            {
                if (session.IsHost)
                {
                    await session.AsHost().DeleteAsync();
                }
                else
                {
                    await session.LeaveAsync();
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[{nameof(RelaySessionConnection)}] Leaving the session failed: {exception.Message}");

                // SDK 2.4.1: a failed leave / delete (e.g. lobby already gone) keeps the session type registered.
                if (SessionSdkWorkarounds.IsRegistered(session))
                {
                    await SessionSdkWorkarounds.ReleaseOrphanAsync(session, OrphanReleaseTimeoutSeconds);
                }
            }

            await StopHandlerAsync(start);
        }

        /// <summary>
        /// Drops <paramref name="start"/> on this machine only, keeping the lobby membership: its NGO run is stopped and
        /// the session, if still registered in the SDK, released locally (see <see cref="SessionSdkWorkarounds"/>). The
        /// host keeps the reserved seat (no PlayerHasLeft), so a later join by code reclaims it as a reconnection.
        /// Never throws.
        /// </summary>
        private static Task DetachAsync(RelaySessionStart start)
        {
            if (start == null || !start.IsSuccess)
            {
                return Task.CompletedTask;
            }

            return TrackEndAsync(start.Session, DetachCoreAsync(start));
        }

        private static async Task DetachCoreAsync(RelaySessionStart start)
        {
            await StopHandlerAsync(start);

            ISession session = start.Session;
            try
            {
                if (SessionSdkWorkarounds.IsRegistered(session))
                {
                    await SessionSdkWorkarounds.ReleaseOrphanAsync(session, OrphanReleaseTimeoutSeconds);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[{nameof(RelaySessionConnection)}] Detaching the session failed: {exception.Message}");
            }
        }

        private static async Task EndAfterDropCoreAsync(RelaySessionStart start, float waitSeconds)
        {
            ISession session = start.Session;
            try
            {
                if (!await SessionSdkWorkarounds.WaitUntilUnregisteredAsync(session, waitSeconds) && Application.isPlaying)
                {
                    // No Deleted / RemovedFromSession in time (event missed, or the host's delete failed).
                    await SessionSdkWorkarounds.ReleaseOrphanAsync(session, OrphanReleaseTimeoutSeconds);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[{nameof(RelaySessionConnection)}] Ending the session after a drop (host left / connection lost) failed: {exception.Message}");
            }

            await StopHandlerAsync(start);
        }

        private static async Task StopHandlerAsync(RelaySessionStart start)
        {
            if (start.Handler == null)
            {
                return;
            }

            try
            {
                await start.Handler.StopAsync();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[{nameof(RelaySessionConnection)}] Stopping the session network failed: {exception.Message}");
            }
        }

        // Registers a running end of the session until it completes (main thread only).
        private static async Task TrackEndAsync(ISession session, Task end)
        {
            s_pendingEnds[session] = end;
            try
            {
                await end;
            }
            finally
            {
                if (s_pendingEnds.TryGetValue(session, out Task tracked) && tracked == end)
                {
                    s_pendingEnds.Remove(session);
                }
            }
        }

        private bool Complete(RelaySessionStart start)
        {
            LastErrorKey = start.ErrorKey;
            Adopt(start);
            return start.IsSuccess;
        }

        private async Task<RelaySessionStart> StartAsync(NetworkManager manager, string joinCode, CancellationToken cancellationToken)
        {
            string invalidKey = Validate(manager);
            if (invalidKey != null)
            {
                return RelaySessionStart.Failed(invalidKey);
            }

            s_startsInFlight++;
            _startsInFlight++;
            try
            {
                return joinCode != null
                    ? await JoinWithReclaimCoreAsync(manager, joinCode, cancellationToken)
                    : await StartCoreAsync(manager, null, cancellationToken);
            }
            finally
            {
                if (s_startsInFlight > 0)
                {
                    s_startsInFlight--;
                }

                if (_startsInFlight > 0)
                {
                    _startsInFlight--;
                }
            }
        }

        // Join by code; refused as 'already in a session' (the lobby still lists this player), the held membership is
        // reclaimed (see ReclaimMembershipAsync). Callers count the start in flight.
        private async Task<RelaySessionStart> JoinWithReclaimCoreAsync(NetworkManager manager, string joinCode, CancellationToken cancellationToken)
        {
            RelaySessionStart start = await StartCoreAsync(manager, joinCode, cancellationToken);
            if (start.ErrorKey != OnlineSessionErrorKeys.AlreadyInSession || cancellationToken.IsCancellationRequested)
            {
                return start;
            }

            RelaySessionStart reclaimed = await ReclaimMembershipAsync(manager, joinCode, cancellationToken);
            return reclaimed ?? start;
        }

        private async Task<RelaySessionStart> StartCoreAsync(NetworkManager manager, string joinCode, CancellationToken cancellationToken)
        {
            bool isJoin = joinCode != null;
            bool retried = false;
            while (true)
            {
                var handler = new NgoSessionNetworkHandler(manager, _transport, _connectTimeoutSeconds);
                try
                {
                    ISession session;
                    if (isJoin)
                    {
                        session = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode, BuildJoinOptions(handler), cancellationToken);
                    }
                    else
                    {
                        session = await MultiplayerService.Instance.CreateSessionAsync(BuildCreateOptions(handler), cancellationToken);
                    }

                    return RelaySessionStart.Succeeded(session, handler);
                }
                catch (Exception exception)
                {
                    string key = Fail(exception, handler, isJoin, cancellationToken);

                    // A previous root's leave may still hold the session type, or the SDK kept an orphan: free it, retry once.
                    if (key == OnlineSessionErrorKeys.AlreadyInSession && !retried && _alreadyInSessionRetrySeconds > 0f)
                    {
                        retried = true;
                        if (await FreeSessionTypeAsync(cancellationToken) && manager != null && !manager.IsListening && !manager.ShutdownInProgress)
                        {
                            Debug.Log($"[{nameof(RelaySessionConnection)}] Retrying the session start once after 'already in a session'.");
                            continue;
                        }

                        key = cancellationToken.IsCancellationRequested ? null : key;
                    }

                    return RelaySessionStart.Failed(key);
                }
            }
        }

        // A join by code refused as 'already in a session' although nothing is registered locally: the lobby still lists
        // this player (dropped without leaving, e.g. a spectator whose membership the host has not removed yet). With
        // exactly one joined session, that session is identified first WITHOUT the network (a probe handler that never
        // starts NGO), so another game's host never sees this client: the session of joinCode is then reconnected for
        // real (ReconnectToSessionAsync starts the network); any other one is a stale membership, left before joining by
        // code once more. Null keeps the original failure.
        private async Task<RelaySessionStart> ReclaimMembershipAsync(NetworkManager manager, string joinCode, CancellationToken cancellationToken)
        {
            List<string> joined;
            try
            {
                joined = await MultiplayerService.Instance.GetJoinedSessionIdsAsync();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[{nameof(RelaySessionConnection)}] Could not list the joined sessions: {exception.Message}");
                return null;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return RelaySessionStart.Failed(null);
            }

            if (joined == null || joined.Count != 1 || string.IsNullOrEmpty(joined[0]) || !Application.isPlaying)
            {
                return null;
            }

            string sessionId = joined[0];
            RelaySessionStart probe = await ProbeSessionAsync(manager, sessionId, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                // Cancelling once the probe is in gives that membership up, as cancelling a rejoin does: the player chose
                // not to come back, and leaving the lobby releases a seat still reserved for it.
                await LeaveAsync(probe);
                RelaySessionStart cancelled = RelaySessionStart.Failed(null);
                return probe.IsSuccess ? cancelled.WithLeftSession(sessionId) : cancelled;
            }

            if (!probe.IsSuccess)
            {
                return null;
            }

            MembershipMatch match = MembershipRules.Classify(probe.Session.Code, joinCode);
            if (match == MembershipMatch.Unknown)
            {
                // Not identified: it may be this very session, so it is neither reconnected to nor left (leaving would
                // release its seat as Left). Only the probe's local registration goes; the original failure stands.
                Debug.LogWarning($"[{nameof(RelaySessionConnection)}] The joined session {sessionId} could not be identified (no code); keeping its membership.");
                await DetachAsync(probe);
                return null;
            }

            if (match == MembershipMatch.Same)
            {
                Debug.Log($"[{nameof(RelaySessionConnection)}] Join by code refused as 'already in a session': reconnecting to its session {sessionId}.");

                // Keeps the lobby membership (no PlayerHasLeft on the host); only the probe's local registration goes.
                await DetachAsync(probe);
                if (cancellationToken.IsCancellationRequested)
                {
                    return RelaySessionStart.Failed(null);
                }

                RelaySessionStart reconnected = await ReconnectCoreAsync(manager, sessionId, cancellationToken);
                if (reconnected.IsSuccess)
                {
                    return reconnected;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return RelaySessionStart.Failed(null);
                }

                // The host's own answer (room full, seat refused...) explains more than 'already in a session'.
                return IsNgoRejection(reconnected.ErrorKey) ? reconnected : null;
            }

            // Other: whatever happens next, that membership is gone (the caller drops a rejoin offer for it).
            Debug.LogWarning($"[{nameof(RelaySessionConnection)}] The joined session {sessionId} is not the one of code {joinCode}; leaving that stale membership and joining again.");
            await LeaveAsync(probe);
            if (cancellationToken.IsCancellationRequested)
            {
                return RelaySessionStart.Failed(null).WithLeftSession(sessionId);
            }

            if (!await WaitForManagerStoppedAsync(manager, cancellationToken))
            {
                string key = cancellationToken.IsCancellationRequested ? null : OnlineSessionErrorKeys.Busy;
                return RelaySessionStart.Failed(key).WithLeftSession(sessionId);
            }

            // Once: StartCoreAsync's own retry is the last one (no further reclaim).
            RelaySessionStart joinedAgain = await StartCoreAsync(manager, joinCode, cancellationToken);
            return joinedAgain.WithLeftSession(sessionId);
        }

        // Reconnects to sessionId with a handler retired beforehand: its StartAsync returns without starting NGO (see
        // NgoSessionNetworkHandler), so the session (its code) can be read without this client reaching that host.
        private async Task<RelaySessionStart> ProbeSessionAsync(NetworkManager manager, string sessionId, CancellationToken cancellationToken)
        {
            var probe = new NgoSessionNetworkHandler(manager, _transport, _connectTimeoutSeconds);
            probe.Retire();
            try
            {
                var options = new ReconnectSessionOptions { Type = SessionType }.WithNetworkHandler(probe);
                ISession session = await MultiplayerService.Instance.ReconnectToSessionAsync(sessionId, options, cancellationToken);
                return RelaySessionStart.Succeeded(session, probe);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[{nameof(RelaySessionConnection)}] Could not identify the joined session {sessionId}: {exception.Message}");
                return RelaySessionStart.Failed(Fail(exception, probe, true, cancellationToken));
            }
        }

        // True when the session type is free again: an end still in flight is awaited (bounded; if it does not finish in
        // time the start fails instead of releasing it, since the SDK unregisters by type and a late second release could
        // drop a newer session). A registered session nobody is ending, starting or using is an SDK orphan and is released.
        private async Task<bool> FreeSessionTypeAsync(CancellationToken cancellationToken)
        {
            ISession registered = SessionSdkWorkarounds.GetRegistered(SessionType);
            if (registered == null)
            {
                // Nothing registered (anymore): a transient refusal; keep the plain delay before retrying.
                return await DelayRetryAsync(cancellationToken);
            }

            if (_current != null && ReferenceEquals(registered, _current.Session))
            {
                return false;
            }

            // The caller's own start (create / join / rejoin) counts as one; another start in flight may own the registered session.
            if (s_startsInFlight > 1)
            {
                Debug.Log($"[{nameof(RelaySessionConnection)}] Another session start is in flight; not releasing the registered session.");
                return false;
            }

            if (s_pendingEnds.TryGetValue(registered, out Task pending))
            {
                Debug.Log($"[{nameof(RelaySessionConnection)}] Waiting for the previous session to finish leaving before retrying.");
                await Task.WhenAny(pending, Task.Delay(TimeSpan.FromSeconds(PendingEndWaitSeconds), cancellationToken));
                if (!pending.IsCompleted)
                {
                    // Its own cleanup is still running: never race it with a second release.
                    return false;
                }
            }

            if (cancellationToken.IsCancellationRequested || !Application.isPlaying)
            {
                return false;
            }

            if (!SessionSdkWorkarounds.IsRegistered(registered))
            {
                return true;
            }

            bool released = await SessionSdkWorkarounds.ReleaseOrphanAsync(registered, OrphanReleaseTimeoutSeconds);
            return released && !cancellationToken.IsCancellationRequested;
        }

        private async Task<bool> DelayRetryAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_alreadyInSessionRetrySeconds), cancellationToken);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        private string Validate(NetworkManager manager)
        {
            if (manager == null || _transport == null || _settings == null)
            {
                Debug.LogError($"[{nameof(RelaySessionConnection)}] Missing NetworkManager, UnityTransport or NetworkSettings on '{name}'.", this);
                return OnlineSessionErrorKeys.NotConfigured;
            }

            if (_current != null)
            {
                return OnlineSessionErrorKeys.AlreadyInSession;
            }

            if (manager.IsListening || manager.ShutdownInProgress)
            {
                Debug.LogWarning($"[{nameof(RelaySessionConnection)}] NetworkManager is already running or shutting down; start ignored.", this);
                return OnlineSessionErrorKeys.Busy;
            }

            if (MultiplayerService.Instance == null)
            {
                return OnlineSessionErrorKeys.ServicesUnavailable;
            }

            return null;
        }

        // Error key of a failed start (null when cancelled). The SDK cleans its own session up on a failed or cancelled
        // create/join (it leaves / releases it before rethrowing and never registers it, so no orphan is left for
        // SessionSdkWorkarounds); this handler only stops NGO if it started it. Its NetworkModule stays subscribed to
        // that dead session's Changed event (no public way to detach it) and may start the network again with this
        // handler: Shutdown retires it, so such a late start is ignored (see NgoSessionNetworkHandler.Retired).
        private static string Fail(Exception exception, NgoSessionNetworkHandler handler, bool isJoin, CancellationToken cancellationToken)
        {
            handler.Shutdown();
            if (cancellationToken.IsCancellationRequested || exception is OperationCanceledException)
            {
                Debug.Log($"[{nameof(RelaySessionConnection)}] Session start cancelled.");
                return null;
            }

            // An NGO rejection (room full, protocol) explains the failure better than the SDK's generic start error.
            string key = OnlineSessionErrorMapper.FromDisconnectReason(handler.DisconnectReason)
                ?? (isJoin ? OnlineSessionErrorMapper.FromJoinException(exception) : OnlineSessionErrorMapper.FromException(exception))
                ?? OnlineSessionErrorKeys.ConnectionFailed;
            Debug.LogWarning($"[{nameof(RelaySessionConnection)}] Session start failed ({key}): {exception.Message}");
            return key;
        }

        private SessionOptions BuildCreateOptions(NgoSessionNetworkHandler handler)
        {
            var options = new SessionOptions
            {
                Type = SessionType,
                MaxPlayers = _settings.MaxPlayers,
                IsPrivate = true,
                PlayerProperties = BuildPlayerProperties()
            };

            return options
                .WithRelayNetwork(string.IsNullOrWhiteSpace(_region) ? null : _region.Trim())
                .WithNetworkHandler(handler);
        }

        private JoinSessionOptions BuildJoinOptions(NgoSessionNetworkHandler handler)
        {
            var options = new JoinSessionOptions
            {
                Type = SessionType,
                PlayerProperties = BuildPlayerProperties()
            };

            return options.WithNetworkHandler(handler);
        }

        private Dictionary<string, PlayerProperty> BuildPlayerProperties()
        {
            return new Dictionary<string, PlayerProperty>
            {
                { DisplayNameProperty, new PlayerProperty(_displayName, VisibilityPropertyOptions.Member) }
            };
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_pendingEnds.Clear();
            s_startsInFlight = 0;
        }

        private void OnValidate()
        {
            if (_transport == null)
            {
                TryGetComponent(out _transport);
            }

            if (_settings == null)
            {
                Debug.LogWarning($"[{nameof(RelaySessionConnection)}] Missing NetworkSettings on '{name}'.", this);
            }
        }
    }
}
