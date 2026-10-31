using UnityEngine;

namespace AlienFarms.Data
{
    /// <summary>Menu navigation tuning shared by per-player menu input.</summary>
    [CreateAssetMenu(fileName = "MenuSettings", menuName = "Alien Farms/UI/Menu Settings")]
    public sealed class MenuSettings : ScriptableObject
    {
        [SerializeField, Range(0.1f, 0.9f), Tooltip("Stick/dpad deflection that counts as one navigation step.")]
        private float _navigateStepThreshold = 0.5f;

        [SerializeField, Min(0f), Tooltip("Delay between everyone confirming and loading the next scene (s, unscaled).")]
        private float _loadDelayAfterConfirm = 2f;

        public float NavigateStepThreshold => _navigateStepThreshold;
        public float LoadDelayAfterConfirm => _loadDelayAfterConfirm;
    }
}
