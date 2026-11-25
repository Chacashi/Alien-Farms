using System;
using AlienFarms.UI;

namespace AlienFarms.Networking.Voice
{
    public static class VoiceErrorMapper
    {
        public const string Credentials = LocalizationKeys.VoiceErrorCredentials;
        public const string Login = LocalizationKeys.VoiceErrorLogin;
        public const string Join = LocalizationKeys.VoiceErrorJoin;
        public const string RecoverFailed = LocalizationKeys.VoiceErrorRecoverFailed;

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
