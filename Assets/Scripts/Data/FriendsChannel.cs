using System;
using System.Collections.Generic;
using AlienFarms.Core;
using UnityEngine;

namespace AlienFarms.Data
{
    [CreateAssetMenu(fileName = "FriendsChannel", menuName = "Alien Farms/Networking/Friends Channel (Runtime)")]
    public sealed class FriendsChannel : ScriptableObject
    {
        private static int s_generation;

        [NonSerialized] private int _generation = -1;
        [NonSerialized] private FriendEntry[] _friends = new FriendEntry[FriendsSettings.DefaultMaxEntries];
        [NonSerialized] private FriendRequestEntry[] _requests = new FriendRequestEntry[FriendsSettings.DefaultMaxEntries];
        [NonSerialized] private int _friendCount;
        [NonSerialized] private int _requestCount;
        [NonSerialized] private FriendsServiceStatus _status;
        [NonSerialized] private string _localPlayerId;
        [NonSerialized] private bool _notificationsConnected;
        [NonSerialized] private string _lastErrorKey;
        [NonSerialized] private string _lastInfoKey;
        [NonSerialized] private SessionInviteOffer _pendingInvite;
        [NonSerialized] private bool _hasInvite;
        [NonSerialized] private int _openPanelCount;
        private Action<string> _addFriendRequested;
        private Action<string> _acceptRequestRequested;
        private Action<string> _declineRequestRequested;
        private Action<string> _cancelOutgoingRequested;
        private Action<string> _inviteRequested;
        private Action _refreshRequested;
        private Func<FriendEntry, bool> _inviteEligibility;

        public event Action Changed;

        public event Action<string> AddFriendRequested
        {
            add => AddListener(ref _addFriendRequested, value);
            remove => RemoveListener(ref _addFriendRequested, value);
        }

        public event Action<string> AcceptRequestRequested
        {
            add => AddListener(ref _acceptRequestRequested, value);
            remove => RemoveListener(ref _acceptRequestRequested, value);
        }

        public event Action<string> DeclineRequestRequested
        {
            add => AddListener(ref _declineRequestRequested, value);
            remove => RemoveListener(ref _declineRequestRequested, value);
        }

        public event Action<string> CancelOutgoingRequested
        {
            add => AddListener(ref _cancelOutgoingRequested, value);
            remove => RemoveListener(ref _cancelOutgoingRequested, value);
        }

        public event Action<string> InviteRequested
        {
            add => AddListener(ref _inviteRequested, value);
            remove => RemoveListener(ref _inviteRequested, value);
        }

        public void SetInviteEligibility(Func<FriendEntry, bool> provider) => Set(ref _inviteEligibility, provider);

        public bool IsPanelOpen => IsCurrent && _openPanelCount > 0;

        public void PanelOpened()
        {
            EnsureCurrent();
            _openPanelCount++;
        }

        public void PanelClosed()
        {
            if (IsPanelOpen)
            {
                _openPanelCount--;
            }
        }

        public bool CanInvite(FriendEntry entry) =>
            IsCurrent && !string.IsNullOrEmpty(entry.UgsPlayerId) && _inviteEligibility?.Invoke(entry) == true;

        public event Action RefreshRequested
        {
            add => AddListener(ref _refreshRequested, value);
            remove => RemoveListener(ref _refreshRequested, value);
        }

        public FriendsServiceStatus Status => IsCurrent ? _status : FriendsServiceStatus.Off;
        public string LocalPlayerId => IsCurrent ? _localPlayerId : null;
        public bool NotificationsConnected => IsCurrent && _notificationsConnected;
        public string LastErrorKey => IsCurrent ? _lastErrorKey : null;
        public string LastInfoKey => IsCurrent ? _lastInfoKey : null;
        public SessionInviteOffer PendingInvite => HasInvite ? _pendingInvite : default;
        public bool HasInvite => IsCurrent && _hasInvite;
        public int FriendCount => IsCurrent ? _friendCount : 0;
        public int RequestCount => IsCurrent ? _requestCount : 0;
        public int Capacity => _friends.Length;

        public bool IsAvailable => IsCurrent && (_addFriendRequested != null || _acceptRequestRequested != null ||
            _declineRequestRequested != null || _cancelOutgoingRequested != null || _inviteRequested != null ||
            _refreshRequested != null);

        private bool IsCurrent => _generation == s_generation;

        private void OnEnable()
        {
            hideFlags |= HideFlags.DontUnloadUnusedAsset;
        }

        public FriendEntry GetFriend(int index) => index >= 0 && index < FriendCount ? _friends[index] : default;
        public FriendRequestEntry GetRequest(int index) => index >= 0 && index < RequestCount ? _requests[index] : default;

        public bool RequestAddFriend(string id) => Dispatch(_addFriendRequested, id);
        public bool RequestAcceptRequest(string id) => Dispatch(_acceptRequestRequested, id);
        public bool RequestDeclineRequest(string id) => Dispatch(_declineRequestRequested, id);
        public bool RequestCancelOutgoing(string id) => Dispatch(_cancelOutgoingRequested, id);
        public bool RequestInvite(string friendId) => Dispatch(_inviteRequested, friendId);

        public bool RequestRefresh()
        {
            if (!IsCurrent || _refreshRequested == null)
            {
                return Reject();
            }

            Action request = _refreshRequested;
            ClearFeedback();
            request();
            return true;
        }

        public void SetStatus(FriendsServiceStatus value) => Set(ref _status, value);
        public void SetLocalPlayerId(string value) => Set(ref _localPlayerId, value);
        public void SetNotificationsConnected(bool value) => Set(ref _notificationsConnected, value);
        public void SetLastErrorKey(string value) => Set(ref _lastErrorKey, value);
        public void SetLastInfoKey(string value) => Set(ref _lastInfoKey, value);

        // Call during service setup using FriendsSettings.MaxEntries. Publishing never resizes these buffers.
        public void ConfigureCapacity(int capacity)
        {
            EnsureCurrent();
            capacity = Mathf.Max(1, capacity);
            if (capacity == Capacity)
            {
                return;
            }

            Array.Resize(ref _friends, capacity);
            Array.Resize(ref _requests, capacity);
            _friendCount = Mathf.Min(_friendCount, capacity);
            _requestCount = Mathf.Min(_requestCount, capacity);
            Changed?.Invoke();
        }

        public void SetFriends(FriendEntry[] entries, int count)
        {
            EnsureCurrent();
            CopyEntries(entries, count, _friends, ref _friendCount);
        }

        public void SetRequests(FriendRequestEntry[] entries, int count)
        {
            EnsureCurrent();
            CopyEntries(entries, count, _requests, ref _requestCount);
        }

        public void SetPendingInvite(SessionInviteOffer offer)
        {
            EnsureCurrent();
            _pendingInvite = offer;
            _hasInvite = true;
            Changed?.Invoke();
        }

        public bool ConsumeInvite(out SessionInviteOffer offer)
        {
            offer = PendingInvite;
            if (!HasInvite)
            {
                return false;
            }

            _hasInvite = false;
            _pendingInvite = default;
            Changed?.Invoke();
            return true;
        }

        public void DeclineInvite() => ConsumeInvite(out _);

        private void CopyEntries<T>(T[] entries, int count, T[] buffer, ref int publishedCount)
        {
            count = Mathf.Clamp(count, 0, Mathf.Min(buffer.Length, entries?.Length ?? 0));
            if (count > 0)
            {
                Array.Copy(entries, buffer, count);
            }

            if (count < publishedCount)
            {
                Array.Clear(buffer, count, publishedCount - count);
            }

            publishedCount = count;
            Changed?.Invoke();
        }

        private bool Dispatch(Action<string> request, string id)
        {
            if (!IsCurrent || request == null)
            {
                return Reject();
            }

            ClearFeedback();
            request(id);
            return true;
        }

        private bool Reject()
        {
            SetLastErrorKey(FriendsErrorKeys.NotEnabled);
            return false;
        }

        private void ClearFeedback()
        {
            if (_lastErrorKey == null && _lastInfoKey == null)
            {
                return;
            }

            _lastErrorKey = null;
            _lastInfoKey = null;
            Changed?.Invoke();
        }

        private void Set<T>(ref T field, T value)
        {
            EnsureCurrent();
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;
            Changed?.Invoke();
        }

        private void AddListener<T>(ref T listeners, T callback) where T : Delegate
        {
            EnsureCurrent();
            bool wasAvailable = IsAvailable;
            listeners = (T)Delegate.Combine(listeners, callback);
            if (wasAvailable != IsAvailable)
            {
                Changed?.Invoke();
            }
        }

        private void RemoveListener<T>(ref T listeners, T callback) where T : Delegate
        {
            bool wasAvailable = IsAvailable;
            listeners = (T)Delegate.Remove(listeners, callback);
            if (wasAvailable != IsAvailable)
            {
                Changed?.Invoke();
            }
        }

        private void EnsureCurrent()
        {
            if (IsCurrent)
            {
                return;
            }

            _generation = s_generation;
            _openPanelCount = 0;
            _status = FriendsServiceStatus.Off;
            _localPlayerId = null;
            _notificationsConnected = false;
            _lastErrorKey = null;
            _lastInfoKey = null;
            _pendingInvite = default;
            _hasInvite = false;
            Array.Clear(_friends, 0, _friendCount);
            Array.Clear(_requests, 0, _requestCount);
            _friendCount = 0;
            _requestCount = 0;
            _addFriendRequested = null;
            _acceptRequestRequested = null;
            _declineRequestRequested = null;
            _cancelOutgoingRequested = null;
            _inviteRequested = null;
            _inviteEligibility = null;
            _refreshRequested = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_generation++;
        }
    }
}
