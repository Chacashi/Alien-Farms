using AlienFarms.Animals;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class AnimalLandingTests
    {
        private const float MinAirTime = 0.08f;
        private const float MaxAirTime = 4f;
        private const float MaxUpSpeed = 0.5f;
        private const float MaxOnPlayerTime = 1.5f;
        private const float KillHeight = -10f;
        private const float GroundHeight = 0f;

        private static AirborneOutcome Evaluate(
            float airTime,
            float verticalSpeed,
            bool grounded,
            float height = GroundHeight,
            bool onPlayer = false,
            float onPlayerTime = 0f)
        {
            return AnimalLanding.Evaluate(
                airTime, verticalSpeed, grounded, onPlayer, onPlayerTime, MaxOnPlayerTime,
                height, KillHeight, MinAirTime, MaxAirTime, MaxUpSpeed);
        }

        [Test]
        public void Grounded_AfterMinTime_Lands()
        {
            Assert.That(Evaluate(0.5f, -2f, true), Is.EqualTo(AirborneOutcome.Land));
        }

        [Test]
        public void Grounded_BeforeMinTime_Keeps()
        {
            Assert.That(Evaluate(0.02f, 0f, true), Is.EqualTo(AirborneOutcome.Keep));
        }

        [Test]
        public void Grounded_MovingUpFast_Keeps()
        {
            Assert.That(Evaluate(0.5f, 3f, true), Is.EqualTo(AirborneOutcome.Keep));
        }

        [Test]
        public void Grounded_SlowUpwardSpeed_Lands()
        {
            Assert.That(Evaluate(0.5f, 0.4f, true), Is.EqualTo(AirborneOutcome.Land));
        }

        [Test]
        public void NotGrounded_Keeps()
        {
            Assert.That(Evaluate(1f, -5f, false), Is.EqualTo(AirborneOutcome.Keep));
        }

        [Test]
        public void Timeout_Grounded_Lands()
        {
            Assert.That(Evaluate(MaxAirTime, 2f, true), Is.EqualTo(AirborneOutcome.Land));
        }

        [Test]
        public void Timeout_NotGrounded_Despawns()
        {
            Assert.That(Evaluate(MaxAirTime, -1f, false), Is.EqualTo(AirborneOutcome.Despawn));
        }

        [Test]
        public void OnPlayer_Settled_SlidesOff()
        {
            Assert.That(Evaluate(0.5f, 0f, false, onPlayer: true, onPlayerTime: 0.2f), Is.EqualTo(AirborneOutcome.SlideOff));
        }

        [Test]
        public void OnPlayer_PastAirTimeout_StillSlidesOff_NotDespawn()
        {
            Assert.That(Evaluate(MaxAirTime + 10f, 0f, false, onPlayer: true, onPlayerTime: 0.2f), Is.EqualTo(AirborneOutcome.SlideOff));
        }

        [Test]
        public void OnPlayer_BeyondLimit_Relocates()
        {
            Assert.That(Evaluate(0.5f, 0f, false, onPlayer: true, onPlayerTime: MaxOnPlayerTime), Is.EqualTo(AirborneOutcome.Relocate));
            Assert.That(Evaluate(MaxAirTime + 30f, 0f, false, onPlayer: true, onPlayerTime: 30f), Is.EqualTo(AirborneOutcome.Relocate));
        }

        [Test]
        public void OnPlayer_BeyondLimit_EvenWhileMovingUp_Relocates()
        {
            Assert.That(Evaluate(3f, 2f, false, onPlayer: true, onPlayerTime: 2f), Is.EqualTo(AirborneOutcome.Relocate));
        }

        [Test]
        public void OnPlayer_BeforeMinTime_Keeps()
        {
            Assert.That(Evaluate(0.02f, 0f, false, onPlayer: true, onPlayerTime: 0.02f), Is.EqualTo(AirborneOutcome.Keep));
        }

        [Test]
        public void OnPlayer_MovingUpFast_Keeps()
        {
            Assert.That(Evaluate(0.5f, 2f, false, onPlayer: true, onPlayerTime: 0.2f), Is.EqualTo(AirborneOutcome.Keep));
        }

        [Test]
        public void OnPlayerAndGrounded_GroundWins()
        {
            Assert.That(Evaluate(0.5f, 0f, true, onPlayer: true, onPlayerTime: 5f), Is.EqualTo(AirborneOutcome.Land));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void BelowKillHeight_Despawns(bool grounded)
        {
            Assert.That(Evaluate(0.5f, -2f, grounded, KillHeight - 0.1f), Is.EqualTo(AirborneOutcome.Despawn));
        }

        [Test]
        public void AtKillHeight_IsNotLost()
        {
            Assert.That(Evaluate(0.5f, -2f, false, KillHeight), Is.EqualTo(AirborneOutcome.Keep));
        }

        [Test]
        public void TrackPlayerContact_ContactStarts_StampsOnPlayerSince()
        {
            float airborneSince = 1f;
            float onPlayerSince = 0f;

            AnimalLanding.TrackPlayerContact(true, false, 3f, ref airborneSince, ref onPlayerSince);

            Assert.That(onPlayerSince, Is.EqualTo(3f));
            Assert.That(airborneSince, Is.EqualTo(1f));
        }

        [Test]
        public void TrackPlayerContact_StayingOnPlayer_KeepsTimers()
        {
            float airborneSince = 1f;
            float onPlayerSince = 3f;

            AnimalLanding.TrackPlayerContact(true, true, 4f, ref airborneSince, ref onPlayerSince);

            Assert.That(onPlayerSince, Is.EqualTo(3f));
            Assert.That(airborneSince, Is.EqualTo(1f));
        }

        [Test]
        public void TrackPlayerContact_LeavingPlayer_RestartsAirTime()
        {
            float airborneSince = 1f;
            float onPlayerSince = 3f;

            AnimalLanding.TrackPlayerContact(false, true, 40f, ref airborneSince, ref onPlayerSince);

            Assert.That(airborneSince, Is.EqualTo(40f));
            // A fresh fall: right after leaving the player it keeps flying instead of timing out.
            Assert.That(Evaluate(40.1f - airborneSince, -1f, false), Is.EqualTo(AirborneOutcome.Keep));
        }

        [Test]
        public void TrackPlayerContact_NeverOnPlayer_KeepsTimers()
        {
            float airborneSince = 1f;
            float onPlayerSince = 0f;

            AnimalLanding.TrackPlayerContact(false, false, 2f, ref airborneSince, ref onPlayerSince);

            Assert.That(airborneSince, Is.EqualTo(1f));
            Assert.That(onPlayerSince, Is.EqualTo(0f));
        }
    }
}
