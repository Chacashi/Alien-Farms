using System;
using AlienFarms.Core;
using UnityEngine;

namespace AlienFarms.Data
{
    /// <summary>
    /// Runtime channel with the local player's persistent progress (lifetime stats). The title screen only reads it;
    /// the cloud save service (task 2.8) is the only writer. Until then it stays <see cref="PlayerProgressStatus.Unavailable"/>.
    /// Nothing is serialized: state from a previous play session is discarded through a generation counter.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerProgressChannel", menuName = "Alien Farms/Networking/Player Progress Channel (Runtime)")]
    public sealed class PlayerProgressChannel : ScriptableObject
    {
        private static int s_generation;

        [NonSerialized] private int _generation = -1;
        [NonSerialized] private PlayerProgressStatus _status;
        [NonSerialized] private PlayerStatsSummary _stats;
        [NonSerialized] private PlayerProgressSaveStatus _saveStatus;
        [NonSerialized] private string _lastErrorKey;

        /// <summary>Raised after any state change (status, stats, save status, error).</summary>
        public event Action Changed;

        public PlayerProgressStatus Status => IsCurrent ? _status : PlayerProgressStatus.Unavailable;

        /// <summary>Last loaded stats; default (all zero) until the first load.</summary>
        public PlayerStatsSummary Stats => IsCurrent ? _stats : default;

        /// <summary>True when <see cref="Stats"/> holds loaded values.</summary>
        public bool HasStats => Status == PlayerProgressStatus.Loaded;

        public PlayerProgressSaveStatus SaveStatus => IsCurrent ? _saveStatus : PlayerProgressSaveStatus.Idle;

        /// <summary>Localization key of the last load failure, null when none.</summary>
        public string LastErrorKey => IsCurrent ? _lastErrorKey : null;

        public bool HasError => !string.IsNullOrEmpty(LastErrorKey);

        private bool IsCurrent => _generation == s_generation;

        private void OnEnable()
        {
            // Read from the MainMenu and written by a persistent service: keep the runtime state across scene loads.
            hideFlags |= HideFlags.DontUnloadUnusedAsset;
        }

        /// <summary>Back to the initial state (for example after signing out).</summary>
        public void SetUnavailable()
        {
            Apply(PlayerProgressStatus.Unavailable, default, PlayerProgressSaveStatus.Idle, null);
        }

        /// <summary>Loading started; keeps the previous stats and clears the error.</summary>
        public void SetLoading()
        {
            EnsureCurrent();
            Apply(PlayerProgressStatus.Loading, _stats, _saveStatus, null);
        }

        public void SetLoaded(in PlayerStatsSummary stats)
        {
            EnsureCurrent();
            Apply(PlayerProgressStatus.Loaded, stats, _saveStatus, null);
        }

        /// <summary>Loading failed; keeps the previous stats and records <paramref name="errorKey"/>.</summary>
        public void SetFailed(string errorKey)
        {
            EnsureCurrent();
            Apply(PlayerProgressStatus.Failed, _stats, _saveStatus, errorKey);
        }

        public void SetSaveStatus(PlayerProgressSaveStatus saveStatus)
        {
            EnsureCurrent();
            Apply(_status, _stats, saveStatus, _lastErrorKey);
        }

        private void Apply(PlayerProgressStatus status, in PlayerStatsSummary stats, PlayerProgressSaveStatus saveStatus, string errorKey)
        {
            EnsureCurrent();
            bool changed = status != _status
                || !stats.Equals(_stats)
                || saveStatus != _saveStatus
                || !string.Equals(errorKey, _lastErrorKey, StringComparison.Ordinal);
            if (!changed)
            {
                return;
            }

            _status = status;
            _stats = stats;
            _saveStatus = saveStatus;
            _lastErrorKey = errorKey;
            Changed?.Invoke();
        }

        private void EnsureCurrent()
        {
            if (IsCurrent)
            {
                return;
            }

            _generation = s_generation;
            _status = PlayerProgressStatus.Unavailable;
            _stats = default;
            _saveStatus = PlayerProgressSaveStatus.Idle;
            _lastErrorKey = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_generation++;
        }
    }
}
