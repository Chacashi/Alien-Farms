using AlienFarms.Core;
using AlienFarms.Networking.Persistence;
using AlienFarms.Networking.Session;
using NUnit.Framework;
using UnityEngine;

namespace AlienFarms.Networking.Tests
{
    /// <summary>
    /// Client side of the host leaving notice: the flag and event lifecycle of <see cref="LeaveNoticeMessenger"/>, and the
    /// round it closes early (a win, counted once whatever arrives later) (B1 / I2).
    /// </summary>
    public sealed class HostLeavingNoticeTests
    {
        private GameObject _go;
        private LeaveNoticeMessenger _notice;
        private int _announced;

        [SetUp]
        public void SetUp()
        {
            _announced = 0;
            _go = new GameObject("NoticeUnderTest");
            _notice = _go.AddComponent<LeaveNoticeMessenger>();
            _notice.HostLeavingAnnounced += CountAnnounced;
        }

        [TearDown]
        public void TearDown()
        {
            _notice.HostLeavingAnnounced -= CountAnnounced;
            Object.DestroyImmediate(_go);
        }

        private void CountAnnounced()
        {
            _announced++;
        }

        [Test]
        public void NewMessenger_HasNoAnnouncement()
        {
            Assert.That(_notice.HostAnnouncedLeaving, Is.False);
        }

        [Test]
        public void Notice_SetsTheFlagAndRaisesOnce()
        {
            _notice.ReceiveHostLeavingNotice();
            _notice.ReceiveHostLeavingNotice();

            Assert.That(_notice.HostAnnouncedLeaving, Is.True);
            Assert.That(_announced, Is.EqualTo(1));
        }

        [Test]
        public void NewClientRun_ClearsTheFlag_AndTheNextNoticeRaisesAgain()
        {
            _notice.ReceiveHostLeavingNotice();
            _notice.ResetAnnouncement();

            Assert.That(_notice.HostAnnouncedLeaving, Is.False);

            _notice.ReceiveHostLeavingNotice();
            Assert.That(_announced, Is.EqualTo(2));
        }

        [Test]
        public void ThrowingListener_StillSetsTheFlag()
        {
            _notice.HostLeavingAnnounced += Throw;
            UnityEngine.TestTools.LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("listener"));

            _notice.ReceiveHostLeavingNotice();

            _notice.HostLeavingAnnounced -= Throw;
            Assert.That(_notice.HostAnnouncedLeaving, Is.True);
        }

        private static void Throw()
        {
            throw new System.InvalidOperationException("listener");
        }

        [Test]
        public void NoticeWinDuringPlaying_IsCountedOnce_WhateverArrivesLater()
        {
            var tally = new MatchStatsTally();
            tally.OnPhaseChanged(MatchPhase.Countdown, MatchPhase.Playing);

            // Notice: the recorder closes the round as a win.
            Assert.That(tally.TryCompleteEarly(true, 2, 0, out PlayerStatsSummary win), Is.True);
            Assert.That(win.Wins, Is.EqualTo(1));

            // Then the host_disconnected drop, a round end replicated in the window, or this player's own exit.
            Assert.That(tally.TryCompleteEarly(true, 2, 0, out _), Is.False);
            Assert.That(tally.TryComplete(PlayerId.Player2, new MatchResult(PlayerId.Player1, 5, 2), 2, 0, out _), Is.False);
            Assert.That(tally.TryCompleteEarly(false, 2, 0, out _), Is.False);
        }

        [Test]
        public void NoticeOutsidePlaying_RecordsNothing()
        {
            var tally = new MatchStatsTally();
            tally.OnPhaseChanged(MatchPhase.None, MatchPhase.Countdown);

            Assert.That(tally.TryCompleteEarly(true, 0, 0, out _), Is.False);
        }
    }
}
