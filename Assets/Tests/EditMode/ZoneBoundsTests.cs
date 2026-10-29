using AlienFarms.Zones;
using NUnit.Framework;
using UnityEngine;

namespace AlienFarms.Tests
{
    public sealed class ZoneBoundsTests
    {
        // X in [-40, -20], Z in [-15, 15], Y in [0, 4].
        private static readonly Bounds s_zone = new Bounds(new Vector3(-30f, 2f, 0f), new Vector3(20f, 4f, 30f));

        [Test]
        public void ContainsPlanar_Inside_IsTrue()
        {
            Assert.That(ZoneBounds.ContainsPlanar(s_zone, new Vector3(-30f, 1f, 5f)), Is.True);
        }

        [Test]
        public void ContainsPlanar_Edges_AreInclusive()
        {
            Assert.That(ZoneBounds.ContainsPlanar(s_zone, new Vector3(-20f, 1f, 15f)), Is.True);
            Assert.That(ZoneBounds.ContainsPlanar(s_zone, new Vector3(-40f, 1f, -15f)), Is.True);
        }

        [Test]
        public void ContainsPlanar_JustOutside_IsFalse()
        {
            Assert.That(ZoneBounds.ContainsPlanar(s_zone, new Vector3(-19.99f, 1f, 0f)), Is.False);
            Assert.That(ZoneBounds.ContainsPlanar(s_zone, new Vector3(-30f, 1f, 15.01f)), Is.False);
        }

        [Test]
        public void ContainsPlanar_IgnoresY()
        {
            Assert.That(ZoneBounds.ContainsPlanar(s_zone, new Vector3(-30f, 100f, 0f)), Is.True);
            Assert.That(ZoneBounds.ContainsPlanar(s_zone, new Vector3(-30f, -50f, 0f)), Is.True);
        }
    }
}
