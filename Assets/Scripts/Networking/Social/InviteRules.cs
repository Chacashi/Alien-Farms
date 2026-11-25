using AlienFarms.Core;

namespace AlienFarms.Networking.Social
{
    public static class InviteRules
    {
        private const long ClockSkewToleranceMs = 60_000;
        private const int MaxFallbackNameLength = 24;

        public enum Result
        {
            Valid,
            Malformed,
            WrongVersion,
            FromSelf,
            Expired
        }

        public static Result CreateOffer(SessionInviteMessage message, string localId, string senderId,
            string displayName, long nowUnixMs, long ttlMs, double nowRealtime, out SessionInviteOffer offer)
        {
            offer = default;
            Result result = Validate(message, localId, senderId, nowUnixMs, ttlMs, out string code);
            if (result != Result.Valid)
            {
                return result;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = DisplayNameSanitizer.Sanitize(message.hostName);
                if (displayName.Length > MaxFallbackNameLength)
                {
                    int length = char.IsHighSurrogate(displayName[MaxFallbackNameLength - 1])
                        ? MaxFallbackNameLength - 1 : MaxFallbackNameLength;
                    displayName = displayName.Substring(0, length).TrimEnd();
                }
            }

            // Wall-clock skew determines admission. The local countdown starts on receipt.
            offer = new SessionInviteOffer(senderId, displayName, code, message.sessionId,
                nowRealtime + ttlMs / 1000d);
            return Result.Valid;
        }

        public static Result Validate(SessionInviteMessage message, string localId, string senderId, long nowUnixMs, long ttlMs)
        {
            return Validate(message, localId, senderId, nowUnixMs, ttlMs, out _);
        }

        public static Result Validate(SessionInviteMessage message, string localId, string senderId, long nowUnixMs,
            long ttlMs, out string normalizedCode)
        {
            normalizedCode = null;
            if (message == null || message.type != SessionInviteMessage.ExpectedType)
            {
                return Result.Malformed;
            }

            if (message.version != SessionInviteMessage.CurrentVersion)
            {
                return Result.WrongVersion;
            }

            if (string.IsNullOrWhiteSpace(senderId) || string.IsNullOrWhiteSpace(message.sessionId) ||
                message.sentAtUnixMs <= 0 || !SessionCodeFormat.TryNormalize(message.code, out string code))
            {
                return Result.Malformed;
            }

            if (senderId == localId)
            {
                return Result.FromSelf;
            }

            bool outsideWindow = nowUnixMs >= message.sentAtUnixMs
                ? nowUnixMs - message.sentAtUnixMs - ClockSkewToleranceMs > ttlMs
                : nowUnixMs < message.sentAtUnixMs - ClockSkewToleranceMs;
            if (ttlMs <= 0 || outsideWindow)
            {
                return Result.Expired;
            }

            normalizedCode = code;
            return Result.Valid;
        }
    }
}
