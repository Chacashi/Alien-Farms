using AlienFarms.Utilities;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class SystemRandomSourceTests
    {
        [Test]
        public void Range_StaysWithinBounds()
        {
            var rng = new SystemRandomSource(7);
            for (int i = 0; i < 1000; i++)
            {
                int value = rng.Range(-3, 5);
                Assert.That(value, Is.GreaterThanOrEqualTo(-3).And.LessThan(5));
            }
        }

        [Test]
        public void Range_EmptyRange_ReturnsMin()
        {
            var rng = new SystemRandomSource(7);
            Assert.That(rng.Range(4, 4), Is.EqualTo(4));
            Assert.That(rng.Range(4, 2), Is.EqualTo(4));
        }

        [Test]
        public void Value01_StaysWithinBounds()
        {
            var rng = new SystemRandomSource(11);
            for (int i = 0; i < 1000; i++)
            {
                Assert.That(rng.Value01(), Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
            }
        }

        [Test]
        public void SameSeed_SameSequence()
        {
            var a = new SystemRandomSource(12345);
            var b = new SystemRandomSource(12345);
            for (int i = 0; i < 100; i++)
            {
                Assert.That(a.Range(0, 1000), Is.EqualTo(b.Range(0, 1000)));
                Assert.That(a.Value01(), Is.EqualTo(b.Value01()));
            }
        }
    }
}
