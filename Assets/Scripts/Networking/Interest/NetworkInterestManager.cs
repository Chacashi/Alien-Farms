using System.Collections.Generic;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Match;
using AlienFarms.Networking.Data;
using AlienFarms.Networking.Player;
using AlienFarms.Networking.Session;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Interest
{
    [DisallowMultipleComponent]
    public sealed class NetworkInterestManager : MonoBehaviour, IInterestPolicy
    {
        [SerializeField, Tooltip("Server-side interest filtering settings.")] private InterestSettings _settings;
        [SerializeField, Tooltip("Network runtime anchor for the session and observer avatars.")] private NetworkRuntimeAnchor _anchor;
        [SerializeField, Tooltip("Online roster providing fallback spawn positions.")] private NetworkPlayerRoster _roster;

        [SerializeField, Tooltip("Authoritative match phase when interest filtering starts.")] private MatchManager _match;

        private readonly List<NetworkCarriable> _tracked = new List<NetworkCarriable>(128);
        private readonly HashSet<NetworkCarriable> _dirty = new HashSet<NetworkCarriable>(128);
        private readonly List<NetworkObject> _toShow = new List<NetworkObject>(64);
        private readonly List<NetworkObject> _toHide = new List<NetworkObject>(64);
        private readonly InterestCounters[] _counters = new InterestCounters[ServerSlotDirectory.Capacity];

        private NetworkManager _server;
        private NetworkTickSystem _tickSystem;
        private MatchPhase _phase;
        private float _windowStart;
        private int _ticksSinceReview;
        private int _lastReviewFrame = -1;
        private bool _active;
        private bool _hasWindow;
        private bool _countsDirty;

        public bool IsActive => _active;
        public float WindowSeconds => _hasWindow ? Time.unscaledTime - _windowStart : 0f;
        public int TrackedCount => _tracked.Count;
        public float EnterRadius => _settings != null ? _settings.EnterRadius : 0f;
        public float ExitRadius => _settings != null ? _settings.ExitRadius : 0f;
        public int ReviewIntervalTicks => _settings != null ? _settings.ReviewIntervalTicks : 1;

        private NetworkManager Manager => _anchor != null ? _anchor.Manager : null;

        private void OnEnable()
        {
            TryStart();
        }

        private void OnDisable()
        {
            Stop();
        }

        private void OnDestroy() => ClearTracking();

        private void TryStart()
        {
            NetworkManager manager = Manager;
            if (_tickSystem != null)
            {
                if (manager != null && _tickSystem == manager.NetworkTickSystem)
                {
                    return;
                }

                Stop();
            }

            if (!isActiveAndEnabled || manager == null ||
                !manager.IsListening || !manager.IsServer || manager.NetworkTickSystem == null)
            {
                return;
            }

            _active = _settings != null && InterestToggle.IsEnabled(_settings.Enabled);
            HandleMatchPhaseChanged(MatchPhase.None, _match != null ? _match.CurrentPhase : MatchPhase.None);
            _server = manager;
            _tickSystem = manager.NetworkTickSystem;
            _tickSystem.Tick += HandleTick;
            manager.OnServerStopped += HandleServerStopped;
            InterestToggle.Changed += HandleToggleChanged;
            GameEvents.OnMatchPhaseChanged += HandleMatchPhaseChanged;

            if (_active && _tracked.Count > 0)
            {
                BindTracked();
                Review(manager, dirtyOnly: false, showLimit: _settings.MaxShowsPerReview);
            }
        }

        private void Stop()
        {
            if (_tickSystem == null)
            {
                return;
            }

            NetworkManager manager = _server;
            bool sameTickSystem = manager != null && _tickSystem == manager.NetworkTickSystem;
            _server = null;
            _tickSystem.Tick -= HandleTick;
            _tickSystem = null;
            if (manager != null)
            {
                manager.OnServerStopped -= HandleServerStopped;
            }

            InterestToggle.Changed -= HandleToggleChanged;
            GameEvents.OnMatchPhaseChanged -= HandleMatchPhaseChanged;
            _active = false;
            BindTracked();

            if (sameTickSystem && manager == Manager && manager.IsListening && manager.IsServer && !manager.ShutdownInProgress)
            {
                Review(manager, dirtyOnly: false, showLimit: 0);
                return;
            }

            ClearTracking();
        }

        private void ClearTracking()
        {
            while (_tracked.Count > 0)
            {
                RemoveAt(_tracked.Count - 1);
            }

            _dirty.Clear();
            _countsDirty = false;
            _phase = default;
            _ticksSinceReview = 0;
            _lastReviewFrame = -1;
            _hasWindow = false;
            System.Array.Clear(_counters, 0, _counters.Length);
        }

        private void HandleServerStopped(bool wasHost)
        {
            Stop();
            ClearTracking();
        }

        public void Register(NetworkCarriable target)
        {
            TryStart();
            NetworkManager manager = Manager;
            if (manager == null || !manager.IsListening || !manager.IsServer ||
                manager.ShutdownInProgress || target.InterestIndex >= 0)
            {
                return;
            }

            target.ServerBindInterest(_active ? this : null);
            target.InterestIndex = _tracked.Count;
            _tracked.Add(target);
            target.ViewChanged += MarkDirty;
        }

        public void Unregister(NetworkCarriable target)
        {
            int index = target.InterestIndex;
            if (index >= 0 && index < _tracked.Count && ReferenceEquals(_tracked[index], target))
            {
                RemoveAt(index);
            }
        }

        private void RemoveAt(int index)
        {
            NetworkCarriable target = _tracked[index];
            if (target != null)
            {
                target.ServerBindInterest(null);
                target.InterestIndex = -1;
                target.ViewChanged -= MarkDirty;
            }

            _dirty.Remove(target);
            int lastIndex = _tracked.Count - 1;
            if (index != lastIndex)
            {
                NetworkCarriable last = _tracked[lastIndex];
                _tracked[index] = last;
                if (last != null)
                {
                    last.InterestIndex = index;
                }
            }

            _tracked.RemoveAt(lastIndex);
            _countsDirty = true;
        }

        private void MarkDirty(NetworkCarriable target) => _dirty.Add(target);

        public bool Evaluate(NetworkCarriable target, ulong clientId)
        {
            if (!_active)
            {
                return true;
            }

            bool hasPosition = TryGetObserverPosition(clientId, out Vector3 position, out _, out _);
            return Evaluate(target, clientId, hasPosition, position, InterestRadii.Create(EnterRadius, ExitRadius));
        }

        private bool Evaluate(NetworkCarriable target, ulong clientId, bool hasPosition, Vector3 position,
            in InterestRadii radii)
        {
            bool filtered = _active && (target.InterestKind == InterestKind.Animal
                ? _settings.FilterAnimals : _settings.FilterBombs);
            NetworkObject networkObject = target.NetworkObject;
            var query = new InterestQuery(_active, filtered, clientId == NetworkManager.ServerClientId,
                networkObject.OwnerClientId == clientId, target.IsInterestExempt, hasPosition,
                networkObject.IsNetworkVisibleTo(clientId),
                hasPosition ? InterestRules.SqrHorizontalDistance(target.transform.position, position) : 0f);

            return InterestRules.ShouldBeVisible(query, radii);
        }

        public void EnsureVisibleTo(NetworkCarriable target, ulong clientId)
        {
            NetworkObject networkObject = target.NetworkObject;
            if (!_active || !networkObject.IsSpawned || clientId == NetworkManager.ServerClientId ||
                networkObject.IsNetworkVisibleTo(clientId))
            {
                return;
            }

            // The simulation has already entered its exempt carried state.
            networkObject.NetworkShow(clientId);
            if (networkObject.IsNetworkVisibleTo(clientId))
            {
                TryGetObserverPosition(clientId, out _, out PlayerId seat, out string source);
                RecordChanges(seat, clientId, 1, 0, source);
                _countsDirty = true;
            }
        }

        public bool TryGetCounters(PlayerId seat, out InterestCounters counters)
        {
            int index = (int)seat - 1;
            if (_tickSystem != null && (uint)index < (uint)_counters.Length)
            {
                counters = _counters[index];
                return true;
            }

            counters = default;
            return false;
        }

        private bool TryGetObserverPosition(ulong clientId, out Vector3 position, out PlayerId seat, out string source)
        {
            if (_anchor.TryGetPlayerByClient(clientId, out NetworkPlayer player))
            {
                position = player.transform.position;
                seat = player.PlayerId;
                source = "avatar";
                return true;
            }

            seat = PlayerId.None;
            ServerSlotDirectory slots = _anchor.Slots;
            if (slots != null && slots.TryGetByClient(clientId, out PlayerSlot slot))
            {
                seat = slot.Id;
                if (slot.HasSavedPose)
                {
                    position = slot.SavedPosition;
                    source = "savedPose";
                    return true;
                }

                if (_roster != null && _roster.TryGetSpawnPosition(seat, out position))
                {
                    source = "spawn";
                    return true;
                }
            }

            position = default;
            source = "none";
            return false;
        }

        private void HandleTick()
        {
            if (_ticksSinceReview < ReviewIntervalTicks)
            {
                _ticksSinceReview++;
            }
        }

        private void LateUpdate()
        {
            TryStart();
            NetworkManager manager = Manager;
            if (_tickSystem == null || manager == null || !manager.IsServer || manager.ShutdownInProgress)
            {
                return;
            }

            bool periodic = _ticksSinceReview >= ReviewIntervalTicks && _lastReviewFrame != Time.frameCount;
            if (periodic)
            {
                _ticksSinceReview = 0;
                _lastReviewFrame = Time.frameCount;

                for (int i = _tracked.Count - 1; i >= 0; i--)
                {
                    NetworkCarriable target = _tracked[i];
                    if (target == null || !target.IsSpawned)
                    {
                        RemoveAt(i);
                    }
                }
            }

            if (_dirty.Count > 0)
            {
                if (_active)
                {
                    Review(manager, dirtyOnly: true, showLimit: 0);
                }

                _dirty.Clear();
                _countsDirty = true;
            }

            if (periodic && _active)
            {
                Review(manager, dirtyOnly: false, showLimit: _settings.MaxShowsPerReview);
            }

            if (periodic || _countsDirty)
            {
                UpdateVisibleCounts(manager);
                _countsDirty = false;
            }
        }

        private void Review(NetworkManager manager, bool dirtyOnly, int showLimit)
        {
            InterestRadii radii = InterestRadii.Create(EnterRadius, ExitRadius);
            IReadOnlyList<ulong> clients = manager.ConnectedClientsIds;
            for (int i = 0; i < clients.Count; i++)
            {
                ulong clientId = clients[i];
                if (clientId == NetworkManager.ServerClientId)
                {
                    continue;
                }

                bool hasPosition = TryGetObserverPosition(clientId, out Vector3 position, out PlayerId seat, out string source);
                if (dirtyOnly)
                {
                    foreach (NetworkCarriable target in _dirty)
                    {
                        CollectChange(target, clientId, hasPosition, position, radii, showLimit);
                    }
                }
                else
                {
                    foreach (NetworkCarriable target in _tracked)
                    {
                        CollectChange(target, clientId, hasPosition, position, radii, showLimit);
                    }
                }

                if (_toHide.Count > 0)
                {
                    NetworkObject.NetworkHide(_toHide, clientId);
                }

                if (_toShow.Count > 0)
                {
                    NetworkObject.NetworkShow(_toShow, clientId);
                }

                RecordChanges(seat, clientId, _toShow.Count, _toHide.Count, source);
                _toShow.Clear();
                _toHide.Clear();
            }
        }

        private void CollectChange(NetworkCarriable target, ulong clientId, bool hasPosition, Vector3 position,
            in InterestRadii radii, int showLimit)
        {
            if (target == null || !target.IsSpawned)
            {
                return;
            }

            NetworkObject networkObject = target.NetworkObject;
            bool wasVisible = networkObject.IsNetworkVisibleTo(clientId);
            bool shouldBeVisible = Evaluate(target, clientId, hasPosition, position, radii);
            switch (InterestRules.Classify(wasVisible, shouldBeVisible))
            {
                case InterestChange.Show when showLimit == 0 || _toShow.Count < showLimit:
                    _toShow.Add(networkObject);
                    break;
                case InterestChange.Hide:
                    _toHide.Add(networkObject);
                    break;
            }
        }

        private void RecordChanges(PlayerId seat, ulong clientId, int shows, int hides, string source)
        {
            int index = (int)seat - 1;
            if ((uint)index < (uint)_counters.Length)
            {
                _counters[index].Shows += shows;
                _counters[index].Hides += hides;
            }

            if ((shows > 0 || hides > 0) && _settings != null && _settings.LogChanges)
            {
                Debug.Log($"[{nameof(NetworkInterestManager)}] Client {clientId} ({seat}): +{shows} -{hides}, position {source}.", this);
            }
        }

        private void UpdateVisibleCounts(NetworkManager manager)
        {
            for (int i = 0; i < _counters.Length; i++)
            {
                _counters[i].VisibleAnimals = 0;
                _counters[i].VisibleBombs = 0;
            }

            IReadOnlyList<ulong> clients = manager.ConnectedClientsIds;
            for (int i = 0; i < clients.Count; i++)
            {
                ulong clientId = clients[i];
                TryGetObserverPosition(clientId, out _, out PlayerId seat, out _);
                int index = (int)seat - 1;
                if ((uint)index >= (uint)_counters.Length)
                {
                    continue;
                }

                foreach (NetworkCarriable target in _tracked)
                {
                    if (target == null || !target.IsSpawned || !target.NetworkObject.IsNetworkVisibleTo(clientId))
                    {
                        continue;
                    }

                    if (target.InterestKind == InterestKind.Animal)
                    {
                        _counters[index].VisibleAnimals++;
                    }
                    else
                    {
                        _counters[index].VisibleBombs++;
                    }
                }
            }
        }

        private void HandleToggleChanged()
        {
            bool enabled = _settings != null && InterestToggle.IsEnabled(_settings.Enabled);
            if (enabled == _active)
            {
                return;
            }

            if (_phase == MatchPhase.Playing)
            {
                Debug.Log($"[{nameof(NetworkInterestManager)}] Ignoring AOI toggle during Playing.", this);
                InterestToggle.Set(_active);
                return;
            }

            NetworkManager manager = Manager;
            if (manager == null || !manager.IsServer || manager.ShutdownInProgress)
            {
                return;
            }

            _active = enabled;
            BindTracked();
            Review(manager, dirtyOnly: false, showLimit: _active ? _settings.MaxShowsPerReview : 0);
            UpdateVisibleCounts(manager);
        }

        private void BindTracked()
        {
            foreach (NetworkCarriable target in _tracked)
            {
                if (target != null)
                {
                    target.ServerBindInterest(_active ? this : null);
                }
            }
        }

        private void HandleMatchPhaseChanged(MatchPhase previous, MatchPhase current)
        {
            _phase = current;
            if (current == MatchPhase.Playing)
            {
                for (int i = 0; i < _counters.Length; i++)
                {
                    _counters[i].Shows = 0;
                    _counters[i].Hides = 0;
                }

                _windowStart = Time.unscaledTime;
                _hasWindow = true;
            }
        }

        private void OnValidate()
        {
            if (_settings == null || _anchor == null || _roster == null || _match == null)
            {
                Debug.LogWarning($"[{nameof(NetworkInterestManager)}] Assign interest settings, runtime anchor, online roster and match manager.", this);
            }
        }
    }
}
