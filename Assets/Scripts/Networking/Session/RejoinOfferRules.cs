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
        /// A failed rejoin keeps the offer when retrying may still work (transient network or service, the server not having
        /// noticed the drop yet: <see cref="OnlineSessionErrorKeys.AlreadyConnected"/>). Every key that does not fall back
        /// to the join by code (<see cref="FallsBackToCodeJoin"/>) is one of them. A full room also keeps it: the offer
        /// expires with the grace. A missing session, a refused seat or a cancellation (null) drop it.
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
                case OnlineSessionErrorKeys.ServicesUnavailable:
                case OnlineSessionErrorKeys.RateLimited:
                case OnlineSessionErrorKeys.RelayFailed:
                case OnlineSessionErrorKeys.Busy:
                case OnlineSessionErrorKeys.Unknown:
                case OnlineSessionErrorKeys.SessionFull:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// The client retries the rejoin by itself (2.9): it holds an offer, is idle in the hub
        /// (<see cref="OnlineSessionStatus.Ready"/>: never while a start is already in flight), the device reports a
        /// network and the retry delay passed. Times are <c>Time.realtimeSinceStartupAsDouble</c> seconds.
        /// </summary>
        public static bool ShouldAutoRejoin(bool hasOffer, OnlineSessionStatus status, bool networkReachable, double now, double nextAttemptAt)
        {
            return hasOffer
                && status == OnlineSessionStatus.Ready
                && networkReachable
                && now >= nextAttemptAt;
        }

        /// <summary>
        /// A failed reconnection to the session falls back to joining again by code, except when the failure says there
        /// is no network or service right now: the join would fail too, with a key that drops the offer
        /// (<see cref="OnlineSessionErrorKeys.JoinFailed"/> / <see cref="OnlineSessionErrorKeys.SessionNotFound"/>).
        /// </summary>
        public static bool FallsBackToCodeJoin(string reconnectErrorKey)
        {
            switch (reconnectErrorKey)
            {
                case OnlineSessionErrorKeys.NoConnection:
                case OnlineSessionErrorKeys.ServicesUnavailable:
                case OnlineSessionErrorKeys.RateLimited:
                    return false;
                default:
                    return true;
            }
        }
    }
}
