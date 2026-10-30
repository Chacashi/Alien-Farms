using AlienFarms.Animals;
using NUnit.Framework;
using UnityEngine;

namespace AlienFarms.Tests
{
    public sealed class ObstacleAvoidanceTests
    {
        private const float Tolerance = 0.0001f;
        private const float ProbeDistance = 2f;
        private const float HoldTime = 0.6f;
        private const int ObstacleA = 101;
        private const int ObstacleB = 202;

        // Obstacle ahead along +Z: its surface normal faces the animal (-Z).
        private static readonly Vector3 s_normal = Vector3.back;

        [Test]
        public void MovingAway_IsUnchanged()
        {
            var memory = new AvoidanceSide();
            Vector3 desired = new Vector3(0.3f, 0f, -0.9f);

            Vector3 result = ObstacleAvoidance.Avoid(desired, s_normal, 1f, ProbeDistance, ObstacleA, Vector3.zero, 0f, HoldTime, ref memory);

            Assert.That((result - desired).magnitude, Is.LessThan(Tolerance));
            Assert.That(memory.Side, Is.EqualTo(0));
        }

        [Test]
        public void Oblique_SlidesWithoutGoingIntoTheObstacle()
        {
            var memory = new AvoidanceSide();
            Vector3 desired = new Vector3(0.6f, 0f, 0.8f);

            Vector3 result = ObstacleAvoidance.Avoid(desired, s_normal, 1f, ProbeDistance, ObstacleA, Vector3.zero, 0f, HoldTime, ref memory);

            Assert.That(Vector3.Dot(result, s_normal), Is.GreaterThanOrEqualTo(-Tolerance));
            Assert.That(result.magnitude, Is.LessThanOrEqualTo(desired.magnitude + Tolerance));
            // Keeps sliding the way it was already going.
            Assert.That(result.x, Is.GreaterThan(0f));
        }

        [Test]
        public void HeadOn_TurnsPerpendicularTowardsHint()
        {
            Vector3 desired = Vector3.forward;

            var right = new AvoidanceSide();
            Vector3 towardsRight = ObstacleAvoidance.Avoid(desired, s_normal, 1f, ProbeDistance, ObstacleA, Vector3.right, 0f, HoldTime, ref right);
            var left = new AvoidanceSide();
            Vector3 towardsLeft = ObstacleAvoidance.Avoid(desired, s_normal, 1f, ProbeDistance, ObstacleA, Vector3.left, 0f, HoldTime, ref left);

            Assert.That(towardsRight.x, Is.GreaterThan(0.5f));
            Assert.That(towardsLeft.x, Is.LessThan(-0.5f));
            Assert.That(Vector3.Dot(towardsRight, s_normal), Is.GreaterThanOrEqualTo(-Tolerance));
            Assert.That(right.IsActive(0f), Is.True);
            Assert.That(right.Until, Is.EqualTo(HoldTime).Within(Tolerance));
        }

        [Test]
        public void ActiveMemory_HoldsSide_ForSameObstacle()
        {
            var memory = new AvoidanceSide();
            Vector3 first = ObstacleAvoidance.Avoid(Vector3.forward, s_normal, 1f, ProbeDistance, ObstacleA, Vector3.right, 0f, HoldTime, ref memory);

            // Slightly oblique the other way (tangent below the hold ratio), no hint: the side is kept.
            Vector3 desired = new Vector3(-0.3f, 0f, 0.95f).normalized;
            Vector3 second = ObstacleAvoidance.Avoid(desired, s_normal, 1f, ProbeDistance, ObstacleA, Vector3.zero, HoldTime * 0.5f, HoldTime, ref memory);

            Assert.That(Mathf.Sign(second.x), Is.EqualTo(Mathf.Sign(first.x)));
        }

        [Test]
        public void ActiveMemory_DoesNotRenewOnEveryHit()
        {
            var memory = new AvoidanceSide();
            ObstacleAvoidance.Avoid(Vector3.forward, s_normal, 1f, ProbeDistance, ObstacleA, Vector3.right, 0f, HoldTime, ref memory);
            ObstacleAvoidance.Avoid(Vector3.forward, s_normal, 1f, ProbeDistance, ObstacleA, Vector3.zero, HoldTime * 0.5f, HoldTime, ref memory);

            Assert.That(memory.Until, Is.EqualTo(HoldTime).Within(Tolerance));
        }

        [Test]
        public void ActiveMemory_ObliqueHitOnOtherObstacle_DoesNotFlip()
        {
            var memory = new AvoidanceSide();
            // Head-on with A, hint picks the +X side.
            ObstacleAvoidance.Avoid(Vector3.forward, s_normal, 1f, ProbeDistance, ObstacleA, Vector3.right, 0f, HoldTime, ref memory);
            Assert.That(memory.IsActive(0.1f), Is.True);

            // Now an oblique hit on a different obstacle (a wall facing -X) while moving towards -X / +Z.
            Vector3 wallNormal = new Vector3(1f, 0f, -0.2f).normalized;
            Vector3 desired = new Vector3(-0.6f, 0f, 0.8f);
            Vector3 natural = desired - Vector3.Dot(desired, wallNormal) * wallNormal;
            Vector3 result = ObstacleAvoidance.Avoid(desired, wallNormal, 1f, ProbeDistance, ObstacleB, Vector3.zero, 0.1f, HoldTime, ref memory);

            Assert.That(Vector3.Dot(result, natural), Is.GreaterThan(0f));
            Assert.That(Vector3.Dot(result, wallNormal), Is.GreaterThanOrEqualTo(-Tolerance));
        }

        [Test]
        public void ActiveMemory_ClearlyObliqueHit_UsesNaturalTangent()
        {
            var memory = new AvoidanceSide();
            memory.Begin(-1, 10f, ObstacleA);

            // Tangent 0.8 of desired (above the hold ratio) towards -X; memory side -1 would mean +X.
            Vector3 desired = new Vector3(-0.8f, 0f, 0.6f);
            Vector3 result = ObstacleAvoidance.Avoid(desired, s_normal, 1f, ProbeDistance, ObstacleA, Vector3.zero, 0f, HoldTime, ref memory);

            Assert.That(result.x, Is.LessThan(0f));
        }

        [Test]
        public void FleeHint_IsNeverContradicted()
        {
            var memory = new AvoidanceSide();
            // Memory says go -X (side +1 with perpendicular -X), but the threat is on -X (hint +X).
            memory.Begin(1, 10f, ObstacleA);

            Vector3 result = ObstacleAvoidance.Avoid(Vector3.forward, s_normal, 1f, ProbeDistance, ObstacleA, Vector3.right, 0f, HoldTime, ref memory);

            Assert.That(Vector3.Dot(result, Vector3.right), Is.GreaterThan(0f));
        }

        [Test]
        public void Detour_GoesAroundTheRequestedSide()
        {
            Vector3 plus = ObstacleAvoidance.Detour(Vector3.forward, s_normal, 1f, ProbeDistance, 1);
            Vector3 minus = ObstacleAvoidance.Detour(Vector3.forward, s_normal, 1f, ProbeDistance, -1);

            Assert.That(ObstacleAvoidance.SideOf(plus, s_normal), Is.EqualTo(1));
            Assert.That(ObstacleAvoidance.SideOf(minus, s_normal), Is.EqualTo(-1));
            Assert.That(Vector3.Dot(plus, s_normal), Is.GreaterThanOrEqualTo(-Tolerance));
            Assert.That(plus.magnitude, Is.LessThanOrEqualTo(1f + Tolerance));
        }

        [Test]
        public void NoHint_UsesStoredSide()
        {
            var memory = new AvoidanceSide();
            // Expired memory that still remembers side -1.
            memory.Begin(-1, 0f, ObstacleA);

            Vector3 result = ObstacleAvoidance.Avoid(Vector3.forward, s_normal, 1f, ProbeDistance, ObstacleA, Vector3.zero, 5f, HoldTime, ref memory);

            Vector3 perpendicular = new Vector3(s_normal.z, 0f, -s_normal.x);
            Assert.That(Vector3.Dot(result, perpendicular), Is.LessThan(0f));
            Assert.That(memory.Side, Is.EqualTo(-1));
        }

        [Test]
        public void NoHintNoMemory_DefaultsToPositiveSide()
        {
            var memory = new AvoidanceSide();

            Vector3 result = ObstacleAvoidance.Avoid(Vector3.forward, s_normal, 1f, ProbeDistance, ObstacleA, Vector3.zero, 0f, HoldTime, ref memory);

            Vector3 perpendicular = new Vector3(s_normal.z, 0f, -s_normal.x);
            Assert.That(Vector3.Dot(result, perpendicular), Is.GreaterThan(0f));
            Assert.That(memory.Side, Is.EqualTo(1));
        }

        [Test]
        public void CloserHit_RepelsMore()
        {
            Vector3 desired = new Vector3(0.6f, 0f, 0.8f);
            var memoryFar = new AvoidanceSide();
            var memoryNear = new AvoidanceSide();

            Vector3 far = ObstacleAvoidance.Avoid(desired, s_normal, 1.8f, ProbeDistance, ObstacleA, Vector3.zero, 0f, HoldTime, ref memoryFar);
            Vector3 near = ObstacleAvoidance.Avoid(desired, s_normal, 0.2f, ProbeDistance, ObstacleA, Vector3.zero, 0f, HoldTime, ref memoryNear);

            Assert.That(Vector3.Dot(near, s_normal), Is.GreaterThan(Vector3.Dot(far, s_normal)));
        }

        [Test]
        public void Result_IsPlanar()
        {
            var memory = new AvoidanceSide();
            Vector3 desired = new Vector3(0.5f, 0.7f, 0.5f);
            Vector3 tiltedNormal = new Vector3(-0.2f, 0.6f, -0.8f);

            Vector3 result = ObstacleAvoidance.Avoid(desired, tiltedNormal, 0.5f, ProbeDistance, ObstacleA, Vector3.right, 0f, HoldTime, ref memory);

            Assert.That(result.y, Is.EqualTo(0f));
        }
    }
}
