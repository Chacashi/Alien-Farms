using AlienFarms.Core;
using AlienFarms.Match;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class CaptureRulesTests
    {
        [Test]
        public void ReleasedInsideOwnZoneWhilePlaying_Counts()
        {
            Assert.That(CaptureRules.CountsAsCapture(DropReason.Released, PlayerId.Player1, PlayerId.Player1, true, true), Is.True);
        }

        [TestCase(DropReason.Stolen)]
        [TestCase(DropReason.Forced)]
        public void NonVoluntaryDrop_DoesNotCount(DropReason reason)
        {
            Assert.That(CaptureRules.CountsAsCapture(reason, PlayerId.Player1, PlayerId.Player1, true, true), Is.False);
        }

        [Test]
        public void RivalZone_DoesNotCount()
        {
            Assert.That(CaptureRules.CountsAsCapture(DropReason.Released, PlayerId.Player1, PlayerId.Player2, true, true), Is.False);
        }

        [Test]
        public void OutsideZone_DoesNotCount()
        {
            Assert.That(CaptureRules.CountsAsCapture(DropReason.Released, PlayerId.Player2, PlayerId.Player2, false, true), Is.False);
        }

        [Test]
        public void NotPlaying_DoesNotCount()
        {
            Assert.That(CaptureRules.CountsAsCapture(DropReason.Released, PlayerId.Player1, PlayerId.Player1, true, false), Is.False);
        }

        [Test]
        public void HolderNone_DoesNotCount()
        {
            Assert.That(CaptureRules.CountsAsCapture(DropReason.Released, PlayerId.None, PlayerId.None, true, true), Is.False);
        }
    }
}
