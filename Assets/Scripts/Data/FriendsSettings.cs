using UnityEngine;

namespace AlienFarms.Data
{
    [CreateAssetMenu(fileName = "FriendsSettings", menuName = "Alien Farms/Networking/Friends Settings")]
    public sealed class FriendsSettings : ScriptableObject
    {
        public const int DefaultMaxEntries = 16;

        [Min(1)] [SerializeField] private int _inviteTtlSeconds = 120;
        [Min(0)] [SerializeField] private float _inviteCooldownSeconds = 5f;
        [Min(1)] [SerializeField] private int _maxEntries = DefaultMaxEntries;
        [Tooltip("Refresh interval while notifications are disconnected.")]
        [Min(1)] [SerializeField] private float _fallbackRefreshSeconds = 30f;
        [Min(0)] [SerializeField] private float _presenceDebounceSeconds = 1f;

        public int InviteTtlSeconds => _inviteTtlSeconds;
        public float InviteCooldownSeconds => _inviteCooldownSeconds;
        public int MaxEntries => _maxEntries;
        public float FallbackRefreshSeconds => _fallbackRefreshSeconds;
        public float PresenceDebounceSeconds => _presenceDebounceSeconds;

        private void OnValidate()
        {
            _inviteTtlSeconds = Mathf.Max(1, _inviteTtlSeconds);
            _inviteCooldownSeconds = Mathf.Max(0f, _inviteCooldownSeconds);
            _maxEntries = Mathf.Max(1, _maxEntries);
            _fallbackRefreshSeconds = Mathf.Max(1f, _fallbackRefreshSeconds);
            _presenceDebounceSeconds = Mathf.Max(0f, _presenceDebounceSeconds);
        }
    }
}
