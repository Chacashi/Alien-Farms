using AlienFarms.Items;
using NUnit.Framework;
using UnityEngine;

namespace AlienFarms.Tests
{
    public sealed class BombPlacementTests
    {
        private const float Tolerance = 0.0001f;

        // Center (10, 2, -4), size 20 x 1 x 10: extents 10 x 5 on XZ.
        private static readonly Bounds Area = new Bounds(new Vector3(10f, 2f, -4f), new Vector3(20f, 1f, 10f));

        [Test]
        public void MinValues_LandOnInnerMinEdge()
        {
            var rng = ScriptedRandomSource.WithFloats(0f, 0f);

            Vector3 point = BombPlacement.RandomPointInArea(Area, 3f, rng);

            Assert.That(point.x, Is.EqualTo(3f).Within(Tolerance));
            Assert.That(point.z, Is.EqualTo(-6f).Within(Tolerance));
        }

        [Test]
        public void MaxValues_LandOnInnerMaxEdge()
        {
            var rng = ScriptedRandomSource.WithFloats(1f, 1f);

            Vector3 point = BombPlacement.RandomPointInArea(Area, 3f, rng);

            Assert.That(point.x, Is.EqualTo(17f).Within(Tolerance));
            Assert.That(point.z, Is.EqualTo(-2f).Within(Tolerance));
        }

        [Test]
        public void MarginLargerThanHalf_CollapsesToCenter()
        {
            var rng = ScriptedRandomSource.WithFloats(0f, 1f);

            Vector3 point = BombPlacement.RandomPointInArea(Area, 50f, rng);

            Assert.That(point.x, Is.EqualTo(10f).Within(Tolerance));
            Assert.That(point.z, Is.EqualTo(-4f).Within(Tolerance));
        }

        [TestCase(0f)]
        [TestCase(0.7f)]
        [TestCase(1f)]
        public void Height_IsAreaCenter(float value01)
        {
            var rng = ScriptedRandomSource.WithFloats(value01, value01);

            Vector3 point = BombPlacement.RandomPointInArea(Area, 3f, rng);

            Assert.That(point.y, Is.EqualTo(2f));
        }
    }
}
