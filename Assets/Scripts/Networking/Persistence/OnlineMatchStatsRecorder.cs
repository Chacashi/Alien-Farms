using System;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Networking.Data;
using AlienFarms.Networking.Player;
using AlienFarms.Networking.Session;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Persistence
{
    /// <summary>
    /// Records this machine's online rounds into the stats ledger (task 2.8). Lives under OnlineModeRoot (online only;
    /// local matches never count). Only a round whose Playing phase this instance saw live counts, once
    /// (<see cref="MatchStatsTally"/>); spectators record nothing. Each round is enqueued with a fresh batch id and the
    /// ledger flushes it to PlayerPrefs right away, so it survives closing the game; the cloud save service uploads it.
    /// <list type="bullet">
    /// <item>Round end (<see cref="GameEvents.OnMatchEnded"/>, forfeits included): win / draw / loss, animals in the own pen.</item>
    /// <item>Leaving on purpose during Playing (<see cref="RecordLeaveLoss"/>, called by the online exit before the
    /// shutdown): a loss.</item>
    /// <item>The host leaving during Playing (pure client, NGO drop reason <c>host_disconnected</c>): a win, and the
    /// ledger raises the host-left win notice the online menu shows (<see cref="PlayerStatsLedger.RaiseHostLeftWinNotice"/>).
    /// A drop without that reason (connection lost, host crash) records nothing: this machine cannot know the result.</item>
    /// </list>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OnlineMatchStatsRecorder : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Runtime anchor of the NetworkRoot: local avatar (seat), NGO manager and the stats ledger.")]
        private NetworkRuntimeAnchor _anchor;

        [SerializeField, Tooltip("Same AnimalSpawnSettings as the animal spawner: its entries are the pen's animal types.")]
        private AnimalSpawnSettings _spawnSettings;

        [SerializeField, Tooltip("Network settings (persistence logging).")]
        private NetworkSettings _settings;

        private readonly MatchStatsTally _tally = new MatchStatsTally();
        private PlayerId _localId = PlayerId.None;
        private NetworkManager _watchedManager;

        private void OnEnable()
        {
            GameEvents.OnMatchPhaseChanged += HandleMatchPhaseChanged;
            GameEvents.OnMatchEnded += HandleMatchEnded;

            if (_anchor != null && _anchor.IsPureClient)
            {
                _watchedManager = _anchor.Manager;
                _watchedManager.OnClientDisconnectCallback += HandleClientDisconnected;
            }
        }

        private void OnDisable()
        {
            GameEvents.OnMatchPhaseChanged -= HandleMatchPhaseChanged;
            GameEvents.OnMatchEnded -= HandleMatchEnded;

            if (_watchedManager != null)
            {
                _watchedManager.OnClientDisconnectCallback -= HandleClientDisconnected;
                _watchedManager = null;
            }
        }

        /// <summary>
        /// This player leaves on purpose (pause / results exit). During live Playing it records a loss, persisted before
        /// the caller shuts the session down; otherwise nothing.
        /// </summary>
        public void RecordLeaveLoss()
        {
            TryRecordEarly(false, "left on purpose");
        }

        /// <summary>
        /// Tackles this player landed in the round. Until task 3.2 nobody counts them, so 0. 3.2 changes only this method:
        /// <c>local.State.TackleStats.Landed</c> (an owner-readable NetworkVariable reset by the server every round).
        /// </summary>
        private static int CountLocalTacklesLanded(NetworkPlayer local)
        {
            return 0;
        }

        private void HandleMatchPhaseChanged(MatchPhase previous, MatchPhase current)
        {
            _tally.OnPhaseChanged(previous, current);
            if (current == MatchPhase.Countdown || current == MatchPhase.Playing)
            {
                RefreshLocalId();
                if (_anchor != null)
                {
                    _anchor.PlayerStats.MarkRoundStarted();
                }
            }
        }

        private void HandleMatchEnded(MatchResult result)
        {
            RefreshLocalId();
            NetworkPlayer local = _anchor != null ? _anchor.LocalPlayer : null;
            if (!_tally.TryComplete(_localId, result, CountPenAnimals(_localId), CountLocalTacklesLanded(local), out PlayerStatsSummary delta))
            {
                return;
            }

            Enqueue(delta, result.IsForfeit ? "round (forfeit)" : "round");
        }

        // Pure client: NGO reports this client's drop with the server's reason before it stops.
        private void HandleClientDisconnected(ulong clientId)
        {
            NetworkManager manager = _watchedManager;
            if (manager == null || clientId != manager.LocalClientId
                || DisconnectReasons.Normalize(manager.DisconnectReason) != DisconnectReasons.HostDisconnected)
            {
                return;
            }

            TryRecordEarly(true, "host left");
        }

        private void TryRecordEarly(bool localWon, string cause)
        {
            RefreshLocalId();
            NetworkPlayer local = _anchor != null ? _anchor.LocalPlayer : null;
            if (_localId != PlayerId.Player1 && _localId != PlayerId.Player2)
            {
                return;
            }

            if (!_tally.TryCompleteEarly(localWon, CountPenAnimals(_localId), CountLocalTacklesLanded(local), out PlayerStatsSummary delta))
            {
                return;
            }

            if (Enqueue(delta, cause) && localWon)
            {
                _anchor.PlayerStats.RaiseHostLeftWinNotice();
            }
        }

        private bool Enqueue(in PlayerStatsSummary delta, string cause)
        {
            if (_anchor == null)
            {
                return false;
            }

            PlayerStatsLedger ledger = _anchor.PlayerStats;
            bool queued = ledger.TryEnqueue(delta, Guid.NewGuid().ToString("N"));
            if (_settings == null || _settings.LogPersistence)
            {
                Debug.Log($"[{nameof(OnlineMatchStatsRecorder)}] {_localId} {cause}: +{delta.MatchesPlayed} match, {delta.Wins}W/{delta.Draws}D/{delta.Losses}L, {delta.AnimalsScored} animals, {delta.TacklesLanded} tackles ({(queued ? "queued" : "not queued: signed out")}).", this);
            }

            return queued;
        }

        // The seat is cached while the avatar exists (on a drop it may already be gone: the cached value stays).
        private void RefreshLocalId()
        {
            NetworkPlayer local = _anchor != null ? _anchor.LocalPlayer : null;
            if (local != null)
            {
                _localId = local.PlayerId;
            }
        }

        private int CountPenAnimals(PlayerId id)
        {
            if (_spawnSettings == null || _spawnSettings.Entries == null)
            {
                return 0;
            }

            int types = Mathf.Min(_spawnSettings.Entries.Length, PenInventorySnapshot.MaxTypeCount);
            int total = 0;
            for (int i = 0; i < types; i++)
            {
                total += PenInventorySnapshot.Get(id, i);
            }

            return total;
        }

        private void OnValidate()
        {
            if (_anchor == null)
            {
                Debug.LogWarning($"[{nameof(OnlineMatchStatsRecorder)}] Missing NetworkRuntimeAnchor on '{name}'.", this);
            }

            if (_spawnSettings == null)
            {
                Debug.LogWarning($"[{nameof(OnlineMatchStatsRecorder)}] Missing AnimalSpawnSettings on '{name}'; animals count as 0.", this);
            }

        }
    }
}
