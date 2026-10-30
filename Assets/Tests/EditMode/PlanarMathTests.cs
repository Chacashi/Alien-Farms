using AlienFarms.Utilities;
using NUnit.Framework;
using UnityEngine;

namespace AlienFarms.Tests
{
    public sealed class PlanarMathTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void KnockbackDirection_IsPlanarUnitAwayFromCenter()
        {
            Vector3 direction = PlanarMath.KnockbackDirection(
                new Vector3(0f, 1f, 0f), new Vector3(3f, 5f, 4f), Vector3.left);

            Assert.That(direction.y, Is.EqualTo(0f));
            Assert.That(direction.magnitude, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(direction.x, Is.EqualTo(0.6f).Within(Tolerance));
            Assert.That(direction.z, Is.EqualTo(0.8f).Within(Tolerance));
        }

        [Test]
        public void KnockbackDirection_Coincident_UsesPlanarFallback()
        {
            Vector3 direction = PlanarMath.KnockbackDirection(
                new Vector3(2f, 0f, 2f), new Vector3(2f, 3f, 2f), new Vector3(0f, 5f, -2f));

            Assert.That(direction.x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(direction.y, Is.EqualTo(0f));
            Assert.That(direction.z, Is.EqualTo(-1f).Within(Tolerance));
        }

        [Test]
        public void KnockbackDirection_CoincidentAndZeroFallback_IsForward()
        {
            Vector3 direction = PlanarMath.KnockbackDirection(Vector3.zero, Vector3.zero, Vector3.zero);

            Assert.That(direction, Is.EqualTo(Vector3.forward));
        }
    }
}
