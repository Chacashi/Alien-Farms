using System;
using AlienFarms.Core;
using AlienFarms.Networking.Data;
using AlienFarms.Networking.Interest;
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
    [DisallowMultipleComponent]
    public sealed class InterestStatsView : MonoBehaviour
    {
        [SerializeField, Tooltip("Online settings providing default pool stats visibility.")] private NetworkSettings _settings;
        [SerializeField, Tooltip("Runtime anchor for the local network session.")] private NetworkRuntimeAnchor _anchor;
        [SerializeField, Tooltip("Optional server-side visibility manager.")] private NetworkInterestManager _interest;
        [SerializeField, Tooltip("Spawn registry providing active pool totals.")] private NetworkSpawnRegistry _registry;
        [SerializeField, Tooltip("Label receiving the AOI stats.")] private TMP_Text _label;
        [SerializeField, Tooltip("Optional child panel shown or hidden with the stats.")] private GameObject _content;
        [SerializeField, Range(0.05f, 2f), Tooltip("Seconds between stats samples.")] private float _refreshInterval = 0.25f;

        private readonly char[] _buffer = new char[InterestStatsFormatter.BufferSize];
        private readonly KnownObjectsCounter _knownObjects = new KnownObjectsCounter();
        private InterestStatsSample _shown;
        private bool _hasShown;
        private bool _visible;
        private bool _hasWindow;
        private float _nextSample;
        private float _windowStart;
        private ulong _bytesAtWindowStart;

        private NetworkManager Manager => _anchor != null ? _anchor.Manager : null;

        private void OnEnable()
        {
            PoolStatsToggle.Changed += ApplyVisibility;
            GameEvents.OnMatchPhaseChanged += HandleMatchPhaseChanged;
            ApplyVisibility();
        }

        private void OnDisable()
        {
            PoolStatsToggle.Changed -= ApplyVisibility;
            GameEvents.OnMatchPhaseChanged -= HandleMatchPhaseChanged;
            _hasWindow = false;
            _knownObjects.Clear();
        }

        private void ApplyVisibility()
        {
            _visible = PoolStatsToggle.IsVisible(_settings == null || _settings.ShowPoolStats);
            if (_content != null && _content != gameObject)
            {
                _content.SetActive(_visible);
            }

            if (_label != null)
            {
                _label.enabled = _visible;
            }

            _hasShown = false;
            _nextSample = 0f;
        }

        private void HandleMatchPhaseChanged(MatchPhase previous, MatchPhase current)
        {
            if (current == MatchPhase.Playing)
            {
                NetworkManager manager = Manager;
                _bytesAtWindowStart = ReadTrafficBytes(manager, manager != null && manager.IsServer);
                _windowStart = Time.unscaledTime;
                _hasWindow = true;
                _nextSample = 0f;
            }
        }

        private void LateUpdate()
        {
            float now = Time.unscaledTime;
            if (!_visible || _label == null || now < _nextSample)
            {
                return;
            }

            _nextSample = now + _refreshInterval;
            InterestStatsSample sample = Sample(now);
            if (!_hasShown || !_shown.Equals(sample))
            {
                _shown = sample;
                _hasShown = true;
                _label.SetCharArray(_buffer, 0, InterestStatsFormatter.Write(_buffer, sample));
            }
        }

        private InterestStatsSample Sample(float now)
        {
            NetworkManager manager = Manager;
            bool isHost = manager != null && manager.IsServer;
            bool hasInterest = isHost && _interest != null && _interest.isActiveAndEnabled;
            KnownObjectsCounts known = _knownObjects.Count(manager);
            int totalAnimals = isHost && _registry != null ? _registry.AnimalStats.Active : known.Animals;
            int totalBombs = isHost && _registry != null ? _registry.BombStats.Active : known.Bombs;

            InterestCounters remote = default;
            if (hasInterest)
            {
                _interest.TryGetCounters(PlayerId.Player2, out remote);
            }
            else if (isHost && manager.ConnectedClientsIds.Count > 1)
            {
                remote.VisibleAnimals = known.Animals;
                remote.VisibleBombs = known.Bombs;
            }

            float elapsed = _hasWindow ? now - _windowStart : 0f;
            ulong currentBytes = _hasWindow ? ReadTrafficBytes(manager, isHost) : 0;
            double trafficKb = currentBytes >= _bytesAtWindowStart ? (currentBytes - _bytesAtWindowStart) / 1024d : 0d;
            long rateTenths = elapsed > 0f ? (long)Math.Round(trafficKb * 10d / elapsed) : 0;
            return new InterestStatsSample(isHost, hasInterest && _interest.IsActive,
                hasInterest ? _interest.EnterRadius : 0f, hasInterest ? _interest.ExitRadius : 0f,
                hasInterest ? _interest.ReviewIntervalTicks : 0, known, remote, totalAnimals, totalBombs,
                (int)(hasInterest ? _interest.WindowSeconds : elapsed), isHost, (long)Math.Round(trafficKb), rateTenths);
        }

        private static ulong ReadTrafficBytes(NetworkManager manager, bool isTx)
        {
            if (manager == null || !manager.IsListening || manager.NetworkConfig.NetworkTransport is not UnityTransport transport)
            {
                return 0;
            }

            NetworkDriver driver = transport.GetNetworkDriver();
            if (!driver.IsCreated)
            {
                return 0;
            }

            DriverStatistics stats = driver.GetStatistics();
            return isTx ? stats.TxTotalBytes : stats.RxTotalBytes;
        }

        private void OnValidate()
        {
            if (_settings == null || _anchor == null || _registry == null || _label == null || _content == gameObject)
            {
                Debug.LogWarning($"[{nameof(InterestStatsView)}] Assign settings, runtime anchor, registry and label. Use a child for optional content.", this);
            }
        }
    }
}
