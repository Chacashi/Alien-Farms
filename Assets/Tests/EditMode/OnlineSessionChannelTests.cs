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
        private bool _hadSavedName;
        private string _savedName;

        [SetUp]
        public void SetUp()
        {
            // The display name lives in the editor's PlayerPrefs: keep the developer's value and restore it afterwards.
            _hadSavedName = PlayerPrefs.HasKey(OnlineSessionChannel.DisplayNamePrefsKey);
            _savedName = PlayerPrefs.GetString(OnlineSessionChannel.DisplayNamePrefsKey, string.Empty);
            PlayerPrefs.DeleteKey(OnlineSessionChannel.DisplayNamePrefsKey);

            _channel = ScriptableObject.CreateInstance<OnlineSessionChannel>();
            _channel.Changed += CountChange;
            _changed = 0;
        }

        [TearDown]
        public void TearDown()
        {
            _channel.Changed -= CountChange;
            Object.DestroyImmediate(_channel);

            if (_hadSavedName)
            {
                PlayerPrefs.SetString(OnlineSessionChannel.DisplayNamePrefsKey, _savedName);
            }
            else
            {
                PlayerPrefs.DeleteKey(OnlineSessionChannel.DisplayNamePrefsKey);
            }

            PlayerPrefs.Save();
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
        public void LocalPlayerName_IsKeptAcrossSessions_AndClearedOnSignOut()
        {
            MoveToInSession();
            _channel.SetLocalPlayerName("Brave#1234");
            int changes = _changed;

            _channel.SetLocalPlayerName("Brave#1234");
            Assert.That(_changed, Is.EqualTo(changes), "Same name: no change raised.");

            _channel.TrySetStatus(OnlineSessionStatus.Leaving);
            _channel.TrySetStatus(OnlineSessionStatus.Ready);
            Assert.That(_channel.LocalPlayerName, Is.EqualTo("Brave#1234"));

            _channel.TrySetStatus(OnlineSessionStatus.SignedOut);
            Assert.That(_channel.LocalPlayerName, Is.Null);
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

        // ---- Rejoin offer (2.7) ----

        [Test]
        public void NewChannel_HasNoRejoinOffer()
        {
            Assert.That(_channel.HasRejoinOffer, Is.False);
            Assert.That(_channel.RejoinCode, Is.Null);
            Assert.That(_channel.RejoinExpiresAt, Is.EqualTo(0d));
        }

        [Test]
        public void SetRejoinOffer_StoresItOnceAndClearRemovesIt()
        {
            MoveToReady();
            _changed = 0;
            double expiresAt = Time.realtimeSinceStartupAsDouble + 20d;

            _channel.SetRejoinOffer("BCD789", expiresAt);
            _channel.SetRejoinOffer("BCD789", expiresAt);

            Assert.That(_channel.HasRejoinOffer, Is.True);
            Assert.That(_channel.RejoinCode, Is.EqualTo("BCD789"));
            Assert.That(_channel.RejoinExpiresAt, Is.EqualTo(expiresAt));
            Assert.That(_changed, Is.EqualTo(1));

            _channel.ClearRejoinOffer();
            _channel.ClearRejoinOffer();

            Assert.That(_channel.HasRejoinOffer, Is.False);
            Assert.That(_channel.RejoinCode, Is.Null);
            Assert.That(_changed, Is.EqualTo(2));
        }

        [Test]
        public void SetRejoinOffer_EmptyCode_ClearsTheOffer()
        {
            _channel.SetRejoinOffer("BCD789", Time.realtimeSinceStartupAsDouble + 20d);

            _channel.SetRejoinOffer(null, 0d);

            Assert.That(_channel.HasRejoinOffer, Is.False);
        }

        [Test]
        public void RejoinOffer_SurvivesReturningToReady_ButNotSigningOut()
        {
            MoveToInSession();
            _channel.SetRejoinOffer("CODE1", Time.realtimeSinceStartupAsDouble + 20d);

            _channel.TrySetStatus(OnlineSessionStatus.Ready);
            Assert.That(_channel.RejoinCode, Is.EqualTo("CODE1"), "The dropped client is back in the Hub (Ready) with its offer.");

            _channel.TrySetStatus(OnlineSessionStatus.SignedOut);
            Assert.That(_channel.HasRejoinOffer, Is.False);
        }

        [Test]
        public void RequestRejoin_ValidOffer_Dispatches()
        {
            MoveToReady();
            _channel.SetRejoinOffer("BCD789", Time.realtimeSinceStartupAsDouble + 20d);
            _channel.SetError(OnlineSessionErrorKeys.ConnectionLost);
            int requests = 0;
            void OnRejoin() => requests++;
            _channel.RejoinRequested += OnRejoin;

            bool accepted = _channel.RequestRejoin();
            _channel.RejoinRequested -= OnRejoin;

            Assert.That(accepted, Is.True);
            Assert.That(requests, Is.EqualTo(1));
            Assert.That(_channel.HasError, Is.False);
            Assert.That(_channel.HasRejoinOffer, Is.True, "The service clears the offer once it is used.");
        }

        [Test]
        public void RequestRejoin_ExpiredOffer_IsRejectedAndCleared()
        {
            MoveToReady();
            _channel.SetRejoinOffer("BCD789", Time.realtimeSinceStartupAsDouble - 0.5d);
            bool dispatched = false;
            void OnRejoin() => dispatched = true;
            _channel.RejoinRequested += OnRejoin;

            bool accepted = _channel.RequestRejoin();
            _channel.RejoinRequested -= OnRejoin;

            Assert.That(accepted, Is.False);
            Assert.That(dispatched, Is.False);
            Assert.That(_channel.LastErrorKey, Is.EqualTo(OnlineSessionErrorKeys.RejoinExpired));
            Assert.That(_channel.HasRejoinOffer, Is.False);
        }

        [Test]
        public void RequestRejoin_WithoutOffer_IsRejoinExpired()
        {
            MoveToReady();
            _channel.RejoinRequested += NoOp;
            bool accepted = _channel.RequestRejoin();
            _channel.RejoinRequested -= NoOp;

            Assert.That(accepted, Is.False);
            Assert.That(_channel.LastErrorKey, Is.EqualTo(OnlineSessionErrorKeys.RejoinExpired));
        }

        [Test]
        public void RequestRejoin_WithoutService_IsNotConfigured()
        {
            MoveToReady();
            _channel.SetRejoinOffer("BCD789", Time.realtimeSinceStartupAsDouble + 20d);

            Assert.That(_channel.RequestRejoin(), Is.False);
            Assert.That(_channel.LastErrorKey, Is.EqualTo(OnlineSessionErrorKeys.NotConfigured));
            Assert.That(_channel.HasRejoinOffer, Is.True);
        }

        [Test]
        public void RequestRejoin_InSession_IsAlreadyInSession()
        {
            MoveToInSession();
            _channel.SetRejoinOffer("BCD789", Time.realtimeSinceStartupAsDouble + 20d);
            _channel.RejoinRequested += NoOp;
            bool accepted = _channel.RequestRejoin();
            _channel.RejoinRequested -= NoOp;

            Assert.That(accepted, Is.False);
            Assert.That(_channel.LastErrorKey, Is.EqualTo(OnlineSessionErrorKeys.AlreadyInSession));
        }

        // ---- Display name (2.7) ----

        [Test]
        public void DisplayName_EmptyByDefault()
        {
            Assert.That(_channel.DisplayName, Is.Empty);
        }

        [Test]
        public void SetDisplayName_SanitizesPersistsAndRaisesChangedOnce()
        {
            _channel.SetDisplayName("  Ana<b>  ");
            _channel.SetDisplayName("Anab");

            Assert.That(_channel.DisplayName, Is.EqualTo("Anab"));
            Assert.That(_changed, Is.EqualTo(1));
            Assert.That(PlayerPrefs.GetString(OnlineSessionChannel.DisplayNamePrefsKey, null), Is.EqualTo("Anab"));

            var other = ScriptableObject.CreateInstance<OnlineSessionChannel>();
            try
            {
                Assert.That(other.DisplayName, Is.EqualTo("Anab"), "Read back from PlayerPrefs.");
            }
            finally
            {
                Object.DestroyImmediate(other);
            }
        }

        [Test]
        public void SetDisplayName_Empty_ClearsTheSavedName()
        {
            _channel.SetDisplayName("Ana");

            _channel.SetDisplayName("   ");

            Assert.That(_channel.DisplayName, Is.Empty);
            Assert.That(PlayerPrefs.HasKey(OnlineSessionChannel.DisplayNamePrefsKey), Is.False);
        }

        [Test]
        public void DisplayName_SavedValueIsSanitizedOnLoad()
        {
            PlayerPrefs.SetString(OnlineSessionChannel.DisplayNamePrefsKey, " <Bob> ");
            var other = ScriptableObject.CreateInstance<OnlineSessionChannel>();
            try
            {
                Assert.That(other.DisplayName, Is.EqualTo("Bob"));
            }
            finally
            {
                Object.DestroyImmediate(other);
            }
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
