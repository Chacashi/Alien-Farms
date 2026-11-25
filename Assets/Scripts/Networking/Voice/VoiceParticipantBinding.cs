using System;
using AlienFarms.Core;
using Unity.Services.Vivox;

namespace AlienFarms.Networking.Voice
{
    internal sealed class VoiceParticipantBinding : IDisposable
    {
        private readonly Action<VoiceParticipantBinding> _speechChanged;
        private readonly Action<VoiceParticipantBinding> _muteChanged;

        public VivoxParticipant Participant { get; }
        public PlayerId Seat { get; }

        public VoiceParticipantBinding(VivoxParticipant participant, PlayerId seat,
            Action<VoiceParticipantBinding> speechChanged, Action<VoiceParticipantBinding> muteChanged)
        {
            Participant = participant;
            Seat = seat;
            _speechChanged = speechChanged;
            _muteChanged = muteChanged;
            Participant.ParticipantSpeechDetected += HandleSpeech;
            Participant.ParticipantMuteStateChanged += HandleMute;
        }

        public void Dispose()
        {
            Participant.ParticipantSpeechDetected -= HandleSpeech;
            Participant.ParticipantMuteStateChanged -= HandleMute;
        }

        private void HandleSpeech() => _speechChanged(this);
        private void HandleMute() => _muteChanged(this);
    }
}
