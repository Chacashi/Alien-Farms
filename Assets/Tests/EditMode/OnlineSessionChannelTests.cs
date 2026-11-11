using AlienFarms.Core;
using AlienFarms.Data;
using NUnit.Framework;
using UnityEngine;

namespace AlienFarms.Tests
{
    /// <summary>Online session channel: status transitions, requests, participants, errors and code format.</summary>
    public sealed class OnlineSessionChannelTests
    {
        private OnlineSessionChannel _channel;
        private int _changed;

        [SetUp]
        public void SetUp()
        {
            _channel = ScriptableObject.CreateInstance<OnlineSessionChannel>();
            _channel.Changed += CountChange;
            _changed = 0;
        }

        [TearDown]
        public void TearDown()
        {
            _channel.Changed -= CountChange;
            Object.DestroyImmediate(_channel);
        }

        // ---- Transition rules ----

        [TestCase(OnlineSessionStatus.SignedOut, OnlineSessionStatus.SigningIn)]
        [TestCase(OnlineSessionStatus.SigningIn, OnlineSessionStatus.Ready)]
        [TestCase(OnlineSessionStatus.Ready, OnlineSessionStatus.Creating)]
        [TestCase(OnlineSessionStatus.Ready, OnlineSessionStatus.Joining)]
        [TestCase(OnlineSessionStatus.Creating, OnlineSessionStatus.InSession)]
        [TestCase(OnlineSessionStatus.Joining, OnlineSessionStatus.Ready)]
        [TestCase(OnlineSessionStatus.Creating, OnlineSessionStatus.Leaving)]
        [TestCase(OnlineSessionStatus.Joining, OnlineSessionStatus.Leaving)]
        [TestCase(OnlineSessionStatus.InSession, OnlineSessionStatus.Leaving)]
        [TestCase(OnlineSessionStatus.InSession, OnlineSessionStatus.Ready)]
        [TestCase(OnlineSessionStatus.Leaving, OnlineSessionStatus.Ready)]
        [TestCase(OnlineSessionStatus.InSession, OnlineSessionStatus.SignedOut)]
        public void CanTransition_Allowed(OnlineSessionStatus from, OnlineSessionStatus to)
        {
            Assert.That(OnlineSessionStatusRules.CanTransition(from, to), Is.True);
        }

        [TestCase(OnlineSessionStatus.SignedOut, OnlineSessionStatus.Creating)]
        [TestCase(OnlineSessionStatus.SignedOut, OnlineSessionStatus.Ready)]
        [TestCase(OnlineSessionStatus.SigningIn, OnlineSessionStatus.Joining)]
        [TestCase(OnlineSessionStatus.Ready, OnlineSessionStatus.InSession)]
        [TestCase(OnlineSessionStatus.Ready, OnlineSessionStatus.Leaving)]
        [TestCase(OnlineSessionStatus.Creating, OnlineSessionStatus.Joining)]
        [TestCase(OnlineSessionStatus.InSession, OnlineSessionStatus.Creating)]
        [TestCase(OnlineSessionStatus.Leaving, OnlineSessionStatus.InSession)]
        [TestCase(OnlineSessionStatus.Ready, OnlineSessionStatus.Ready)]
        public void CanTransition_Rejected(OnlineSessionStatus from, OnlineSessionStatus to)
        {
            Assert.That(OnlineSessionStatusRules.CanTransition(from, to), Is.False);
        }

        // ---- Channel state ----

        [Test]
        public void NewChannel_IsSignedOutWithoutSession()
        {
            Assert.That(_channel.Status, Is.EqualTo(OnlineSessionStatus.SignedOut));
            Assert.That(_channel.Code, Is.Null);
            Assert.That(_channel.ParticipantCount, Is.Zero);
            Assert.That(_channel.HasError, Is.False);
        }

        [Test]
        public void TrySetStatus_InvalidTransition_IsIgnoredWithoutChange()
        {
            Assert.That(_channel.TrySetStatus(OnlineSessionStatus.InSession), Is.False);
            Assert.That(_channel.Status, Is.EqualTo(OnlineSessionStatus.SignedOut));
            Assert.That(_changed, Is.Zero);
        }

        [Test]
        public void FullHostFlow_RaisesChangedAndKeepsCode()
        {
            MoveToReady();
            Assert.That(_channel.TrySetStatus(OnlineSessionStatus.Creating), Is.True);
            _channel.SetSession("ABC123", true);
            Assert.That(_channel.TrySetStatus(OnlineSessionStatus.InSession), Is.True);

            Assert.That(_channel.Code, Is.EqualTo("ABC123"));
            Assert.That(_channel.IsHost, Is.True);
            Assert.That(_changed, Is.EqualTo(6));
        }

        [Test]
        public void BackToReady_ClearsSessionData()
        {
            MoveToInSession();
            _channel.SetParticipants(new[] { Participant("a", true) }, 1);

            _channel.TrySetStatus(OnlineSessionStatus.Leaving);
            _channel.TrySetStatus(OnlineSessionStatus.Ready);

            Assert.That(_channel.Code, Is.Null);
            Assert.That(_channel.IsHost, Is.False);
            Assert.That(_channel.ParticipantCount, Is.Zero);
            Assert.That(_channel.LocalPlayerId, Is.EqualTo("local"));
        }

        [Test]
        public void SignedOut_ClearsLocalPlayer()
        {
            MoveToInSession();

            _channel.TrySetStatus(OnlineSessionStatus.SignedOut);

            Assert.That(_channel.LocalPlayerId, Is.Null);
            Assert.That(_channel.Code, Is.Null);
        }

        [Test]
        public void SetParticipants_CapsAtThreeAndSkipsUnchanged()
        {
            OnlineParticipant[] four =
            {
                Participant("a", true), Participant("b", false), Participant("c", false), Participant("d", false)
            };

            _channel.SetParticipants(four, 4);
            int afterFirst = _changed;
            _channel.SetParticipants(four, 4);

            Assert.That(_channel.ParticipantCount, Is.EqualTo(OnlineSessionChannel.MaxParticipants));
            Assert.That(_channel.GetParticipant(2).UgsPlayerId, Is.EqualTo("c"));
            Assert.That(afterFirst, Is.EqualTo(1));
            Assert.That(_changed, Is.EqualTo(1));
        }

        [Test]
        public void GetParticipant_OutOfRange_Throws()
        {
            Assert.That(() => _channel.GetParticipant(0), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        // ---- Requests ----

        [Test]
        public void RequestCreate_WhenReady_DispatchesAndClearsError()
        {
            MoveToReady();
            _channel.SetError(OnlineSessionErrorKeys.Unknown);
            int requests = 0;
            void OnCreate() => requests++;
            _channel.CreateRequested += OnCreate;

            bool accepted = _channel.RequestCreate();
            _channel.CreateRequested -= OnCreate;

            Assert.That(accepted, Is.True);
            Assert.That(requests, Is.EqualTo(1));
            Assert.That(_channel.HasError, Is.False);
        }

        [Test]
        public void RequestCreate_WhenSignedOut_IsRejectedAsBusy()
        {
            _channel.CreateRequested += NoOp;
            bool accepted = _channel.RequestCreate();
            _channel.CreateRequested -= NoOp;

            Assert.That(accepted, Is.False);
            Assert.That(_channel.LastErrorKey, Is.EqualTo(OnlineSessionErrorKeys.Busy));
        }

        [Test]
        public void RequestCreate_InSession_IsRejectedAsAlreadyInSession()
        {
            MoveToInSession();

            Assert.That(_channel.RequestCreate(), Is.False);
            Assert.That(_channel.LastErrorKey, Is.EqualTo(OnlineSessionErrorKeys.AlreadyInSession));
        }

        [Test]
        public void RequestCreate_WithoutService_IsNotConfigured()
        {
            MoveToReady();

            Assert.That(_channel.RequestCreate(), Is.False);
            Assert.That(_channel.LastErrorKey, Is.EqualTo(OnlineSessionErrorKeys.NotConfigured));
        }

        [Test]
        public void RequestJoin_NormalizesTheCode()
        {
            MoveToReady();
            string received = null;
            void OnJoin(string code) => received = code;
            _channel.JoinRequested += OnJoin;

            bool accepted = _channel.RequestJoin("  bcd789 ");
            _channel.JoinRequested -= OnJoin;

            Assert.That(accepted, Is.True);
            Assert.That(received, Is.EqualTo("BCD789"));
        }

        [Test]
        public void RequestJoin_MalformedCode_IsRejectedLocally()
        {
            MoveToReady();
            bool dispatched = false;
            void OnJoin(string code) => dispatched = true;
            _channel.JoinRequested += OnJoin;

            bool accepted = _channel.RequestJoin("AB-12");
            _channel.JoinRequested -= OnJoin;

            Assert.That(accepted, Is.False);
            Assert.That(dispatched, Is.False);
            Assert.That(_channel.LastErrorKey, Is.EqualTo(OnlineSessionErrorKeys.InvalidCode));
        }

        [Test]
        public void LeaveWhileCreating_HoldsLeavingUntilReady()
        {
            MoveToReady();
            _channel.TrySetStatus(OnlineSessionStatus.Creating);
            _channel.LeaveRequested += NoOp;
            bool accepted = _channel.RequestLeave();
            _channel.LeaveRequested -= NoOp;

            Assert.That(accepted, Is.True);
            Assert.That(_channel.TrySetStatus(OnlineSessionStatus.Leaving), Is.True);
            Assert.That(_channel.TrySetStatus(OnlineSessionStatus.InSession), Is.False);
            Assert.That(_channel.RequestCreate(), Is.False);
            Assert.That(_channel.TrySetStatus(OnlineSessionStatus.Ready), Is.True);
        }

        [Test]
        public void RequestLeave_OnlyWhileStartingOrInSession()
        {
            _channel.LeaveRequested += NoOp;
            bool whenSignedOut = _channel.RequestLeave();
            MoveToInSession();
            bool whenInSession = _channel.RequestLeave();
            _channel.LeaveRequested -= NoOp;

            Assert.That(whenSignedOut, Is.False);
            Assert.That(whenInSession, Is.True);
        }

        [Test]
        public void RequestSignIn_IsIdempotentOnceSignedIn()
        {
            int requests = 0;
            void OnSignIn() => requests++;
            _channel.SignInRequested += OnSignIn;

            bool first = _channel.RequestSignIn();
            MoveToReady();
            bool second = _channel.RequestSignIn();
            _channel.SignInRequested -= OnSignIn;

            Assert.That(first, Is.True);
            Assert.That(second, Is.True);
            Assert.That(requests, Is.EqualTo(1));
        }

        // ---- Code format ----

        [TestCase("BCD789", "BCD789")]
        [TestCase(" gHjK6w ", "GHJK6W")]
        [TestCase("6789bcdfghjk", "6789BCDFGHJK")]
        [TestCase("LMNPQRTW", "LMNPQRTW")]
        public void SessionCode_Valid_IsNormalized(string raw, string expected)
        {
            Assert.That(SessionCodeFormat.TryNormalize(raw, out string normalized), Is.True);
            Assert.That(normalized, Is.EqualTo(expected));
        }

        [Test]
        public void SessionCode_AcceptsEveryAlphabetCharacter()
        {
            string alphabet = SessionCodeFormat.Alphabet;
            for (int i = 0; i + SessionCodeFormat.MinLength <= alphabet.Length; i++)
            {
                string code = alphabet.Substring(i, SessionCodeFormat.MinLength);
                Assert.That(SessionCodeFormat.TryNormalize(code, out _), Is.True, code);
            }
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("BCD78")]
        [TestCase("BCDFGHJKLMNPQ")]
        [TestCase("BC 789")]
        [TestCase("BCD-78")]
        [TestCase("ÁBC789")]
        [TestCase("BCD78Z")]
        [TestCase("ABC789")]
        [TestCase("BCD780")]
        [TestCase("BCD781")]
        [TestCase("BCD785")]
        [TestCase("BCD78S")]
        [TestCase("BCD78O")]
        [TestCase("BCD78X")]
        public void SessionCode_Invalid_IsRejected(string raw)
        {
            Assert.That(SessionCodeFormat.TryNormalize(raw, out string normalized), Is.False);
            Assert.That(normalized, Is.Null);
        }

        private void MoveToReady()
        {
            _channel.TrySetStatus(OnlineSessionStatus.SigningIn);
            _channel.SetLocalPlayerId("local");
            _channel.TrySetStatus(OnlineSessionStatus.Ready);
        }

        private void MoveToInSession()
        {
            MoveToReady();
            _channel.TrySetStatus(OnlineSessionStatus.Joining);
            _channel.SetSession("CODE1", false);
            _channel.TrySetStatus(OnlineSessionStatus.InSession);
        }

        private static OnlineParticipant Participant(string id, bool isHost)
        {
            return new OnlineParticipant(id, id, isHost, false, false);
        }

        private void CountChange()
        {
            _changed++;
        }

        private static void NoOp()
        {
        }
    }
}
