using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AlienFarms.Core;
using AlienFarms.Data;
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

        private enum Request { Add, Accept, Decline, CancelOutgoing, Refresh }

        private static SemaphoreSlim s_initializationGate = new SemaphoreSlim(1);
        private readonly Queue<(Request Kind, string Id)> _requests = new();
        private readonly List<FriendEntry> _friendEntries = new();
        private readonly List<FriendRequestEntry> _incomingEntries = new();
        private readonly List<FriendRequestEntry> _outgoingEntries = new();
        private readonly PresenceDebounce _presence = new();
        private FriendEntry[] _friendBuffer;
        private FriendRequestEntry[] _requestBuffer;
        private IFriendsService _sdk;
        private MatchPhase _matchPhase;
        private bool _active;
        private bool _quitting;
        private bool _initializing;
        private bool _requestInFlight;
        private bool _presenceInFlight;
        private bool _connected;
        private int _lifetime;
        private double _nextRefreshAt;

        private bool HasReferences => _settings != null && _friends != null &&
            _onlineSession != null && _sessionService != null;
        private bool CanInitialize => _active && !_quitting && _onlineSession.Status >= OnlineSessionStatus.Ready &&
            _sessionService.Bootstrapper?.IsSignedIn == true;
        private bool IsReady => CanInitialize && _friends.Status == FriendsServiceStatus.Ready;

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
            ResetSession();
        }

        private void ResetSession()
        {
            _lifetime++;
            if (_sdk != null)
            {
                _sdk.RelationshipAdded -= HandleRelationshipAdded;
                _sdk.RelationshipDeleted -= HandleRelationshipDeleted;
                _sdk.PresenceUpdated -= HandlePresenceUpdated;
                _sdk.NotificationsConnectivityChanged -= HandleConnectivityChanged;
                _sdk = null;
            }

            _requests.Clear();
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

            if (_friends.Status == FriendsServiceStatus.Off && !_initializing)
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
                _sdk.RelationshipAdded += HandleRelationshipAdded;
                _sdk.RelationshipDeleted += HandleRelationshipDeleted;
                _sdk.PresenceUpdated += HandlePresenceUpdated;
                _sdk.NotificationsConnectivityChanged += HandleConnectivityChanged;

                // InitializeAsync is idempotent when the SDK instance survives a NetworkRoot replacement.
                await _sdk.InitializeAsync(new InitializeOptions().WithEvents(true)
                    .WithMemberPresence(true).WithMemberProfile(true));
                if (!IsCurrent(lifetime))
                {
                    return;
                }

                _friends.SetLocalPlayerId(_onlineSession.LocalPlayerId);
                _friends.SetNotificationsConnected(_connected);
                _nextRefreshAt = Time.unscaledTimeAsDouble + _settings.FallbackRefreshSeconds;
                _friends.SetLastErrorKey(null);
                PublishLists();
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

        private void Update()
        {
            if (!IsReady)
            {
                return;
            }

            double now = Time.unscaledTimeAsDouble;
            TrySendPresence(now);
            if (_requestInFlight)
            {
                return;
            }

            if (_requests.Count > 0)
            {
                var request = _requests.Dequeue();
                _ = ExecuteRequestAsync(request.Kind, request.Id, true);
            }
            else if (!_connected && now >= _nextRefreshAt)
            {
                _ = ExecuteRequestAsync(Request.Refresh, null, false);
            }
        }

        private void HandleAddFriend(string id) => Enqueue(Request.Add, id);
        private void HandleAccept(string id) => Enqueue(Request.Accept, id);
        private void HandleDecline(string id) => Enqueue(Request.Decline, id);
        private void HandleCancelOutgoing(string id) => Enqueue(Request.CancelOutgoing, id);
        private void HandleRefresh() => Enqueue(Request.Refresh, null);

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

            _requests.Enqueue((kind, id?.Trim()));
        }

        private async Task ExecuteRequestAsync(Request kind, string id, bool userRequested)
        {
            _requestInFlight = true;
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
                    default:
                        await _sdk.ForceRelationshipsRefreshAsync();
                        info = FriendsErrorKeys.ListRefreshed;
                        break;
                }

                if (IsCurrent(lifetime))
                {
                    PublishLists();
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
                    ReportError(exception);
                }
            }
            finally
            {
                if (kind == Request.Refresh && IsCurrent(lifetime))
                {
                    _nextRefreshAt = Time.unscaledTimeAsDouble + _settings.FallbackRefreshSeconds;
                }

                _requestInFlight = false;
            }
        }

        private bool IsCurrent(int lifetime) => lifetime == _lifetime && CanInitialize;

        private void ReportError(Exception exception)
        {
            string key = exception switch
            {
                FriendsServiceException friendsException => FriendsErrorMapper.Map((int)friendsException.ErrorCode),
                RelationshipNotFoundException => FriendsErrorKeys.NotFriend,
                _ => FriendsErrorKeys.Unknown
            };
            _friends.SetLastInfoKey(null);
            _friends.SetLastErrorKey(key);
        }

        private void HandleRelationshipAdded(IRelationshipAddedEvent change) => PublishIfReady();
        private void HandleRelationshipDeleted(IRelationshipDeletedEvent change) => PublishIfReady();
        private void HandlePresenceUpdated(IPresenceUpdatedEvent change) => PublishIfReady();

        private void HandleConnectivityChanged(INotificationsStateChangedEvent change)
        {
            if (!CanInitialize)
            {
                return;
            }

            bool connected = change.State == FriendsEventConnectionState.Subscribed;
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
                    FriendPresenceActivity.Hub => FriendPresenceActivity.Hub,
                    FriendPresenceActivity.Room => FriendPresenceActivity.Room,
                    FriendPresenceActivity.Match => FriendPresenceActivity.Match,
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
            bool inMatch = _onlineSession.Status == OnlineSessionStatus.InSession &&
                _matchPhase is MatchPhase.Countdown or MatchPhase.Playing or MatchPhase.Results;
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
                    ReportError(exception);
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
