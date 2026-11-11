namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Pure connection approval rules (version 1.5): valid payload, same protocol and a free seat. Reconnection to a
    /// previous seat and spectators come in week 2. Reasons are stable keys the client maps to localized text.
    /// </summary>
    public static class ApprovalRules
    {
        public const string ReasonInvalidPayload = DisconnectReasons.InvalidPayload;
        public const string ReasonProtocolMismatch = DisconnectReasons.ProtocolMismatch;
        public const string ReasonRoomFull = DisconnectReasons.RoomFull;

        /// <param name="payloadValid">The connection data parsed into <paramref name="payload"/>.</param>
        /// <param name="payload">What the client sent.</param>
        /// <param name="protocolVersion">Server protocol version.</param>
        /// <param name="occupiedSeats">Seats already held.</param>
        /// <param name="maxPlayers">Seats available in the session.</param>
        public static ApprovalDecision Evaluate(bool payloadValid, in ConnectionPayload payload, int protocolVersion, int occupiedSeats, int maxPlayers)
        {
            if (!payloadValid)
            {
                return ApprovalDecision.Reject(ReasonInvalidPayload);
            }

            if (payload.ProtocolVersion != protocolVersion)
            {
                return ApprovalDecision.Reject(ReasonProtocolMismatch);
            }

            if (occupiedSeats >= maxPlayers)
            {
                return ApprovalDecision.Reject(ReasonRoomFull);
            }

            return ApprovalDecision.Approve();
        }
    }
}
