using AlienFarms.Core;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Pure rules of the client's reconnection offer: when a drop arms it, until when it lasts and whether a failed
    /// rejoin keeps it for another try. Times are <c>Time.realtimeSinceStartupAsDouble</c> seconds.
    /// </summary>
    public static class RejoinOfferRules
    {
        /// <summary>
        /// A pure client that lost the connection (<see cref="OnlineSessionErrorKeys.ConnectionLost"/>) while in a known
        /// session gets an offer. The host, a kick or a host that left on purpose do not.
        /// </summary>
        public static bool ShouldOffer(bool isHost, string errorKey, string sessionId, string code)
        {
            return !isHost
                && errorKey == OnlineSessionErrorKeys.ConnectionLost
                && !string.IsNullOrEmpty(sessionId)
                && !string.IsNullOrEmpty(code);
        }

        /// <summary>
        /// Seconds the offer ends before the server's grace: the client notices the drop after the server (transport
        /// timeout) and a rejoin still needs time to reach the host, so the button must not outlive the reserved seat.
        /// </summary>
        public const float ExpiryMarginSeconds = 2f;

        /// <summary>
        /// When an offer armed at <paramref name="now"/> expires: the server's grace minus <see cref="ExpiryMarginSeconds"/>
        /// (never before <paramref name="now"/>).
        /// </summary>
        public static double ExpiresAt(double now, float graceSeconds)
        {
            float lasts = graceSeconds - ExpiryMarginSeconds;
            return now + (lasts > 0f ? lasts : 0f);
        }

        public static bool IsExpired(double expiresAt, double now)
        {
            return now >= expiresAt;
        }

        /// <summary>
        /// A failed rejoin keeps the offer when retrying may still work (transient network, the server not having noticed
        /// the drop yet: <see cref="OnlineSessionErrorKeys.AlreadyConnected"/>). A missing session, a full room, a refused
        /// seat or a cancellation (null) drop it.
        /// </summary>
        public static bool KeepsOfferAfterFailure(string errorKey)
        {
            switch (errorKey)
            {
                case OnlineSessionErrorKeys.AlreadyConnected:
                case OnlineSessionErrorKeys.AlreadyInSession:
                case OnlineSessionErrorKeys.ConnectionFailed:
                case OnlineSessionErrorKeys.ConnectionLost:
                case OnlineSessionErrorKeys.NoConnection:
                case OnlineSessionErrorKeys.RateLimited:
                case OnlineSessionErrorKeys.RelayFailed:
                case OnlineSessionErrorKeys.Busy:
                case OnlineSessionErrorKeys.Unknown:
                    return true;
                default:
                    return false;
            }
        }
    }
}
