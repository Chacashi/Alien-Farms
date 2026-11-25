using AlienFarms.Core;
using AlienFarms.Data;
using Unity.Services.Vivox;

namespace AlienFarms.Networking.Voice
{
    public static class VoiceFadeModelMapper
    {
        public static AudioFadeModel ToVivox(VoiceFadeModel model) => model switch
        {
            VoiceFadeModel.LinearByDistance => AudioFadeModel.LinearByDistance,
            VoiceFadeModel.ExponentialByDistance => AudioFadeModel.ExponentialByDistance,
            _ => AudioFadeModel.InverseByDistance
        };

        public static Channel3DProperties ToChannel3DProperties(VoiceChatSettings settings) =>
            new Channel3DProperties(settings.AudibleDistance, settings.ConversationalDistance,
                settings.FadeIntensity, ToVivox(settings.FadeModel));
    }
}
