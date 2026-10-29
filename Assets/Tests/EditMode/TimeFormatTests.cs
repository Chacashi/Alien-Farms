using AlienFarms.Match;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class TimeFormatTests
    {
        [TestCase(300, "5:00")]
        [TestCase(59, "0:59")]
        [TestCase(0, "0:00")]
        [TestCase(605, "10:05")]
        [TestCase(-3, "0:00")]
        public void WriteMinutesSeconds_FormatsAsMinutesAndPaddedSeconds(int totalSeconds, string expected)
        {
            char[] buffer = new char[8];

            int length = TimeFormat.WriteMinutesSeconds(totalSeconds, buffer);

            Assert.That(new string(buffer, 0, length), Is.EqualTo(expected));
        }
    }
}
