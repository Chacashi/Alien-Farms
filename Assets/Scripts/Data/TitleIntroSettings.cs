using Unity.Cinemachine;
using UnityEngine;

namespace AlienFarms.Data
{
    [CreateAssetMenu(fileName = "TitleIntroSettings", menuName = "Alien Farms/UI/Title Intro Settings")]
    public sealed class TitleIntroSettings : ScriptableObject
    {
        [SerializeField, Min(0.01f), Tooltip("Fade-out for the centered logo and prompt when the first player joins (unscaled seconds).")]
        private float _attractFadeOutDuration = 0.4f;

        [SerializeField, Min(0.01f), Tooltip("Right column fade-in once the camera settles on CM_Menu (s).")]
        private float _menuFadeInDuration = 0.4f;

        [SerializeField, Min(0.01f), Tooltip("Menu column fade-out when returning to attract mode (s).")]
        private float _menuFadeOutDuration = 0.3f;

        [SerializeField, Min(0.01f), Tooltip("Attract fade-in once the camera settles on CM_Sky (s).")]
        private float _attractFadeInDuration = 0.5f;

        [SerializeField, Tooltip("Blend from CM_Sky to CM_Menu (set as the Brain's DefaultBlend before switching).")]
        private CinemachineBlendDefinition _cameraToMenuBlend = new CinemachineBlendDefinition
        {
            Style = CinemachineBlendDefinition.Styles.EaseInOut,
            Time = 1.8f
        };

        [SerializeField, Tooltip("Blend from CM_Menu to CM_Sky.")]
        private CinemachineBlendDefinition _cameraToAttractBlend = new CinemachineBlendDefinition
        {
            Style = CinemachineBlendDefinition.Styles.EaseInOut,
            Time = 1.2f
        };

        [SerializeField, Min(0.1f), Tooltip("Duration of one prompt alpha pulse (s).")]
        private float _promptPulsePeriod = 1.6f;

        [SerializeField, Range(0f, 1f), Tooltip("Minimum alpha of the prompt pulse.")]
        private float _promptMinAlpha = 0.3f;
        [SerializeField, Range(0f, 1f), Tooltip("Maximum alpha of the prompt pulse.")]
        private float _promptMaxAlpha = 1f;

        public float AttractFadeOutDuration => _attractFadeOutDuration;
        public float MenuFadeInDuration => _menuFadeInDuration;
        public float MenuFadeOutDuration => _menuFadeOutDuration;
        public float AttractFadeInDuration => _attractFadeInDuration;
        public CinemachineBlendDefinition CameraToMenuBlend => _cameraToMenuBlend;
        public CinemachineBlendDefinition CameraToAttractBlend => _cameraToAttractBlend;
        public float PromptPulsePeriod => _promptPulsePeriod;
        public float PromptMinAlpha => _promptMinAlpha;
        public float PromptMaxAlpha => _promptMaxAlpha;

        private void OnValidate()
        {
            if (_promptMinAlpha > _promptMaxAlpha)
            {
                (_promptMinAlpha, _promptMaxAlpha) = (_promptMaxAlpha, _promptMinAlpha);
            }
        }
    }
}
