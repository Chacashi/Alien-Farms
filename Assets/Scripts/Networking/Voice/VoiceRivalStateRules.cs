using AlienFarms.Core;

namespace AlienFarms.Networking.Voice
{
    public static class VoiceRivalStateRules
    {
        public static VoiceRivalState Evaluate(bool inChannel, float distance, float audible)
        {
            if (!inChannel)
            {
                return VoiceRivalState.NoVoice;
            }

            return distance > audible ? VoiceRivalState.OutOfRange : VoiceRivalState.InRange;
        }
    }
}
