using AlienFarms.Core;

namespace AlienFarms.Networking.Voice
{
    public static class VoiceRivalStateRules
    {
        private const float ReturnMargin = 5f;
        private const double ReturnTimeout = 3d;

        public static (VoiceRivalState State, double? ReturnDeadline) Evaluate(bool present, bool heard,
            bool connected, float distance, float audible, double now, double? returnDeadline)
        {
            if (!connected)
            {
                return (VoiceRivalState.NoVoice, null);
            }

            if (present)
            {
                return (distance > audible ? VoiceRivalState.OutOfRange : VoiceRivalState.InRange, null);
            }

            if (!heard || distance < 0f)
            {
                return (VoiceRivalState.NoVoice, null);
            }

            if (distance > System.Math.Max(0f, audible - ReturnMargin))
            {
                return (VoiceRivalState.OutOfRange, null);
            }

            // Give Vivox time to restore the participant after crossing the inner boundary.
            double deadline = returnDeadline ?? now + ReturnTimeout;
            return (now < deadline ? VoiceRivalState.OutOfRange : VoiceRivalState.NoVoice, deadline);
        }
    }
}
