using AlienFarms.Utilities;
using NUnit.Framework;
using UnityEngine;

namespace AlienFarms.Tests
{
    public sealed class BallisticMathTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void ElevationFromPitch_AtReference_ReturnsBase()
        {
            Assert.That(BallisticMath.ElevationFromPitch(20f, 20f, 30f, 0.8f, 10f, 55f), Is.EqualTo(30f).Within(Tolerance));
        }

        [Test]
        public void ElevationFromPitch_LookingUp_RaisesElevation()
        {
            // 10 degrees above the reference pitch * 0.8 influence = +8.
            Assert.That(BallisticMath.ElevationFromPitch(10f, 20f, 30f, 0.8f, 10f, 55f), Is.EqualTo(38f).Within(Tolerance));
        }

        [Test]
        public void ElevationFromPitch_LookingDown_LowersElevation()
        {
            Assert.That(BallisticMath.ElevationFromPitch(30f, 20f, 30f, 0.8f, 10f, 55f), Is.EqualTo(22f).Within(Tolerance));
        }

        [TestCase(-89f, 55f)]
        [TestCase(89f, 10f)]
        public void ElevationFromPitch_IsClamped(float pitch, float expected)
        {
            Assert.That(BallisticMath.ElevationFromPitch(pitch, 20f, 30f, 0.8f, 10f, 55f), Is.EqualTo(expected).Within(Tolerance));
        }

        [Test]
        public void LaunchVelocity_HasSpeedMagnitude_AndElevation()
        {
            Vector3 velocity = BallisticMath.LaunchVelocity(new Vector3(0f, 0.5f, 2f), 30f, 10f);

            Assert.That(velocity.magnitude, Is.EqualTo(10f).Within(Tolerance));
            Assert.That(velocity.x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(velocity.y, Is.EqualTo(5f).Within(Tolerance));
            Assert.That(velocity.z, Is.EqualTo(10f * Mathf.Cos(30f * Mathf.Deg2Rad)).Within(Tolerance));
        }

        [Test]
        public void LaunchVelocity_DegenerateForward_FallsBackToWorldForward()
        {
            Vector3 velocity = BallisticMath.LaunchVelocity(Vector3.up, 0f, 4f);

            Assert.That(Vector3.Distance(velocity, new Vector3(0f, 0f, 4f)), Is.LessThan(Tolerance));
        }

        [Test]
        public void Step_IsSemiImplicitEuler()
        {
            Vector3 position = Vector3.zero;
            Vector3 velocity = new Vector3(1f, 0f, 0f);

            BallisticMath.Step(ref position, ref velocity, new Vector3(0f, -10f, 0f), 0.1f);

            // Velocity is updated first, then position uses the new velocity.
            Assert.That(Vector3.Distance(velocity, new Vector3(1f, -1f, 0f)), Is.LessThan(Tolerance));
            Assert.That(Vector3.Distance(position, new Vector3(0.1f, -0.1f, 0f)), Is.LessThan(Tolerance));
        }

        [Test]
        public void PreviewPointCount_CoversMaxTime_PlusOrigin()
        {
            // 2.5 s / (0.02 s * 2 steps) = 62.5 -> 63 intervals + origin.
            Assert.That(BallisticMath.PreviewPointCount(2.5f, 0.02f, 2), Is.EqualTo(64));
        }

        [Test]
        public void PreviewPointCount_StepsBelowOne_TreatedAsOne()
        {
            Assert.That(BallisticMath.PreviewPointCount(1f, 0.1f, 0), Is.EqualTo(BallisticMath.PreviewPointCount(1f, 0.1f, 1)));
        }

        [TestCase(0f, 0.02f)]
        [TestCase(1f, 0f)]
        public void PreviewPointCount_Degenerate_IsAtLeastTwo(float maxTime, float dt)
        {
            Assert.That(BallisticMath.PreviewPointCount(maxTime, dt, 1), Is.GreaterThanOrEqualTo(2));
        }

        [TestCase(1f, 1f)]
        [TestCase(0.25f, 0.5f)]
        [TestCase(0.55f, 0.7416198f)]
        public void SpeedScaleForDistance_IsSquareRoot(float multiplier, float expected)
        {
            Assert.That(BallisticMath.SpeedScaleForDistance(multiplier), Is.EqualTo(expected).Within(1e-4f));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        public void SpeedScaleForDistance_NonPositive_IsZero(float multiplier)
        {
            Assert.That(BallisticMath.SpeedScaleForDistance(multiplier), Is.EqualTo(0f));
        }

        [TestCase(0.55f)]
        [TestCase(0.3f)]
        public void SpeedScaleForDistance_ScalesFlatGroundRange(float multiplier)
        {
            float fullRange = SimulateFlatRange(13f, 30f);
            float scaledRange = SimulateFlatRange(13f * BallisticMath.SpeedScaleForDistance(multiplier), 30f);

            Assert.That(scaledRange / fullRange, Is.EqualTo(multiplier).Within(0.02f));
        }

        private static float SimulateFlatRange(float speed, float elevationDeg)
        {
            Vector3 position = Vector3.zero;
            Vector3 velocity = BallisticMath.LaunchVelocity(Vector3.forward, elevationDeg, speed);
            Vector3 gravity = new Vector3(0f, -9.81f, 0f);
            const float dt = 0.001f;
            for (int i = 0; i < 100000; i++)
            {
                BallisticMath.Step(ref position, ref velocity, gravity, dt);
                if (position.y <= 0f)
                {
                    break;
                }
            }

            return position.z;
        }
    }
}
