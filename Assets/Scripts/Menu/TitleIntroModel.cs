using UnityEngine;

namespace AlienFarms.Menu
{
    public sealed class TitleIntroModel
    {
        private const float MinimumDuration = 0.0001f;

        private readonly float _attractFadeOut;
        private readonly float _menuFadeIn;
        private readonly float _menuFadeOut;
        private readonly float _attractFadeIn;

        public TitleIntroModel(float attractFadeOut, float menuFadeIn, float menuFadeOut, float attractFadeIn)
        {
            _attractFadeOut = Mathf.Max(attractFadeOut, MinimumDuration);
            _menuFadeIn = Mathf.Max(menuFadeIn, MinimumDuration);
            _menuFadeOut = Mathf.Max(menuFadeOut, MinimumDuration);
            _attractFadeIn = Mathf.Max(attractFadeIn, MinimumDuration);
            SnapToAttract();
        }

        public TitleIntroState State { get; private set; }
        public float AttractAlpha { get; private set; }
        public float MenuAlpha { get; private set; }
        public bool IsMenuInteractable => State == TitleIntroState.Menu;
        public bool IsTransitioning => State == TitleIntroState.ToMenu || State == TitleIntroState.ToAttract;
        public MenuCameraShot CameraTarget => State == TitleIntroState.ToMenu || State == TitleIntroState.Menu
            ? MenuCameraShot.Menu
            : MenuCameraShot.Sky;

        public void SnapToAttract()
        {
            State = TitleIntroState.Attract;
            AttractAlpha = 1f;
            MenuAlpha = 0f;
        }

        public void SnapToMenu()
        {
            State = TitleIntroState.Menu;
            AttractAlpha = 0f;
            MenuAlpha = 1f;
        }

        public bool RequestMenu()
        {
            if (CameraTarget == MenuCameraShot.Menu)
            {
                return false;
            }

            State = TitleIntroState.ToMenu;
            return true;
        }

        public bool RequestAttract()
        {
            if (CameraTarget == MenuCameraShot.Sky)
            {
                return false;
            }

            State = TitleIntroState.ToAttract;
            return true;
        }

        public bool Skip()
        {
            if (State == TitleIntroState.ToMenu)
            {
                SnapToMenu();
                return true;
            }

            if (State == TitleIntroState.ToAttract)
            {
                SnapToAttract();
            }

            return false;
        }

        public bool Tick(float dt, bool cameraSettledOnMenu, bool cameraSettledOnSky)
        {
            if (State == TitleIntroState.ToMenu)
            {
                AttractAlpha = Mathf.MoveTowards(AttractAlpha, 0f, dt / _attractFadeOut);
                if (AttractAlpha == 0f && cameraSettledOnMenu)
                {
                    MenuAlpha = Mathf.MoveTowards(MenuAlpha, 1f, dt / _menuFadeIn);
                    if (MenuAlpha >= 1f)
                    {
                        State = TitleIntroState.Menu;
                        return true;
                    }
                }
            }
            else if (State == TitleIntroState.ToAttract)
            {
                MenuAlpha = Mathf.MoveTowards(MenuAlpha, 0f, dt / _menuFadeOut);
                if (MenuAlpha == 0f && cameraSettledOnSky)
                {
                    AttractAlpha = Mathf.MoveTowards(AttractAlpha, 1f, dt / _attractFadeIn);
                    if (AttractAlpha >= 1f)
                    {
                        State = TitleIntroState.Attract;
                    }
                }
            }

            return false;
        }
    }
}
