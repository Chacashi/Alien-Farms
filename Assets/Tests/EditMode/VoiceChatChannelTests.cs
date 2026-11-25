using System.Reflection;
using AlienFarms.Core;
using AlienFarms.Data;
using NUnit.Framework;
using UnityEngine;

namespace AlienFarms.Tests
{
    public sealed class VoiceChatChannelTests
    {
        private VoiceChatChannel _channel;

        [SetUp]
        public void SetUp()
        {
            _channel = ScriptableObject.CreateInstance<VoiceChatChannel>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_channel);
        }

        [Test]
        public void RequestsWithoutListeners_LeaveChannelUnavailableAndStateUnchanged()
        {
            _channel.RequestToggleMic();
            _channel.RequestSetRivalVolume(35);
            _channel.RequestToggleRivalMute();

            Assert.That(_channel.IsAvailable, Is.False);
            Assert.That(_channel.MicMuted, Is.False);
            Assert.That(_channel.RivalVolume, Is.Zero);
            Assert.That(_channel.RivalLocallyMuted, Is.False);
            Assert.That(_channel.LastErrorKey, Is.Null);
        }

        [Test]
        public void RequestsWithListeners_DispatchAndClampVolume()
        {
            int micRequests = 0;
            int rivalMuteRequests = 0;
            int volume = 0;
            _channel.ToggleMicRequested += () => micRequests++;
            _channel.SetRivalVolumeRequested += value => volume = value;
            _channel.ToggleRivalMuteRequested += () => rivalMuteRequests++;
            _channel.SetHasDesiredChannel(true);

            Assert.That(_channel.IsAvailable, Is.True);

            _channel.RequestToggleMic();
            _channel.RequestSetRivalVolume(100);
            _channel.RequestToggleRivalMute();

            Assert.That(micRequests, Is.EqualTo(1));
            Assert.That(rivalMuteRequests, Is.EqualTo(1));
            Assert.That(volume, Is.EqualTo(50));
            Assert.That(_channel.RivalVolume, Is.Zero);
        }

        [Test]
        public void Availability_PublishesWhenFirstListenerArrivesAndLastLeaves()
        {
            int changed = 0;
            void Toggle() { }
            void SetVolume(int volume) { }
            _channel.Changed += () => changed++;

            _channel.ToggleMicRequested += Toggle;

            Assert.That(_channel.IsAvailable, Is.False);
            Assert.That(changed, Is.Zero);

            _channel.SetHasDesiredChannel(true);
            _channel.SetRivalVolumeRequested += SetVolume;
            _channel.ToggleMicRequested -= Toggle;

            Assert.That(_channel.IsAvailable, Is.True);
            Assert.That(changed, Is.EqualTo(1));

            _channel.SetRivalVolumeRequested -= SetVolume;

            Assert.That(_channel.IsAvailable, Is.False);
            Assert.That(changed, Is.EqualTo(2));
        }

        [Test]
        public void Availability_ChangesWithDesiredChannelAndVoiceStatus()
        {
            int changed = 0;
            int requests = 0;
            _channel.ToggleMicRequested += () => requests++;
            _channel.SetStatus(VoiceStatus.LoggedIn);
            _channel.Changed += () => changed++;

            _channel.RequestToggleMic();
            Assert.That(requests, Is.Zero);

            _channel.SetHasDesiredChannel(true);
            Assert.That(_channel.IsAvailable, Is.True);
            _channel.SetHasDesiredChannel(false);
            Assert.That(_channel.IsAvailable, Is.False);

            _channel.SetStatus(VoiceStatus.Joining);
            Assert.That(_channel.IsAvailable, Is.True);
            _channel.SetStatus(VoiceStatus.InChannel);
            Assert.That(_channel.IsAvailable, Is.True);
            _channel.SetStatus(VoiceStatus.LoggedIn);
            Assert.That(_channel.IsAvailable, Is.False);
            Assert.That(changed, Is.EqualTo(5));
        }

        [Test]
        public void RivalPreferences_SurviveReconnectionAndResetForNewSession()
        {
            _channel.SetRivalPreferencesSession("session-a");
            _channel.SetRivalVolume(25);
            _channel.SetRivalLocallyMuted(true);

            _channel.SetRivalPreferencesSession(null);
            _channel.SetRivalPreferencesSession("session-a");
            Assert.That(_channel.RivalVolume, Is.EqualTo(25));
            Assert.That(_channel.RivalLocallyMuted, Is.True);

            int changed = 0;
            _channel.Changed += () => changed++;
            _channel.SetRivalPreferencesSession("session-b");
            Assert.That(_channel.RivalVolume, Is.Zero);
            Assert.That(_channel.RivalLocallyMuted, Is.False);
            Assert.That(changed, Is.EqualTo(1));
        }

        [Test]
        public void SetRivalVolume_PublishesOnlyActualChanges()
        {
            int changed = 0;
            _channel.Changed += () => changed++;

            _channel.SetRivalVolume(100);
            _channel.SetRivalVolume(50);

            Assert.That(_channel.RivalVolume, Is.EqualTo(50));
            Assert.That(changed, Is.EqualTo(1));
        }

        [Test]
        public void NewGeneration_DiscardsStateAndStaleRequestListeners()
        {
            int requests = 0;
            _channel.ToggleMicRequested += () => requests++;
            _channel.SetStatus(VoiceStatus.InChannel);
            _channel.SetChannelName("af-session");
            _channel.SetMicMuted(true);
            _channel.SetLocalSpeaking(true);
            _channel.SetRivalSeat(PlayerId.Player2);
            _channel.SetRivalState(VoiceRivalState.InRange);
            _channel.SetRivalSpeaking(true);
            _channel.SetRivalVolume(25);
            _channel.SetRivalLocallyMuted(true);
            _channel.SetRivalDistance(5f);
            _channel.SetLastErrorKey("voice.error.join");

            MethodInfo reset = typeof(VoiceChatChannel).GetMethod("ResetStatics", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(reset, Is.Not.Null);
            reset.Invoke(null, null);
            Assert.That(_channel.Status, Is.EqualTo(VoiceStatus.Off));
            Assert.That(_channel.IsAvailable, Is.False);

            _channel.SetStatus(VoiceStatus.Initializing);
            _channel.RequestToggleMic();

            Assert.That(requests, Is.Zero);
            Assert.That(_channel.IsAvailable, Is.False);
            Assert.That(_channel.Status, Is.EqualTo(VoiceStatus.Initializing));
            Assert.That(_channel.ChannelName, Is.Null);
            Assert.That(_channel.MicMuted, Is.False);
            Assert.That(_channel.LocalSpeaking, Is.False);
            Assert.That(_channel.RivalSeat, Is.EqualTo(PlayerId.None));
            Assert.That(_channel.RivalState, Is.EqualTo(VoiceRivalState.NoVoice));
            Assert.That(_channel.RivalVolume, Is.Zero);
            Assert.That(_channel.RivalSpeaking, Is.False);
            Assert.That(_channel.RivalLocallyMuted, Is.False);
            Assert.That(_channel.RivalDistance, Is.EqualTo(-1f));
            Assert.That(_channel.LastErrorKey, Is.Null);
        }
    }
}
