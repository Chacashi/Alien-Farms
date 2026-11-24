using UnityEngine;

namespace AlienFarms.Data
{
    [CreateAssetMenu(menuName = "Alien Farms/Networking/Interest Settings", fileName = "InterestSettings")]
    public sealed class InterestSettings : ScriptableObject
    {
        [SerializeField, Tooltip("Enable server-side interest filtering.")] private bool _enabled = true;
        [SerializeField, Min(1), Tooltip("Distance below which a hidden object becomes visible.")] private float _enterRadius = 30f;
        [SerializeField, Min(1), Tooltip("Distance beyond which a visible object becomes hidden.")] private float _exitRadius = 36f;
        [SerializeField, Range(1, 50), Tooltip("Network ticks between visibility reviews.")] private int _reviewIntervalTicks = 5;
        [SerializeField, Min(0), Tooltip("Maximum shows per client per periodic review. Zero is unlimited.")] private int _maxShowsPerReview;
        [SerializeField, Tooltip("Apply distance filtering to animals.")] private bool _filterAnimals = true;
        [SerializeField, Tooltip("Apply distance filtering to bombs.")] private bool _filterBombs = true;
        [SerializeField, Tooltip("Log visibility changes and observer position sources.")] private bool _logChanges;

        public bool Enabled => _enabled;
        public float EnterRadius => _enterRadius;
        public float ExitRadius => _exitRadius;
        public int ReviewIntervalTicks => _reviewIntervalTicks;
        public int MaxShowsPerReview => _maxShowsPerReview;
        public bool FilterAnimals => _filterAnimals;
        public bool FilterBombs => _filterBombs;
        public bool LogChanges => _logChanges;

        private void OnValidate()
        {
            _enterRadius = Mathf.Max(1f, _enterRadius);
            if (_exitRadius <= _enterRadius)
            {
                _exitRadius = _enterRadius + Mathf.Max(0.01f, _enterRadius * 0.000001f);
            }

            _reviewIntervalTicks = Mathf.Clamp(_reviewIntervalTicks, 1, 50);
            _maxShowsPerReview = Mathf.Max(0, _maxShowsPerReview);
        }
    }
}
