using AlienFarms.Core;

namespace AlienFarms.Networking.Voice
{
    public static class VoiceDesiredChannelRules
    {
        public static string Desired(OnlineSessionStatus status, string sessionId, string prefix) =>
            status == OnlineSessionStatus.InSession ? VoiceChannelNameRules.Build(prefix, sessionId) : null;
    }
}
