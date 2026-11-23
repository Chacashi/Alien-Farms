using AlienFarms.Audio;
using AlienFarms.Core;
using AlienFarms.Data;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class OnlineSessionCueRulesTests
    {
        [TestCase(OnlineSessionStatus.Ready, OnlineSessionStatus.Creating, false, false, UiSfxCue.CreateRoom)]
        [TestCase(OnlineSessionStatus.Ready, OnlineSessionStatus.Joining, false, false, UiSfxCue.JoinRoom)]
        [TestCase(OnlineSessionStatus.Ready, OnlineSessionStatus.Joining, false, true, UiSfxCue.None)]
        [TestCase(OnlineSessionStatus.Creating, OnlineSessionStatus.Ready, true, false, UiSfxCue.None)]
        [TestCase(OnlineSessionStatus.Joining, OnlineSessionStatus.Ready, true, false, UiSfxCue.None)]
        [TestCase(OnlineSessionStatus.Ready, OnlineSessionStatus.Ready, true, false, UiSfxCue.None)]
        public void OnStatusChanged_MapsCreateJoinAndFailures(OnlineSessionStatus previous, OnlineSessionStatus current,
            bool hasError, bool joiningIsRejoin, UiSfxCue expected)
        {
            UiSfxCue cue = OnlineSessionCueRules.OnStatusChanged(previous, current, hasError, joiningIsRejoin, roomWasOpen: false);

            Assert.That(cue, Is.EqualTo(expected));
        }
    }
}
