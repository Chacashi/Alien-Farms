using AlienFarms.Networking.Data;
using AlienFarms.Networking.Pooling;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Analytics;
using UnityEngine;
using NetworkSettings = AlienFarms.Data.NetworkSettings;

namespace AlienFarms.Networking.Diagnostics
{
    /// <summary>
    /// Online debug view (logic only; the label lives in the scene): this machine's pool counters for animals and bombs
    /// (active / in pool / created: "created" must stay flat during a match) and, while a UnityTransport runs, its
    /// receive side (packets per second, mean and max receive-queue usage against the capacity, both over the driver's
    /// lifetime) and the longest frame of the session. Works on host and clients (each has its own
    /// <see cref="NetworkSpawnRegistry"/>). The session is tracked every frame even while hidden; the label is sampled at
    /// <see cref="_refreshInterval"/> and rewritten only when a number changed, without allocating.
    /// Visibility follows <c>NetworkSettings.ShowPoolStats</c> unless <see cref="PoolStatsToggle"/> overrides it (F1–F4).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PoolStatsView : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Online settings: default visibility (Show Pool Stats).")]
        private NetworkSettings _settings;

        [SerializeField, Tooltip("Runtime anchor of the NetworkRoot (transport stats). Optional: without it only the pool lines are shown.")]
        private NetworkRuntimeAnchor _anchor;

        [SerializeField, Range(0.05f, 2f), Tooltip("Seconds between samples (0.25 = 4 Hz).")]
        private float _refreshInterval = 0.25f;

        [Header("References")]
        [SerializeField, Tooltip("Spawn registry of this scene (pool counters).")]
        private NetworkSpawnRegistry _registry;

        [SerializeField, Tooltip("Label the stats are written to. Rx line: packets/s of the last sample; 'cola' = mean and 'max' = most packets in one update, both over the driver's lifetime (since the session started), against the capacity; 'Frame max' = longest frame of the session.")]
        private TMP_Text _label;

        [SerializeField, Tooltip("Optional object shown / hidden with the view (e.g. the label's panel). Must not be this object; empty = only the label is toggled.")]
        private GameObject _content;

        private readonly char[] _buffer = new char[NetworkStatsFormatter.BufferSize];
        private readonly TransportStatsTracker _tracker = new TransportStatsTracker();
        private NetworkStatsSample _shown;
        private bool _hasShown;
        private bool _visible;
        private float _nextSample;

        private NetworkManager _manager;
        private UnityTransport _transport;

        private void OnEnable()
        {
            PoolStatsToggle.Changed += ApplyVisibility;
            ApplyVisibility();
        }

        private void OnDisable()
        {
            PoolStatsToggle.Changed -= ApplyVisibility;

            // Not ticked while disabled: re-enabling counts as shown again (packet rate re-based).
            _visible = false;
        }

        private void Update()
        {
            // Every frame, visible or not: a session that starts while hidden still resets its counters.
            _tracker.Tick(IsDriverRunning(), Time.unscaledDeltaTime);

            float now = Time.unscaledTime;
            if (!_visible || now < _nextSample)
            {
                return;
            }

            _nextSample = now + _refreshInterval;
            NetworkStatsSample sample = Sample(now);
            if (_hasShown && sample == _shown)
            {
                return;
            }

            _shown = sample;
            _hasShown = true;
            if (_label != null)
            {
                _label.SetCharArray(_buffer, 0, NetworkStatsFormatter.Write(_buffer, sample));
            }
        }

        private bool IsDriverRunning()
        {
            NetworkManager manager = _anchor != null ? _anchor.Manager : null;
            if (manager != _manager)
            {
                _manager = manager;
                _transport = manager != null ? manager.NetworkConfig.NetworkTransport as UnityTransport : null;
            }

            return _transport != null && manager.IsListening && _transport.GetNetworkDriver().IsCreated;
        }

        private NetworkStatsSample Sample(float now)
        {
            NetworkPoolStats animals = _registry != null ? _registry.AnimalStats : default;
            NetworkPoolStats bombs = _registry != null ? _registry.BombStats : default;
            if (!_tracker.IsRunning)
            {
                return new NetworkStatsSample(animals, bombs);
            }

            DriverStatistics stats = _transport.GetNetworkDriver().GetStatistics();
            return new NetworkStatsSample(
                animals,
                bombs,
                true,
                _tracker.SampleRx(stats.RxTotalPackets, now),
                Mathf.RoundToInt(stats.RxMeanQueueUsage * 10f),
                (int)stats.RxMaximumQueueUsage,
                _transport.MaxPacketQueueSize,
                Mathf.RoundToInt(_tracker.LongestFrame * 1000f));
        }

        private void ApplyVisibility()
        {
            bool wasVisible = _visible;
            _visible = PoolStatsToggle.IsVisible(_settings == null || _settings.ShowPoolStats);
            if (_content != null && _content != gameObject)
            {
                _content.SetActive(_visible);
            }

            if (_label != null)
            {
                _label.enabled = _visible;
            }

            // Shown (again): write the current numbers at once, the packet rate measured from now on.
            if (_visible && !wasVisible)
            {
                _tracker.RequestRebase();
            }

            _hasShown = false;
            _nextSample = 0f;
        }

        private void OnValidate()
        {
            if (_registry == null)
            {
                Debug.LogWarning($"[{nameof(PoolStatsView)}] Missing NetworkSpawnRegistry on '{name}'; the pool counters read 0.", this);
            }

            if (_label == null)
            {
                Debug.LogWarning($"[{nameof(PoolStatsView)}] Missing label on '{name}'.", this);
            }

            if (_settings == null)
            {
                Debug.LogWarning($"[{nameof(PoolStatsView)}] Missing NetworkSettings on '{name}'; the view is visible by default.", this);
            }

            if (_content == gameObject)
            {
                Debug.LogWarning($"[{nameof(PoolStatsView)}] Content on '{name}' is this object: hiding it would stop the view; use a child.", this);
            }
        }
    }
}
