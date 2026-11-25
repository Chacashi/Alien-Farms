using System;
using System.Reflection;
using AlienFarms.Core;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class GameEventsResetTests
    {
        [Test]
        public void ResetStatics_ClearsVoiceEvents()
        {
            int participantEvents = 0;
            int speakingEvents = 0;
            int muteEvents = 0;
            Action<PlayerId, bool> participant = (seat, value) =>
            {
                Assert.That(seat, Is.EqualTo(PlayerId.Player1));
                Assert.That(value, Is.True);
                participantEvents++;
            };
            Action<PlayerId, bool> speaking = (seat, value) =>
            {
                Assert.That(seat, Is.EqualTo(PlayerId.Player2));
                Assert.That(value, Is.False);
                speakingEvents++;
            };
            Action<PlayerId, bool> mute = (seat, value) =>
            {
                Assert.That(seat, Is.EqualTo(PlayerId.Player2));
                Assert.That(value, Is.True);
                muteEvents++;
            };
            GameEvents.OnVoiceParticipantChanged += participant;
            GameEvents.OnVoiceSpeakingChanged += speaking;
            GameEvents.OnVoiceMuteChanged += mute;

            try
            {
                RaiseVoiceEvents();
                Assert.That(participantEvents, Is.EqualTo(1));
                Assert.That(speakingEvents, Is.EqualTo(1));
                Assert.That(muteEvents, Is.EqualTo(1));

                MethodInfo reset = typeof(GameEvents).GetMethod("ResetStatics", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.That(reset, Is.Not.Null);
                reset.Invoke(null, null);

                RaiseVoiceEvents();
                Assert.That(participantEvents, Is.EqualTo(1));
                Assert.That(speakingEvents, Is.EqualTo(1));
                Assert.That(muteEvents, Is.EqualTo(1));
            }
            finally
            {
                GameEvents.OnVoiceParticipantChanged -= participant;
                GameEvents.OnVoiceSpeakingChanged -= speaking;
                GameEvents.OnVoiceMuteChanged -= mute;
            }
        }

        private static void RaiseVoiceEvents()
        {
            GameEvents.RaiseVoiceParticipantChanged(PlayerId.Player1, true);
            GameEvents.RaiseVoiceSpeakingChanged(PlayerId.Player2, false);
            GameEvents.RaiseVoiceMuteChanged(PlayerId.Player2, true);
        }
    }
}
