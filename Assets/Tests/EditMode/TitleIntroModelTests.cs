using AlienFarms.Menu;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class TitleIntroModelTests
    {
        private TitleIntroModel _model;

        [SetUp]
        public void SetUp()
        {
            _model = new TitleIntroModel(0.4f, 0.4f, 0.3f, 0.5f);
            _model.SnapToAttract();
        }

        [Test]
        public void AttractTargetsSkyAndIsNotInteractable()
        {
            Assert.That(_model.State, Is.EqualTo(TitleIntroState.Attract));
            Assert.That(_model.AttractAlpha, Is.EqualTo(1f));
            Assert.That(_model.MenuAlpha, Is.Zero);
            Assert.That(_model.CameraTarget, Is.EqualTo(MenuCameraShot.Sky));
            Assert.That(_model.IsMenuInteractable, Is.False);
        }

        [Test]
        public void MenuWaitsForCameraAndReportsReadyOnce()
        {
            Assert.That(_model.RequestMenu(), Is.True);
            Assert.That(_model.State, Is.EqualTo(TitleIntroState.ToMenu));
            Assert.That(_model.CameraTarget, Is.EqualTo(MenuCameraShot.Menu));

            for (int i = 0; i < 10; i++)
            {
                Assert.That(_model.Tick(0.1f, false, false), Is.False);
            }

            Assert.That(_model.AttractAlpha, Is.Zero);
            Assert.That(_model.MenuAlpha, Is.Zero);
            Assert.That(_model.IsMenuInteractable, Is.False);

            for (int i = 0; i < 3; i++)
            {
                Assert.That(_model.Tick(0.1f, true, false), Is.False);
            }

            Assert.That(_model.Tick(0.1f, true, false), Is.True);
            Assert.That(_model.State, Is.EqualTo(TitleIntroState.Menu));
            Assert.That(_model.IsMenuInteractable, Is.True);
            Assert.That(_model.Tick(0.1f, true, false), Is.False);
        }

        [Test]
        public void MenuWaitsForAttractFadeEvenWhenCameraIsSettled()
        {
            _model.RequestMenu();

            Assert.That(_model.Tick(0.2f, true, false), Is.False);
            Assert.That(_model.AttractAlpha, Is.EqualTo(0.5f));
            Assert.That(_model.MenuAlpha, Is.Zero);
        }

        [Test]
        public void ReversingPreservesAlphaAndWaitsForSky()
        {
            _model.RequestMenu();
            _model.Tick(0.2f, false, false);

            Assert.That(_model.RequestAttract(), Is.True);
            Assert.That(_model.State, Is.EqualTo(TitleIntroState.ToAttract));
            Assert.That(_model.CameraTarget, Is.EqualTo(MenuCameraShot.Sky));
            Assert.That(_model.AttractAlpha, Is.EqualTo(0.5f));

            Assert.That(_model.Tick(0.1f, false, false), Is.False);
            Assert.That(_model.AttractAlpha, Is.EqualTo(0.5f));
            Assert.That(_model.Tick(0.25f, false, true), Is.False);
            AttractTargetsSkyAndIsNotInteractable();
        }

        [Test]
        public void AttractWaitsForMenuFadeEvenWhenCameraIsSettled()
        {
            _model.SnapToMenu();
            _model.RequestAttract();

            Assert.That(_model.Tick(0.15f, false, true), Is.False);
            Assert.That(_model.MenuAlpha, Is.EqualTo(0.5f));
            Assert.That(_model.AttractAlpha, Is.Zero);
            Assert.That(_model.IsMenuInteractable, Is.False);
        }

        [Test]
        public void ReversingToMenuPreservesPartialMenuAlpha()
        {
            _model.SnapToMenu();
            Assert.That(_model.RequestAttract(), Is.True);
            _model.Tick(0.15f, cameraSettledOnMenu: false, cameraSettledOnSky: false);

            Assert.That(_model.State, Is.EqualTo(TitleIntroState.ToAttract));
            float partialMenuAlpha = _model.MenuAlpha;
            Assert.That(partialMenuAlpha, Is.EqualTo(0.5f).Within(0.0001f));

            Assert.That(_model.RequestMenu(), Is.True);
            Assert.That(_model.State, Is.EqualTo(TitleIntroState.ToMenu));
            Assert.That(_model.MenuAlpha, Is.EqualTo(partialMenuAlpha));
        }

        [Test]
        public void LargeTickCompletesBothMenuFadesWhenCameraIsSettled()
        {
            Assert.That(_model.RequestMenu(), Is.True);

            Assert.That(_model.Tick(10f, cameraSettledOnMenu: true, cameraSettledOnSky: false), Is.True);
            AssertMenuState();
        }

        [Test]
        public void SkipToMenuReportsReady()
        {
            _model.RequestMenu();

            Assert.That(_model.Skip(), Is.True);
            AssertMenuState();
        }

        [Test]
        public void SkipToAttractDoesNotReportMenuReady()
        {
            _model.SnapToMenu();
            _model.RequestAttract();

            Assert.That(_model.Skip(), Is.False);
            AttractTargetsSkyAndIsNotInteractable();
        }

        [Test]
        public void RequestsAndSkipLeaveTheMatchingStableStateUnchanged()
        {
            Assert.That(_model.RequestAttract(), Is.False);
            Assert.That(_model.Skip(), Is.False);
            AttractTargetsSkyAndIsNotInteractable();

            _model.SnapToMenu();
            Assert.That(_model.RequestMenu(), Is.False);
            Assert.That(_model.Skip(), Is.False);
            AssertMenuState();
        }

        [Test]
        public void SnapToMenuIsImmediatelyReadyAndTickDoesNotReportArrival()
        {
            _model.SnapToMenu();

            AssertMenuState();
            Assert.That(_model.Tick(0.1f, true, false), Is.False);
        }

        private void AssertMenuState()
        {
            Assert.That(_model.State, Is.EqualTo(TitleIntroState.Menu));
            Assert.That(_model.AttractAlpha, Is.Zero);
            Assert.That(_model.MenuAlpha, Is.EqualTo(1f));
            Assert.That(_model.IsMenuInteractable, Is.True);
        }
    }
}
