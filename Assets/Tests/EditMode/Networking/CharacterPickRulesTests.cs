using AlienFarms.Networking.Player;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests
{
    /// <summary>Default character of a seat without a pick (<see cref="CharacterPickRules"/>).</summary>
    public sealed class CharacterPickRulesTests
    {
        [Test]
        public void SeatIndex_WhenFree()
        {
            Assert.That(CharacterPickRules.Pick(0, -1, 4), Is.EqualTo(0));
            Assert.That(CharacterPickRules.Pick(1, -1, 4), Is.EqualTo(1));
            Assert.That(CharacterPickRules.Pick(1, 0, 4), Is.EqualTo(1));
            Assert.That(CharacterPickRules.Pick(0, 3, 4), Is.EqualTo(0));
        }

        [Test]
        public void NextOne_WhenTheOtherSeatUsesIt()
        {
            Assert.That(CharacterPickRules.Pick(1, 1, 4), Is.EqualTo(2));
            Assert.That(CharacterPickRules.Pick(0, 0, 4), Is.EqualTo(1));
        }

        [Test]
        public void WrapsAroundTheRoster()
        {
            Assert.That(CharacterPickRules.Pick(1, 1, 2), Is.EqualTo(0));
            Assert.That(CharacterPickRules.Pick(5, -1, 4), Is.EqualTo(1));
            Assert.That(CharacterPickRules.Pick(3, 3, 4), Is.EqualTo(0));
        }

        [Test]
        public void SmallOrEmptyRoster_ReturnsZero()
        {
            Assert.That(CharacterPickRules.Pick(1, 0, 1), Is.EqualTo(0), "A single character is shared.");
            Assert.That(CharacterPickRules.Pick(1, -1, 0), Is.EqualTo(0));
            Assert.That(CharacterPickRules.Pick(-3, -1, 4), Is.EqualTo(0));
        }

        [Test]
        public void NeverMatchesTheOther_WhenTwoOrMoreExist()
        {
            for (int count = 2; count <= 5; count++)
            {
                for (int seat = 0; seat < 2; seat++)
                {
                    for (int other = -1; other < count; other++)
                    {
                        int pick = CharacterPickRules.Pick(seat, other, count);
                        Assert.That(pick, Is.InRange(0, count - 1));
                        Assert.That(pick, Is.Not.EqualTo(other), $"seat {seat}, other {other}, count {count}");
                    }
                }
            }
        }
    }
}
