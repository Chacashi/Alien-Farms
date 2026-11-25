using System;

namespace AlienFarms.Networking.Voice
{
    public static class VoiceChannelPlanner
    {
        public static VoiceChannelStep Next(string desired, string joined, bool busy)
        {
            if (busy || string.Equals(desired, joined, StringComparison.Ordinal))
            {
                return VoiceChannelStep.None;
            }

            if (joined == null)
            {
                return VoiceChannelStep.Join;
            }

            return desired == null ? VoiceChannelStep.Leave : VoiceChannelStep.LeaveThenJoin;
        }
    }
}
