using UnityEngine;

namespace AlienFarms.Data
{
    /// <summary>Hold-to-pause tuning.</summary>
    [CreateAssetMenu(fileName = "PauseSettings", menuName = "Alien Farms/UI/Pause Settings")]
    public sealed class PauseSettings : ScriptableObject
    {
        [SerializeField, Min(0.1f), Tooltip("How long Pause must be held to open the pause menu (s, unscaled).")]
        private float _holdDuration = 3f;

        [SerializeField, Min(0f), Tooltip("The hold progress indicator appears only after holding this long (s), so taps do not flash it.")]
        private float _showProgressAfter = 0.2f;

        public float HoldDuration => _holdDuration;
        public float ShowProgressAfter => _showProgressAfter;

        private void OnValidate()
        {
            if (_showProgressAfter >= _holdDuration)
            {
                Debug.LogWarning($"[{nameof(PauseSettings)}] Show Progress After should be shorter than Hold Duration on '{name}'.", this);
            }
        }
    }
}
