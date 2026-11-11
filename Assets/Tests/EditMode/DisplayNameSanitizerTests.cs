using AlienFarms.Core;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    /// <summary>Player names from other machines: control / format characters and rich text brackets removed, trimmed, capped.</summary>
    public sealed class DisplayNameSanitizerTests
    {
        [TestCase(null, "")]
        [TestCase("", "")]
        [TestCase("Zorg", "Zorg")]
        [TestCase("Ñandú \U0001F6F8", "Ñandú \U0001F6F8")]
        [TestCase("  Ana  ", "Ana")]
        [TestCase("<color=red>Ana</color>", "color=redAna/color")]
        [TestCase("A\nB\tC\u0000", "ABC")]
        [TestCase("Ev​il‮", "Evil")]
        [TestCase(" \u0007 < > ", "")]
        [TestCase("Mid dle", "Mid dle")]
        public void Sanitize_CleansTheName(string name, string expected)
        {
            Assert.That(DisplayNameSanitizer.Sanitize(name), Is.EqualTo(expected));
        }

        [Test]
        public void Sanitize_CleanName_ReturnsTheSameInstance()
        {
            const string name = "Player 1";

            Assert.That(DisplayNameSanitizer.IsClean(name), Is.True);
            Assert.That(DisplayNameSanitizer.Sanitize(name), Is.SameAs(name));
        }

        [TestCase(" Ana")]
        [TestCase("Ana\r")]
        [TestCase("<b>")]
        public void IsClean_DirtyName_IsFalse(string name)
        {
            Assert.That(DisplayNameSanitizer.IsClean(name), Is.False);
        }

        [Test]
        public void Sanitize_LongName_IsCappedWithoutSplittingASurrogatePair()
        {
            string longAscii = new string('a', DisplayNameSanitizer.MaxLength + 10);
            // 31 letters then an emoji (surrogate pair at 31-32): the pair does not fit and is dropped whole.
            string pairAtEdge = new string('b', DisplayNameSanitizer.MaxLength - 1) + "\U0001F6F8";

            Assert.That(DisplayNameSanitizer.Sanitize(longAscii), Has.Length.EqualTo(DisplayNameSanitizer.MaxLength));
            Assert.That(DisplayNameSanitizer.Sanitize(pairAtEdge), Is.EqualTo(new string('b', DisplayNameSanitizer.MaxLength - 1)));
        }
    }
}
