using System.Text;
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

        [Test]
        public void Sanitize_LongUgsName_KeepsTheDiscriminator()
        {
            // 33 characters: the part before '#' is shortened, the suffix stays whole.
            string clean = DisplayNameSanitizer.Sanitize("ExceptionalDenyingLightbulb#78609");

            Assert.That(clean, Is.EqualTo("ExceptionalDenyingLightbul#78609"));
            Assert.That(clean, Has.Length.EqualTo(DisplayNameSanitizer.MaxLength));
        }

        [TestCase("ExceptionalDenyingLightbulb#78609", "ExceptionalDenyingLight#78609")]
        [TestCase("ExceptionalDenyingLightbul#78609", "ExceptionalDenyingLight#78609")]
        [TestCase("Short#12", "Short#12")]
        [TestCase("ÑandúÑandúÑandúÑandú#4321", "ÑandúÑandúÑandúÑa#4321")]
        [TestCase("Long name with spaces here#99", "Long name with spaces here#99")]
        [TestCase("Long name with spaces      tail#999", "Long name with spaces#999")]
        public void FitToPayload_KeepsTheDiscriminatorWithinTheBytes(string name, string expected)
        {
            string fitted = DisplayNameSanitizer.FitToPayload(name);

            Assert.That(fitted, Is.EqualTo(expected));
            Assert.That(Encoding.UTF8.GetByteCount(fitted), Is.LessThanOrEqualTo(DisplayNameSanitizer.MaxPayloadBytes));
        }

        [Test]
        public void FitToPayload_NoDiscriminator_CutsOnACodePoint()
        {
            // 28 ASCII bytes then a 4-byte emoji: the emoji does not fit and is dropped whole.
            string name = new string('a', DisplayNameSanitizer.MaxPayloadBytes - 1) + "🛸";
            string ending = "Name#";

            Assert.That(DisplayNameSanitizer.FitToPayload(name), Is.EqualTo(new string('a', DisplayNameSanitizer.MaxPayloadBytes - 1)));
            Assert.That(DisplayNameSanitizer.FitToPayload(ending), Is.SameAs(ending));
        }

        [Test]
        public void Fit_DiscriminatorLeavingNoRoomForTheName_FallsBackToAPlainCut()
        {
            // Budget 8: the suffix "#123456" (7) fits but leaves 1 byte, and the name's first character needs 2 ('Ñ'):
            // with an empty head the suffix alone would lose the name, so the plain cut keeps the name's start instead.
            string fitted = DisplayNameSanitizer.Fit("Ñandú#123456", 8, true);

            Assert.That(fitted, Is.EqualTo("Ñandú#"));
            Assert.That(Encoding.UTF8.GetByteCount(fitted), Is.LessThanOrEqualTo(8));
        }

        [Test]
        public void FitToPayload_FittingName_ReturnsTheSameInstance()
        {
            const string name = "Brave#1234";

            Assert.That(DisplayNameSanitizer.FitToPayload(name), Is.SameAs(name));
            Assert.That(DisplayNameSanitizer.FitToPayload(null), Is.Empty);
        }
    }
}
