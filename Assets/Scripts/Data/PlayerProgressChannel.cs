using System;
using AlienFarms.Core;
using UnityEngine;

namespace AlienFarms.Data
{
    /// <summary>
    /// Runtime channel with the local player's persistent progress (lifetime stats) and, in the online room, the rival's
    /// public stats. Views only read it; the cloud save service (task 2.8) is the only writer. Until then it stays
    /// <see cref="PlayerProgressStatus.Unavailable"/>. Nothing is serialized: state from a previous play session is
    /// discarded through a generation counter.
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
        [NonSerialized] private PlayerProgressStatus _rivalStatus;
        [NonSerialized] private PlayerStatsSummary _rivalStats;
        [NonSerialized] private bool _hostLeftWinNotice;

        /// <summary>Raised after any state change (status, stats, save status, error, rival, host-left notice).</summary>
        public event Action Changed;

        /// <summary>Service side: the online menu showed the host-left win notice (see <see cref="RequestConsumeHostLeftWinNotice"/>).</summary>
        public event Action HostLeftWinNoticeConsumeRequested;

        public PlayerProgressStatus Status => IsCurrent ? _status : PlayerProgressStatus.Unavailable;

        /// <summary>Last loaded stats; default (all zero) until the first load.</summary>
        public PlayerStatsSummary Stats => IsCurrent ? _stats : default;

        /// <summary>True when <see cref="Stats"/> holds loaded values.</summary>
        public bool HasStats => Status == PlayerProgressStatus.Loaded;

        public PlayerProgressSaveStatus SaveStatus => IsCurrent ? _saveStatus : PlayerProgressSaveStatus.Idle;

        /// <summary>Localization key of the last load failure, null when none.</summary>
        public string LastErrorKey => IsCurrent ? _lastErrorKey : null;

        public bool HasError => !string.IsNullOrEmpty(LastErrorKey);

        /// <summary>Load state of the rival's public stats (Unavailable when there is no rival to read).</summary>
        public PlayerProgressStatus RivalStatus => IsCurrent ? _rivalStatus : PlayerProgressStatus.Unavailable;

        /// <summary>The rival's public stats; default (all zero) unless <see cref="HasRivalStats"/>.</summary>
        public PlayerStatsSummary RivalStats => IsCurrent ? _rivalStats : default;

        /// <summary>True when <see cref="RivalStats"/> holds loaded values.</summary>
        public bool HasRivalStats => RivalStatus == PlayerProgressStatus.Loaded;

        /// <summary>
        /// True while a win recorded because the host left during Playing waits to be announced by the online menu
        /// (which shows it instead of the plain "host left" error, then asks to consume it).
        /// </summary>
        public bool HasHostLeftWinNotice => IsCurrent && _hostLeftWinNotice;

        private bool IsCurrent => _generation == s_generation;

        private void OnEnable()
        {
            // Read from the MainMenu and written by a persistent service: keep the runtime state across scene loads.
            hideFlags |= HideFlags.DontUnloadUnusedAsset;
        }

        /// <summary>Back to the initial state (for example after signing out); also forgets the rival.</summary>
        public void SetUnavailable()
        {
            EnsureCurrent();
            Apply(PlayerProgressStatus.Unavailable, default, PlayerProgressSaveStatus.Idle, null,
                PlayerProgressStatus.Unavailable, default);
        }

        /// <summary>Loading started; keeps the previous stats and clears the error.</summary>
        public void SetLoading()
        {
            EnsureCurrent();
            Apply(PlayerProgressStatus.Loading, _stats, _saveStatus, null, _rivalStatus, _rivalStats);
        }

        public void SetLoaded(in PlayerStatsSummary stats)
        {
            SetLoaded(stats, null);
        }

        /// <summary>
        /// Loaded, but with a standing error (for example newer or corrupt cloud data: shown, never written).
        /// </summary>
        public void SetLoaded(in PlayerStatsSummary stats, string errorKey)
        {
            EnsureCurrent();
            Apply(PlayerProgressStatus.Loaded, stats, _saveStatus, string.IsNullOrEmpty(errorKey) ? null : errorKey,
                _rivalStatus, _rivalStats);
        }

        /// <summary>Service side: publishes whether the host-left win notice is pending.</summary>
        public void SetHostLeftWinNotice(bool pending)
        {
            EnsureCurrent();
            if (_hostLeftWinNotice == pending)
            {
                return;
            }

            _hostLeftWinNotice = pending;
            Changed?.Invoke();
        }

        /// <summary>
        /// Menu side: the notice was shown; asks the service (the only writer) to consume it. False when none is pending
        /// or no service is listening.
        /// </summary>
        public bool RequestConsumeHostLeftWinNotice()
        {
            if (!HasHostLeftWinNotice || HostLeftWinNoticeConsumeRequested == null)
            {
                return false;
            }

            HostLeftWinNoticeConsumeRequested.Invoke();
            return true;
        }

        /// <summary>Loading failed; keeps the previous stats and records <paramref name="errorKey"/>.</summary>
        public void SetFailed(string errorKey)
        {
            EnsureCurrent();
            Apply(PlayerProgressStatus.Failed, _stats, _saveStatus, errorKey, _rivalStatus, _rivalStats);
        }

        public void SetSaveStatus(PlayerProgressSaveStatus saveStatus)
        {
            EnsureCurrent();
            Apply(_status, _stats, saveStatus, _lastErrorKey, _rivalStatus, _rivalStats);
        }

        /// <summary>Reading a (possibly new) rival's stats started; the previous rival's values are dropped.</summary>
        public void SetRivalLoading()
        {
            EnsureCurrent();
            Apply(_status, _stats, _saveStatus, _lastErrorKey, PlayerProgressStatus.Loading, default);
        }

        public void SetRival(in PlayerStatsSummary stats)
        {
            EnsureCurrent();
            Apply(_status, _stats, _saveStatus, _lastErrorKey, PlayerProgressStatus.Loaded, stats);
        }

        /// <summary>Reading the rival's stats failed (views hide them).</summary>
        public void SetRivalFailed()
        {
            EnsureCurrent();
            Apply(_status, _stats, _saveStatus, _lastErrorKey, PlayerProgressStatus.Failed, default);
        }

        /// <summary>No rival anymore (left the room, rival gone, or rival stats disabled).</summary>
        public void ClearRival()
        {
            EnsureCurrent();
            Apply(_status, _stats, _saveStatus, _lastErrorKey, PlayerProgressStatus.Unavailable, default);
        }

        private void Apply(
            PlayerProgressStatus status,
            in PlayerStatsSummary stats,
            PlayerProgressSaveStatus saveStatus,
            string errorKey,
            PlayerProgressStatus rivalStatus,
            in PlayerStatsSummary rivalStats)
        {
            bool changed = status != _status
                || !stats.Equals(_stats)
                || saveStatus != _saveStatus
                || !string.Equals(errorKey, _lastErrorKey, StringComparison.Ordinal)
                || rivalStatus != _rivalStatus
                || !rivalStats.Equals(_rivalStats);
            if (!changed)
            {
                return;
            }

            _status = status;
            _stats = stats;
            _saveStatus = saveStatus;
            _lastErrorKey = errorKey;
            _rivalStatus = rivalStatus;
            _rivalStats = rivalStats;
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
            _rivalStatus = PlayerProgressStatus.Unavailable;
            _rivalStats = default;
            _hostLeftWinNotice = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_generation++;
        }
    }
}
