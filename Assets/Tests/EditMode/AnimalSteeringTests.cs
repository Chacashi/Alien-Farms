using AlienFarms.Animals;
using NUnit.Framework;
using UnityEngine;

namespace AlienFarms.Tests
{
    public sealed class AnimalSteeringTests
    {
        private const float Tolerance = 0.0001f;
        private const float Margin = 1f;
        private const float Lookahead = 1f;

        // 20 x 20 zone centered at the origin; inner rectangle (margin 1) spans [-9, 9] on X and Z.
        private static readonly Bounds s_bounds = new Bounds(Vector3.zero, new Vector3(20f, 2f, 20f));

        [Test]
        public void ConfineDirection_OutwardNearEdge_IsAttenuated()
        {
            // 0.5 m from the +X inner edge, moving straight out with a 1 m lookahead.
            Vector3 position = new Vector3(8.5f, 0f, 0f);
            Vector3 result = AnimalSteering.ConfineDirection(position, Vector3.right, s_bounds, Margin, Lookahead);

            Assert.That(result.x, Is.GreaterThan(0f));
            Assert.That(result.x, Is.LessThan(1f));
            Assert.That(position.x + result.x * Lookahead, Is.LessThanOrEqualTo(9f + Tolerance));
        }

        [Test]
        public void ConfineDirection_OutwardAtEdge_IsZeroed()
        {
            Vector3 position = new Vector3(9f - 0.00001f, 0f, 0f);
            Vector3 result = AnimalSteering.ConfineDirection(position, Vector3.right, s_bounds, Margin, Lookahead);

            Assert.That(result.x, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void ConfineDirection_OutwardFarFromEdge_IsUnchanged()
        {
            Vector3 position = new Vector3(2f, 0f, 0f);
            Vector3 direction = new Vector3(0.6f, 0f, 0.8f);
            Vector3 result = AnimalSteering.ConfineDirection(position, direction, s_bounds, Margin, Lookahead);

            AssertVector(direction, result);
        }

        [Test]
        public void ConfineDirection_InwardNearEdge_IsUnchanged()
        {
            Vector3 position = new Vector3(8.9f, 0f, -8.9f);
            Vector3 direction = new Vector3(-0.6f, 0f, 0.8f);
            Vector3 result = AnimalSteering.ConfineDirection(position, direction, s_bounds, Margin, Lookahead);

            AssertVector(direction, result);
        }

        [Test]
        public void ConfineDirection_Outside_PushesInwardWithFixedMagnitude()
        {
            Vector3 outsideMax = new Vector3(9.5f, 0f, 0f);
            Vector3 outsideMin = new Vector3(0f, 0f, -9.5f);

            Vector3 resultX = AnimalSteering.ConfineDirection(outsideMax, Vector3.right, s_bounds, Margin, Lookahead);
            Vector3 resultZ = AnimalSteering.ConfineDirection(outsideMin, Vector3.zero, s_bounds, Margin, Lookahead);

            Assert.That(resultX.x, Is.EqualTo(-AnimalSteering.InwardPush).Within(Tolerance));
            Assert.That(resultZ.z, Is.EqualTo(AnimalSteering.InwardPush).Within(Tolerance));
        }

        [Test]
        public void ConfineDirection_ResultIsPlanar()
        {
            Vector3 result = AnimalSteering.ConfineDirection(Vector3.zero, new Vector3(0.3f, 0.9f, 0.3f), s_bounds, Margin, Lookahead);

            Assert.That(result.y, Is.EqualTo(0f));
        }

        [Test]
        public void EscapeAlongEdge_Cornered_ReturnsFreeEdgeDirection()
        {
            // Just inside the +X/+Z corner with the threat pushing further into the corner.
            Vector3 position = new Vector3(8.99f, 0f, 8.99f);
            Vector3 away = new Vector3(0.6f, 0f, 0.8f);

            Vector3 confined = AnimalSteering.ConfineDirection(position, away, s_bounds, Margin, Lookahead);
            Vector3 result = AnimalSteering.EscapeAlongEdge(position, away, s_bounds, Margin, Lookahead);

            Assert.That(confined.sqrMagnitude, Is.LessThan(0.01f));
            // Both free edges move towards the threat; the one that does so least (-X, dot -0.6 vs -0.8) wins.
            AssertVector(Vector3.left, result);
        }

        [TestCase(0f, 5.5f, 12f)]
        [TestCase(0.25f, 5.5f, 12f)]
        [TestCase(0f, 3f, 8f)]
        public void SteerFlee_CorneredWithDiagonalThreat_LeavesCornerAndStaysInside(float threatJitter, float speed, float acceleration)
        {
            // Inner corner (-9, -9); threat on the diagonal pushing the animal into the corner.
            Vector3 corner = new Vector3(-9f, 0f, -9f);
            Vector3 threat = new Vector3(-6f + threatJitter, 0f, -6f - threatJitter);
            Vector3 position = new Vector3(-8.95f, 0f, -8.95f);
            float lookahead = speed * speed / (2f * acceleration);
            const float dt = 0.02f;
            const float holdTime = 0.75f;

            CornerEscape escape = default;
            float initialDistance = Planar(position - corner).magnitude;
            float distanceAfterOneSecond = 0f;
            float previousDistance = initialDistance;
            int shrinkingSteps = 0;

            for (int step = 1; step <= 150; step++)
            {
                float now = step * dt;
                Vector3 away = Planar(position - threat).normalized;
                Vector3 direction = AnimalSteering.SteerFlee(position, away, away, s_bounds, Margin, lookahead, now, holdTime, ref escape);
                position += direction * (speed * dt);

                Assert.That(s_bounds.min.x <= position.x && position.x <= s_bounds.max.x
                    && s_bounds.min.z <= position.z && position.z <= s_bounds.max.z, Is.True,
                    $"Left the zone at step {step}: {position}");

                float distance = Planar(position - corner).magnitude;
                if (distance < previousDistance - Tolerance)
                {
                    shrinkingSteps++;
                }

                previousDistance = distance;
                if (step == 50)
                {
                    distanceAfterOneSecond = distance;
                }
            }

            // Must leave the corner quickly and never be pulled back towards it.
            Assert.That(distanceAfterOneSecond, Is.GreaterThan(initialDistance + 0.5f * speed));
            Assert.That(previousDistance, Is.GreaterThan(distanceAfterOneSecond));
            Assert.That(shrinkingSteps, Is.EqualTo(0), "Distance to the corner decreased (oscillation).");
        }

        [Test]
        public void SteerFlee_FreeDirection_IsNotTreatedAsCornered()
        {
            CornerEscape escape = default;
            Vector3 direction = new Vector3(0.6f, 0f, 0.8f);
            Vector3 result = AnimalSteering.SteerFlee(Vector3.zero, direction, direction, s_bounds, Margin, Lookahead, 0f, 0.75f, ref escape);

            AssertVector(direction, result);
            Assert.That(escape.IsActive(0f), Is.False);
        }

        [TestCase(1)]
        [TestCase(42)]
        [TestCase(1234)]
        [TestCase(98765)]
        public void RandomPointInside_AlwaysInsideInnerRectangle(int seed)
        {
            Random.InitState(seed);
            Vector3 around = new Vector3(8f, 0f, -8f);

            for (int i = 0; i < 200; i++)
            {
                Vector3 point = AnimalSteering.RandomPointInside(around, 6f, s_bounds, Margin);

                Assert.That(point.x, Is.InRange(-9f - Tolerance, 9f + Tolerance));
                Assert.That(point.z, Is.InRange(-9f - Tolerance, 9f + Tolerance));
                Assert.That(point.y, Is.EqualTo(around.y));
            }
        }

        private static Vector3 Planar(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private static void AssertVector(Vector3 expected, Vector3 actual)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(Tolerance));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(Tolerance));
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(Tolerance));
        }
    }
}
