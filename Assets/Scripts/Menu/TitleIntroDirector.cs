using System;
using AlienFarms.Data;
using AlienFarms.UI;
using Unity.Cinemachine;
using UnityEngine;

namespace AlienFarms.Menu
{
    [DisallowMultipleComponent]
    public sealed class TitleIntroDirector : MonoBehaviour
    {
        // A hitch (e.g. spawning the menu player on join) must not swallow the fades.
        private const float MaxDeltaTime = 1f / 30f;

        [SerializeField, Tooltip("Fade, camera blend and prompt pulse settings.")]
        private TitleIntroSettings _settings;

        [SerializeField, Tooltip("Title view with the attract, prompt and menu groups.")]
        private TitleScreenView _view;

        [SerializeField, Tooltip("Cinemachine rig for the Sky and Menu shots.")]
        private MenuCameraRig _cameraRig;

        private TitleIntroModel _model;
        private MenuCameraShot _commandedShot;
        private bool _hasCommandedShot;
        private float _pulseTime;

        public event Action MenuReady;

        public TitleIntroState State => EnsureModel().State;
        public bool IsMenuInteractable => EnsureModel().IsMenuInteractable;
        public bool IsTransitioning => EnsureModel().IsTransitioning;

        public void SnapToAttract()
        {
            EnsureModel().SnapToAttract();
            CutTo(MenuCameraShot.Sky);
            _pulseTime = 0f;
            PushView();
        }

        public void SnapToMenu()
        {
            EnsureModel().SnapToMenu();
            CutTo(MenuCameraShot.Menu);
            PushView();
        }

        public void PlayToMenu()
        {
            if (EnsureModel().RequestMenu())
            {
                SyncCamera();
            }

            PushView();
        }

        public void PlayToAttract()
        {
            if (EnsureModel().RequestAttract())
            {
                SyncCamera();
            }

            PushView();
        }

        public void Skip()
        {
            bool ready = EnsureModel().Skip();
            CutTo(_model.CameraTarget);
            PushView();
            if (ready)
            {
                MenuReady?.Invoke();
            }
        }

        private void Update()
        {
            EnsureModel();
            float dt = Mathf.Min(Time.unscaledDeltaTime, MaxDeltaTime);
            bool ready = _model.Tick(
                dt,
                _cameraRig == null || _cameraRig.IsSettledOn(MenuCameraShot.Menu),
                _cameraRig == null || _cameraRig.IsSettledOn(MenuCameraShot.Sky));

            SyncCamera();
            _pulseTime = _model.AttractAlpha > 0f ? _pulseTime + dt : 0f;
            PushView();
            if (ready)
            {
                MenuReady?.Invoke();
            }
        }

        private TitleIntroModel EnsureModel()
        {
            if (_model == null)
            {
                _model = _settings != null
                    ? new TitleIntroModel(_settings.AttractFadeOutDuration, _settings.MenuFadeInDuration,
                        _settings.MenuFadeOutDuration, _settings.AttractFadeInDuration)
                    : new TitleIntroModel(0.4f, 0.4f, 0.3f, 0.5f);
            }

            return _model;
        }

        private void SyncCamera()
        {
            MenuCameraShot target = _model.CameraTarget;
            if (_cameraRig == null || (_hasCommandedShot && _commandedShot == target))
            {
                return;
            }

            bool toMenu = target == MenuCameraShot.Menu;
            CinemachineBlendDefinition blend = _settings != null
                ? (toMenu ? _settings.CameraToMenuBlend : _settings.CameraToAttractBlend)
                : new CinemachineBlendDefinition
                {
                    Style = CinemachineBlendDefinition.Styles.EaseInOut,
                    Time = toMenu ? 1.8f : 1.2f
                };
            _cameraRig.Show(target, blend);
            _commandedShot = target;
            _hasCommandedShot = true;
        }

        private void CutTo(MenuCameraShot shot)
        {
            if (_cameraRig != null)
            {
                _cameraRig.Cut(shot);
            }

            _commandedShot = shot;
            _hasCommandedShot = true;
        }

        private void PushView()
        {
            if (_view == null)
            {
                return;
            }

            float period = _settings != null ? _settings.PromptPulsePeriod : 1.6f;
            float minAlpha = _settings != null ? _settings.PromptMinAlpha : 0.3f;
            float maxAlpha = _settings != null ? _settings.PromptMaxAlpha : 1f;
            _view.SetAttractAlpha(_model.AttractAlpha);
            _view.SetPromptAlpha(TitlePromptPulse.Evaluate(_pulseTime, period, minAlpha, maxAlpha));
            _view.SetMenuAlpha(_model.MenuAlpha);
            _view.SetMenuInteractable(_model.IsMenuInteractable);
        }

        private void OnValidate()
        {
            if (_settings == null || _view == null || _cameraRig == null)
            {
                Debug.LogWarning($"[{nameof(TitleIntroDirector)}] Assign settings, title view and camera rig on '{name}'.", this);
            }
        }
    }
}
