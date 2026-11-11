using AlienFarms.UI;
using NUnit.Framework;

namespace AlienFarms.Tests.Localization
{
    public sealed class LocaleCycleTests
    {
        [TestCase(0, 2, 1, 1)]
        [TestCase(1, 2, 1, 0)]
        [TestCase(0, 2, -1, 1)]
        [TestCase(1, 3, -1, 0)]
        [TestCase(0, 3, -1, 2)]
        [TestCase(2, 3, 4, 0)]
        [TestCase(0, 1, 1, 0)]
        public void Next_WrapsAround(int current, int count, int step, int expected)
        {
            Assert.That(LocaleCycle.Next(current, count, step), Is.EqualTo(expected));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Next_EmptyList_ReturnsMinusOne(int count)
        {
            Assert.That(LocaleCycle.Next(0, count, 1), Is.EqualTo(-1));
        }

        [Test]
        public void Next_CurrentNotFound_StepsFromFirst()
        {
            Assert.That(LocaleCycle.Next(-1, 2, 1), Is.EqualTo(1));
        }
    }
}
