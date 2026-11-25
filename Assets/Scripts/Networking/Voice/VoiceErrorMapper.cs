using System;

namespace AlienFarms.Networking.Voice
{
    public static class VoiceErrorMapper
    {
        public const string Credentials = "voice.error.credentials";
        public const string Login = "voice.error.login";
        public const string Join = "voice.error.join";
        public const string RecoverFailed = "voice.error.recover_failed";

        public static string FromException(Exception exception, string fallback)
        {
            for (Exception cause = exception; cause != null; cause = cause.InnerException)
            {
                string message = cause.Message;
                if (Contains(message, "credential") || Contains(message, "invalid auth token") ||
                    Contains(message, "invalid app token") || Contains(message, "invalid signature") ||
                    Contains(message, "issuer mismatch"))
                {
                    return Credentials;
                }
            }

            return fallback;
        }

        private static bool Contains(string message, string value) =>
            message.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
