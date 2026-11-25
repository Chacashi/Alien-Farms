using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Menu;
using AlienFarms.Networking.Session;
using Unity.Services.Friends;
using Unity.Services.Friends.Exceptions;
using Unity.Services.Friends.Models;
using Unity.Services.Friends.Notifications;
using Unity.Services.Friends.Options;
using UnityEngine;

namespace AlienFarms.Networking.Social
{
    [DisallowMultipleComponent]
    public sealed class FriendsSocialService : MonoBehaviour
    {
        [SerializeField] private FriendsSettings _settings;
        [SerializeField] private FriendsChannel _friends;
        [SerializeField] private OnlineSessionChannel _onlineSession;
        [SerializeField] private OnlineSessionService _sessionService;

        private enum Request { Add, Accept, Decline, CancelOutgoing, Invite, Refresh }

        private const double InitialRetrySeconds = 2d;
        private const double MaxRetrySeconds = 30d;

        // Two Play sessions without domain reload can overlap if the SDK is still initializing.
        private static SemaphoreSlim s_initializationGate = new SemaphoreSlim(1);
        // The SDK can survive SubsystemRegistration and may not replay its connectivity event.
        private static bool s_lastConnectivity;

        private readonly Queue<(Request Kind, string Id, string SessionId)> _requests = new();
        private readonly List<FriendEntry> _friendEntries = new();
        private readonly List<FriendRequestEntry> _incomingEntries = new();
        private readonly List<FriendRequestEntry> _outgoingEntries = new();
        private readonly Dictionary<string, double> _inviteSentAt = new();
        private string _inviteInFlight;
        private readonly PresenceDebounce _presence = new();
        private FriendEntry[] _friendBuffer;
        private FriendRequestEntry[] _requestBuffer;
        private IFriendsService _sdk;
        private MatchPhase _matchPhase;
        private bool _active;
        private bool _quitting;
        private bool _initializing;
        private (Request Kind, string Id)? _requestInFlight;
        private bool _presenceInFlight;
        private bool _connected;
        private int _lifetime;
        private double _nextRefreshAt;
        private double _nextInitRetryAt;
        private double _initRetrySeconds = InitialRetrySeconds;

        private bool HasReferences => _settings != null && _friends != null &&
            _onlineSession != null && _sessionService != null;
        private bool CanInitialize => _active && !_quitting && _onlineSession.Status >= OnlineSessionStatus.Ready &&
            _sessionService.Bootstrapper?.IsSignedIn == true;
        private bool IsReady => CanInitialize && _friends.Status == FriendsServiceStatus.Ready;
        private bool InMatch => _matchPhase is MatchPhase.Countdown or MatchPhase.Playing or MatchPhase.Results;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_initializationGate = new SemaphoreSlim(1);

        private void OnValidate()
        {
            if (!HasReferences)
            {
                Debug.LogWarning($"[{nameof(FriendsSocialService)}] One or more references are not assigned on '{name}'.", this);
            }
        }

        private void OnEnable()
        {
            if (!HasReferences)
            {
                return;
            }

            _active = true;
            _quitting = false;
            int capacity = _settings.MaxEntries;
            _friendBuffer = new FriendEntry[capacity];
            _requestBuffer = new FriendRequestEntry[capacity];
            _friends.ConfigureCapacity(capacity);

            _onlineSession.Changed += HandleSessionChanged;
            GameEvents.OnMatchPhaseChanged += HandleMatchPhaseChanged;
            _friends.AddFriendRequested += HandleAddFriend;
            _friends.AcceptRequestRequested += HandleAccept;
            _friends.DeclineRequestRequested += HandleDecline;
            _friends.CancelOutgoingRequested += HandleCancelOutgoing;
            _friends.RefreshRequested += HandleRefresh;
            _friends.InviteRequested += HandleInvite;
            _friends.SetInviteEligibility(CanInvite);

            HandleSessionChanged();
        }

        private void OnDisable()
        {
            if (!_active)
            {
                return;
            }

            _active = false;
            _onlineSession.Changed -= HandleSessionChanged;
            GameEvents.OnMatchPhaseChanged -= HandleMatchPhaseChanged;
            _friends.AddFriendRequested -= HandleAddFriend;
            _friends.AcceptRequestRequested -= HandleAccept;
            _friends.DeclineRequestRequested -= HandleDecline;
            _friends.CancelOutgoingRequested -= HandleCancelOutgoing;
            _friends.RefreshRequested -= HandleRefresh;
            _friends.InviteRequested -= HandleInvite;
            _friends.SetInviteEligibility(null);
            ResetSession();
        }

        private void ResetSession()
        {
            _lifetime++;
            UnsubscribeSdk();
            _initRetrySeconds = InitialRetrySeconds;
            _nextInitRetryAt = 0d;

            _requests.Clear();
            _inviteSentAt.Clear();
            _inviteInFlight = null;
            _friends.DeclineInvite();
            _presence.Reset();
            _connected = false;
            _matchPhase = MatchPhase.None;
            _friends.SetStatus(FriendsServiceStatus.Off);
            _friends.SetLocalPlayerId(null);
            _friends.SetNotificationsConnected(false);
            _friends.SetLastErrorKey(null);
            _friends.SetLastInfoKey(null);
            _friends.SetFriends(null, 0);
            _friends.SetRequests(null, 0);
        }

        private void UnsubscribeSdk()
        {
            if (_sdk != null)
            {
                _sdk.RelationshipAdded -= HandleRelationshipAdded;
                _sdk.RelationshipDeleted -= HandleRelationshipDeleted;
                _sdk.PresenceUpdated -= HandlePresenceUpdated;
                _sdk.MessageReceived -= HandleMessageReceived;
                _sdk.NotificationsConnectivityChanged -= HandleConnectivityChanged;
                _sdk = null;
            }
        }

        private void HandleSessionChanged()
        {
            if (_onlineSession.Status != OnlineSessionStatus.InSession)
            {
                _matchPhase = MatchPhase.None;
            }

            if (!CanInitialize)
            {
                if (_friends.Status != FriendsServiceStatus.Off)
                {
                    ResetSession();
                }

                return;
            }

            if ((_friends.Status is FriendsServiceStatus.Off or FriendsServiceStatus.Error) && !_initializing)
            {
                _ = InitializeAsync();
            }
            else if (IsReady)
            {
                SchedulePresence();
            }
        }

        private async Task InitializeAsync()
        {
            _initializing = true;
            int lifetime = _lifetime;
            SemaphoreSlim gate = s_initializationGate;
            _friends.SetStatus(FriendsServiceStatus.Initializing);

            await gate.WaitAsync();
            try
            {
                if (!IsCurrent(lifetime))
                {
                    return;
                }

                _sdk = FriendsService.Instance;
                bool sdkWasInitialized = IsSdkInitialized(_sdk);
                if (!sdkWasInitialized)
                {
                    s_lastConnectivity = false;
                }

                _connected = s_lastConnectivity;
                _sdk.RelationshipAdded += HandleRelationshipAdded;
                _sdk.RelationshipDeleted += HandleRelationshipDeleted;
                _sdk.PresenceUpdated += HandlePresenceUpdated;
                _sdk.MessageReceived += HandleMessageReceived;
                _sdk.NotificationsConnectivityChanged += HandleConnectivityChanged;

                // InitializeAsync is idempotent when the SDK instance survives a NetworkRoot replacement.
                await _sdk.InitializeAsync(new InitializeOptions().WithEvents(true)
                    .WithMemberPresence(true).WithMemberProfile(true));
                if (!IsCurrent(lifetime))
                {
                    return;
                }

                // A surviving SDK keeps cached lists; presence events may have been missed while it was idle.
                if (sdkWasInitialized)
                {
                    await _sdk.ForceRelationshipsRefreshAsync();
                    if (!IsCurrent(lifetime))
                    {
                        return;
                    }
                }

                _friends.SetLocalPlayerId(_onlineSession.LocalPlayerId);
                _friends.SetNotificationsConnected(_connected);
                _nextRefreshAt = Time.unscaledTimeAsDouble + _settings.FallbackRefreshSeconds;
                _friends.SetLastErrorKey(null);
                PublishLists();
                _initRetrySeconds = InitialRetrySeconds;
                _friends.SetStatus(FriendsServiceStatus.Ready);

                var online = FriendPresenceRules.Resolve(_onlineSession.Status, false);
                _presence.Schedule(online, Time.unscaledTimeAsDouble, 0d);
                TrySendPresence(Time.unscaledTimeAsDouble);
                SchedulePresence();
            }
            catch (Exception exception)
            {
                if (IsCurrent(lifetime))
                {
                    UnsubscribeSdk();
                    _nextInitRetryAt = Time.unscaledTimeAsDouble + _initRetrySeconds;
                    _initRetrySeconds = Math.Min(_initRetrySeconds * 2d, MaxRetrySeconds);
                    _friends.SetStatus(FriendsServiceStatus.Error);
                    ReportError(exception);
                }
            }
            finally
            {
                gate.Release();
                _initializing = false;
                if (lifetime != _lifetime && CanInitialize)
                {
                    HandleSessionChanged();
                }
            }
        }

        private static bool IsSdkInitialized(IFriendsService sdk)
        {
            try
            {
                // Friends 1.4.1 exposes no initialization flag. This getter documents the pre-init exception.
                _ = sdk.Relationships;
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private void Update()
        {
            if (CanInitialize && _friends.Status == FriendsServiceStatus.Error && !_initializing &&
                Time.unscaledTimeAsDouble >= _nextInitRetryAt)
            {
                _ = InitializeAsync();
                return;
            }

            if (!IsReady)
            {
                return;
            }

            double now = Time.unscaledTimeAsDouble;
            TrySendPresence(now);
            if (_requestInFlight.HasValue)
            {
                return;
            }

            if (_requests.Count > 0)
            {
                var request = _requests.Dequeue();
                _ = ExecuteRequestAsync(request.Kind, request.Id, true, request.SessionId);
            }
            else if (!_connected && _friends.IsPanelOpen && now >= _nextRefreshAt)
            {
                _ = ExecuteRequestAsync(Request.Refresh, null, false);
            }
        }

        private void HandleAddFriend(string id) => Enqueue(Request.Add, id);
        private void HandleAccept(string id) => Enqueue(Request.Accept, id);
        private void HandleDecline(string id) => Enqueue(Request.Decline, id);
        private void HandleCancelOutgoing(string id) => Enqueue(Request.CancelOutgoing, id);
        private void HandleInvite(string id) => Enqueue(Request.Invite, id);
        private void HandleRefresh()
        {
            if (CanInitialize && _friends.Status == FriendsServiceStatus.Error)
            {
                if (!_initializing)
                {
                    _ = InitializeAsync();
                }

                return;
            }

            Enqueue(Request.Refresh, null);
        }

        private void Enqueue(Request kind, string id)
        {
            if (!IsReady)
            {
                _friends.SetLastErrorKey(FriendsErrorKeys.NotEnabled);
                return;
            }

            if (kind != Request.Refresh && string.IsNullOrWhiteSpace(id))
            {
                _friends.SetLastErrorKey(FriendsErrorKeys.Payload);
                return;
            }

            id = id?.Trim();
            if (_requestInFlight == (kind, id))
            {
                return;
            }

            foreach (var queued in _requests)
            {
                if (queued.Kind == kind && queued.Id == id)
                {
                    return;
                }
            }

            string sessionId = kind == Request.Invite ? _sessionService.CurrentSession?.Id : null;
            _requests.Enqueue((kind, id, sessionId));
        }

        private async Task ExecuteRequestAsync(Request kind, string id, bool userRequested, string sessionId = null)
        {
            _requestInFlight = (kind, id);
            int lifetime = _lifetime;
            try
            {
                string info;
                switch (kind)
                {
                    case Request.Add:
                    case Request.Accept:
                        Relationship relationship = await _sdk.AddFriendAsync(id);
                        info = relationship.Type == RelationshipType.Friend
                            ? FriendsErrorKeys.FriendAdded : FriendsErrorKeys.RequestSent;
                        break;
                    case Request.Decline:
                        await _sdk.DeleteIncomingFriendRequestAsync(id);
                        info = FriendsErrorKeys.RequestDeclined;
                        break;
                    case Request.CancelOutgoing:
                        await _sdk.DeleteOutgoingFriendRequestAsync(id);
                        info = FriendsErrorKeys.RequestCanceled;
                        break;
                    case Request.Invite:
                        string error = await SendInviteAsync(id, sessionId, lifetime);
                        if (!IsCurrent(lifetime))
                        {
                            return;
                        }

                        if (error != null)
                        {
                            _friends.SetLastInfoKey(null);
                            _friends.SetLastErrorKey(error);
                            return;
                        }

                        info = FriendsErrorKeys.InviteSent;
                        break;
                    default:
                        await _sdk.ForceRelationshipsRefreshAsync();
                        info = FriendsErrorKeys.ListRefreshed;
                        break;
                }

                if (IsCurrent(lifetime))
                {
                    if (kind != Request.Invite)
                    {
                        PublishLists();
                    }
                    if (userRequested)
                    {
                        _friends.SetLastErrorKey(null);
                        _friends.SetLastInfoKey(info);
                    }
                }
            }
            catch (Exception exception)
            {
                if (IsCurrent(lifetime))
                {
                    if (userRequested)
                    {
                        ReportError(exception);
                    }
                    else
                    {
                        LogBackgroundError("refresh", exception);
                    }
                }
            }
            finally
            {
                if (kind == Request.Refresh && IsCurrent(lifetime))
                {
                    _nextRefreshAt = Time.unscaledTimeAsDouble + _settings.FallbackRefreshSeconds;
                }

                _requestInFlight = null;
            }
        }

        private bool CanInvite(FriendEntry entry) => IsReady && _inviteInFlight != entry.UgsPlayerId &&
            InviteSendRules.CanInvite(_onlineSession.IsHost, _onlineSession.Status, _onlineSession.ParticipantCount,
                entry.Availability, InMatch,
                _inviteSentAt.TryGetValue(entry.UgsPlayerId, out double sentAt) ? sentAt : (double?)null,
                Time.realtimeSinceStartupAsDouble, _settings.InviteCooldownSeconds);

        private int FindFriendIndex(string id)
        {
            for (int i = 0; i < _friendEntries.Count; i++)
            {
                if (_friendEntries[i].UgsPlayerId == id)
                {
                    return i;
                }
            }

            return -1;
        }

        private async Task<string> SendInviteAsync(string id, string sessionId, int lifetime)
        {
            if (id == _friends.LocalPlayerId)
            {
                return FriendsErrorKeys.Self;
            }

            int index = FindFriendIndex(id);
            if (index < 0)
            {
                return FriendsErrorKeys.NotFriend;
            }

            if (string.IsNullOrEmpty(sessionId) || sessionId != _sessionService.CurrentSession?.Id ||
                !CanInvite(_friendEntries[index]))
            {
                return OnlineSessionErrorKeys.Busy;
            }

            if (!SessionCodeFormat.TryNormalize(_onlineSession.Code, out string code))
            {
                return FriendsErrorKeys.Payload;
            }

            _inviteInFlight = id;
            try
            {
                await _sdk.MessageAsync(id, new SessionInviteMessage
                {
                    code = code,
                    sessionId = sessionId,
                    hostName = _onlineSession.LocalPlayerName,
                    sentAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });

                if (IsCurrent(lifetime))
                {
                    _inviteSentAt[id] = Time.realtimeSinceStartupAsDouble;
                }

                return null;
            }
            finally
            {
                _inviteInFlight = null;
            }
        }

        private void HandleMessageReceived(IMessageReceivedEvent notification)
        {
            if (!IsReady)
            {
                return;
            }

            string senderId = notification.UserId;
            int index = string.IsNullOrWhiteSpace(senderId) ? -1 : FindFriendIndex(senderId);
            if (index < 0)
            {
                Debug.Log($"[{nameof(FriendsSocialService)}] Ignored an invitation from a non-friend.", this);
                return;
            }

            SessionInviteMessage message;
            try
            {
                message = notification.GetAs<SessionInviteMessage>();
            }
            catch (Exception)
            {
                return;
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long ttlMs = _settings.InviteTtlSeconds * 1000L;
            var result = InviteRules.CreateOffer(message, _friends.LocalPlayerId, senderId,
                _friendEntries[index].DisplayName, now, ttlMs, Time.realtimeSinceStartupAsDouble,
                out SessionInviteOffer offer);
            if (result == InviteRules.Result.Expired)
            {
                Debug.Log($"[{nameof(FriendsSocialService)}] Ignored an expired invitation.", this);
            }
            else if (result == InviteRules.Result.Valid)
            {
                _friends.SetPendingInvite(offer);
            }
        }

        private bool IsCurrent(int lifetime) => lifetime == _lifetime && CanInitialize;

        private void ReportError(Exception exception)
        {
            if (exception is FriendsServiceException sdkException)
            {
                Debug.LogWarning($"[{nameof(FriendsSocialService)}] Friends error {(int)sdkException.ErrorCode}: {sdkException.Message}", this);
            }
            else if (exception is not RelationshipNotFoundException)
            {
                Debug.LogException(exception, this);
            }

            string key = exception switch
            {
                FriendsServiceException friendsException => FriendsErrorMapper.Map((int)friendsException.ErrorCode),
                RelationshipNotFoundException => FriendsErrorKeys.NotFriend,
                _ => FriendsErrorKeys.Unknown
            };
            _friends.SetLastInfoKey(null);
            _friends.SetLastErrorKey(key);
        }

        private void LogBackgroundError(string operation, Exception exception)
        {
            string code = exception is FriendsServiceException sdkException ? $" ({(int)sdkException.ErrorCode})" : string.Empty;
            Debug.LogWarning($"[{nameof(FriendsSocialService)}] Background {operation} failed{code}: {exception}", this);
        }

        private void HandleRelationshipAdded(IRelationshipAddedEvent change) => PublishIfReady();
        private void HandleRelationshipDeleted(IRelationshipDeletedEvent change) => PublishIfReady();
        private void HandlePresenceUpdated(IPresenceUpdatedEvent change) => PublishIfReady();

        private void HandleConnectivityChanged(INotificationsStateChangedEvent change)
        {
            bool connected = change.State == FriendsEventConnectionState.Subscribed;
            s_lastConnectivity = connected;
            if (!CanInitialize)
            {
                return;
            }
            if (_connected != connected)
            {
                _connected = connected;
                _nextRefreshAt = Time.unscaledTimeAsDouble + _settings.FallbackRefreshSeconds;
            }

            if (IsReady)
            {
                _friends.SetNotificationsConnected(_connected);
            }
        }

        private void PublishIfReady()
        {
            if (IsReady)
            {
                PublishLists();
            }
        }

        private void PublishLists()
        {
            _friendEntries.Clear();
            IReadOnlyList<Relationship> friends = _sdk.Friends;
            for (int i = 0; i < friends.Count; i++)
            {
                Member member = friends[i].Member;
                Presence presence = member?.Presence;
                DateTime lastSeen = presence?.LastSeen ?? default;
                long lastSeenUnixMs = lastSeen == default ? 0 :
                    new DateTimeOffset(DateTime.SpecifyKind(lastSeen, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
                _friendEntries.Add(new FriendEntry(member?.Id, member?.Profile?.Name,
                    (FriendAvailability)(presence?.Availability ?? Availability.Unknown),
                    ReadActivity(presence), lastSeenUnixMs));
            }

            AdaptRequests(_sdk.IncomingFriendRequests, _incomingEntries, true);
            AdaptRequests(_sdk.OutgoingFriendRequests, _outgoingEntries, false);
            int friendCount = FriendListBuilder.Build(_friendEntries, _friendBuffer);
            int requestCount = FriendListBuilder.BuildRequests(_incomingEntries, _outgoingEntries, _requestBuffer);
            _friends.SetFriends(_friendBuffer, friendCount);
            _friends.SetRequests(_requestBuffer, requestCount);
        }

        private static void AdaptRequests(IReadOnlyList<Relationship> source, List<FriendRequestEntry> entries,
            bool incoming)
        {
            entries.Clear();
            for (int i = 0; i < source.Count; i++)
            {
                Member member = source[i].Member;
                entries.Add(new FriendRequestEntry(member?.Id, member?.Profile?.Name, incoming));
            }
        }

        private static string ReadActivity(Presence presence)
        {
            try
            {
                return presence?.GetActivity<FriendPresenceActivity>()?.state switch
                {
                    FriendActivityKeys.Hub => FriendActivityKeys.Hub,
                    FriendActivityKeys.Room => FriendActivityKeys.Room,
                    FriendActivityKeys.Match => FriendActivityKeys.Match,
                    _ => null
                };
            }
            catch (Exception)
            {
                // Friends can publish unrelated activity data or leave it empty.
                return null;
            }
        }

        private void HandleMatchPhaseChanged(MatchPhase previous, MatchPhase current)
        {
            _matchPhase = current;
            if (IsReady)
            {
                SchedulePresence();
            }
        }

        private void SchedulePresence()
        {
            bool inMatch = _onlineSession.Status == OnlineSessionStatus.InSession && InMatch;
            _presence.Schedule(FriendPresenceRules.Resolve(_onlineSession.Status, inMatch),
                Time.unscaledTimeAsDouble, _settings.PresenceDebounceSeconds);
        }

        private void TrySendPresence(double now)
        {
            if (!_presenceInFlight && _presence.TryTake(now, out var desired))
            {
                _ = SendPresenceAsync(desired);
            }
        }

        private async Task SendPresenceAsync((FriendAvailability Availability, string ActivityState) desired)
        {
            _presenceInFlight = true;
            int lifetime = _lifetime;
            try
            {
                await _sdk.SetPresenceAsync((Availability)desired.Availability,
                    new FriendPresenceActivity { state = desired.ActivityState });
            }
            catch (Exception exception)
            {
                if (IsCurrent(lifetime))
                {
                    _presence.Retry(desired, Time.unscaledTimeAsDouble + _settings.FallbackRefreshSeconds);
                    LogBackgroundError("presence", exception);
                }
            }
            finally
            {
                _presenceInFlight = false;
            }
        }

        private void OnApplicationQuit()
        {
            bool wasReady = IsReady;
            _quitting = true;
            if (wasReady)
            {
                _ = SendOfflineAsync(_sdk);
            }
        }

        private static async Task SendOfflineAsync(IFriendsService sdk)
        {
            try
            {
                await sdk.SetPresenceAvailabilityAsync(Availability.Offline);
            }
            catch (Exception)
            {
                // Application shutdown can interrupt the best-effort request.
            }
        }
    }
}
