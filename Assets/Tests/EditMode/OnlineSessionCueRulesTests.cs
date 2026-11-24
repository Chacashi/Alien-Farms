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

        [TestCase(OnlineSessionStatus.Creating, false, UiSfxCue.ConnectionSucceeded)]
        [TestCase(OnlineSessionStatus.Joining, false, UiSfxCue.ConnectionSucceeded)]
        [TestCase(OnlineSessionStatus.Joining, true, UiSfxCue.PlayerReconnected)]
        public void OnStatusChanged_EnteringSession_MapsConnectionCue(OnlineSessionStatus previous,
            bool joiningIsRejoin, UiSfxCue expected)
        {
            UiSfxCue cue = OnlineSessionCueRules.OnStatusChanged(previous, OnlineSessionStatus.InSession,
                hasError: false, joiningIsRejoin, roomWasOpen: false);

            Assert.That(cue, Is.EqualTo(expected));
        }

        [TestCase(true, true, UiSfxCue.ConnectionLost)]
        [TestCase(true, false, UiSfxCue.None)]
        [TestCase(false, true, UiSfxCue.None)]
        [TestCase(false, false, UiSfxCue.None)]
        public void OnStatusChanged_LeavingSession_RequiresErrorAndOpenRoom(bool hasError,
            bool roomWasOpen, UiSfxCue expected)
        {
            UiSfxCue cue = OnlineSessionCueRules.OnStatusChanged(OnlineSessionStatus.InSession,
                OnlineSessionStatus.Leaving, hasError, joiningIsRejoin: false, roomWasOpen);

            Assert.That(cue, Is.EqualTo(expected));
        }

        [TestCase(OnlineSessionStatus.Ready, OnlineSessionErrorKeys.HostLeft, true)]
        [TestCase(OnlineSessionStatus.Ready, OnlineSessionErrorKeys.ConnectionLost, true)]
        [TestCase(OnlineSessionStatus.InSession, OnlineSessionErrorKeys.HostLeft, false)]
        [TestCase(OnlineSessionStatus.InSession, OnlineSessionErrorKeys.ConnectionLost, false)]
        [TestCase(OnlineSessionStatus.Ready, OnlineSessionErrorKeys.ConnectionFailed, false)]
        [TestCase(OnlineSessionStatus.Ready, "", false)]
        [TestCase(OnlineSessionStatus.Ready, null, false)]
        public void IsArrivalNotice_RequiresDisconnectErrorOutsideSession(OnlineSessionStatus status,
            string errorKey, bool expected)
        {
            Assert.That(OnlineSessionCueRules.IsArrivalNotice(status, errorKey), Is.EqualTo(expected));
        }
    }
}
