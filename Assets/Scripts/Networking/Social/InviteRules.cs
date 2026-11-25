using AlienFarms.Core;

namespace AlienFarms.Networking.Social
{
    public static class InviteRules
    {
        public enum Result
        {
            Valid,
            Malformed,
            WrongVersion,
            FromSelf,
            Expired
        }

        public static bool TryCreateOffer(SessionInviteMessage message, string localId, string senderId,
            long nowUnixMs, long ttlMs, double nowRealtime, out SessionInviteOffer offer)
        {
            offer = default;
            if (Validate(message, localId, senderId, nowUnixMs, ttlMs, out string code) != Result.Valid)
            {
                return false;
            }

            long elapsedMs = nowUnixMs >= message.sentAtUnixMs ? nowUnixMs - message.sentAtUnixMs : 0L;
            double remainingSeconds = (ttlMs - elapsedMs) / 1000d;
            offer = new SessionInviteOffer(senderId, message.hostName, code, message.sessionId,
                nowRealtime + remainingSeconds);
            return true;
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

            if (ttlMs <= 0 || (nowUnixMs >= message.sentAtUnixMs && nowUnixMs - message.sentAtUnixMs >= ttlMs))
            {
                return Result.Expired;
            }

            normalizedCode = code;
            return Result.Valid;
        }
    }
}
