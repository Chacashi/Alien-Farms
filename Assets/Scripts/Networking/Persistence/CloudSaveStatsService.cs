using AlienFarms.Data;
using AlienFarms.Networking.Data;
using UnityEngine;

namespace AlienFarms.Networking.Persistence
{
    /// <summary>
    /// Cloud stats service on the persistent NetworkRoot (task 2.8). Composition root of <see cref="PlayerStatsSync"/>:
    /// the anchor's <see cref="NetworkRuntimeAnchor.PlayerStats"/> ledger, Cloud Save (behind the simulated-failure
    /// decorator), the online session channel (UGS id, participants) and the progress channel it is the only writer of.
    /// Loads when the UGS id changes, saves whenever a round is pending, retries with backoff (cancelled in
    /// <see cref="OnDisable"/>) and reads the rival in the room. Event driven: no Update.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CloudSaveStatsService : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Retries, rival stats, logging and the simulated failure (Cloud Save header).")]
        private NetworkSettings _settings;

        [SerializeField, Tooltip("Online session channel: signed-in UGS id and room participants.")]
        private OnlineSessionChannel _session;

        [SerializeField, Tooltip("Progress channel read by the title stats, the save badge and the rival stats (this service is its only writer).")]
        private PlayerProgressChannel _progress;

        [SerializeField, Tooltip("Runtime anchor that owns the stats ledger (shared with the match stats recorder).")]
        private NetworkRuntimeAnchor _anchor;

        private PlayerStatsSync _sync;

        private void OnEnable()
        {
            if (_settings == null || _session == null || _progress == null || _anchor == null)
            {
                Debug.LogError($"[{nameof(CloudSaveStatsService)}] Missing NetworkSettings, OnlineSessionChannel, PlayerProgressChannel or NetworkRuntimeAnchor on '{name}'; cloud stats disabled.", this);
                return;
            }

            NetworkSettings settings = _settings;
            IPlayerStatsStore store = new SimulatedFailureStatsStore(
                new CloudSavePlayerStatsStore(), () => settings != null && settings.SimulateStatsFailure);
            _sync = new PlayerStatsSync(_anchor.PlayerStats, store, _session, _progress, _settings, this);
            _sync.Start();
        }

        private void OnDisable()
        {
            if (_sync != null)
            {
                _sync.Dispose();
                _sync = null;
            }
        }

        private void OnValidate()
        {
            if (_settings == null)
            {
                Debug.LogWarning($"[{nameof(CloudSaveStatsService)}] Missing NetworkSettings on '{name}'.", this);
            }

            if (_session == null)
            {
                Debug.LogWarning($"[{nameof(CloudSaveStatsService)}] Missing OnlineSessionChannel on '{name}'.", this);
            }

            if (_progress == null)
            {
                Debug.LogWarning($"[{nameof(CloudSaveStatsService)}] Missing PlayerProgressChannel on '{name}'.", this);
            }

            if (_anchor == null)
            {
                Debug.LogWarning($"[{nameof(CloudSaveStatsService)}] Missing NetworkRuntimeAnchor on '{name}'.", this);
            }
        }
    }
}
