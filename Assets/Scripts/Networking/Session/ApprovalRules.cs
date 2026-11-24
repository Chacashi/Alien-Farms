using System;
using AlienFarms.Core;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Pure connection approval rules, in order: same protocol, valid payload, one connection per UGS player (a dropped
    /// player gets its own seat back while the grace lasts), a free seat (Player1 first), otherwise
    /// "room full". Every rejection carries a <see cref="DisconnectReasons"/> key the client maps to localized text.
    /// </summary>
    public static class ApprovalRules
    {
        public const string ReasonInvalidPayload = DisconnectReasons.InvalidPayload;
        public const string ReasonProtocolMismatch = DisconnectReasons.ProtocolMismatch;
        public const string ReasonRoomFull = DisconnectReasons.RoomFull;
        public const string ReasonAlreadyConnected = DisconnectReasons.AlreadyConnected;
        public const string ReasonNotSessionMember = DisconnectReasons.NotSessionMember;

        /// <summary>
        /// Checks the UGS id a client declared against the host's online session, before <see cref="Evaluate"/> trusts it
        /// for "already connected" or a reconnection. Not verified without an online session (direct dev connection) or
        /// for the host's own client. In a session, a client without id is rejected; an id not listed is
        /// <see cref="SessionMembership.Unconfirmed"/> until <paramref name="waitExpired"/>, since the host's copy of the
        /// session's players can lag behind the client's join (NGO may connect before the lobby update arrives).
        /// </summary>
        /// <param name="isHostClient">The host's own client (approved synchronously by NGO; it is the session's host).</param>
        /// <param name="onlineSession">The host is in, or still starting, a UGS session.</param>
        /// <param name="hasUgsPlayerId">The payload carries a UGS player id.</param>
        /// <param name="isListedMember">That id is in the host's current list of session players.</param>
        /// <param name="waitExpired">The time allowed for the session to list the id is over.</param>
        public static SessionMembership CheckMembership(bool isHostClient, bool onlineSession, bool hasUgsPlayerId,
            bool isListedMember, bool waitExpired)
        {
            if (isHostClient || !onlineSession)
            {
                return SessionMembership.NotRequired;
            }

            if (!hasUgsPlayerId)
            {
                return SessionMembership.Rejected;
            }

            if (isListedMember)
            {
                return SessionMembership.Member;
            }

            return waitExpired ? SessionMembership.Rejected : SessionMembership.Unconfirmed;
        }

        /// <summary>
        /// The client's protocol matches and its payload parsed: the only case where its identity is worth verifying
        /// (anything else is rejected by <see cref="Evaluate"/> regardless).
        /// </summary>
        public static bool NeedsIdentity(bool payloadValid, in ConnectionPayload payload, in ApprovalPolicy policy)
        {
            return payloadValid && payload.ProtocolVersion == policy.ProtocolVersion;
        }

        /// <param name="payloadValid">The connection data parsed into <paramref name="payload"/>.</param>
        /// <param name="payload">
        /// What the client sent. When it did not parse, only <see cref="ConnectionPayload.ProtocolVersion"/> may be set
        /// (see <see cref="ConnectionPayload.TryParse"/>): another version is still answered as a protocol mismatch.
        /// </param>
        /// <param name="policy">Protocol, seats and reconnection grace.</param>
        /// <param name="seats">Player seats by index ((int)PlayerId - 1), as held by <see cref="ServerSlotDirectory"/>.</param>
        /// <param name="now">Server clock (s), the same one <see cref="PlayerSlot.DisconnectedAt"/> is written with.</param>
        public static ApprovalDecision Evaluate(bool payloadValid, in ConnectionPayload payload, in ApprovalPolicy policy,
            ReadOnlySpan<PlayerSlot> seats, double now)
        {
            // Version first: an older client's payload has another layout, so it may not parse at all.
            if (payload.ProtocolVersion != policy.ProtocolVersion && (payloadValid || payload.ProtocolVersion != 0))
            {
                return ApprovalDecision.Reject(ReasonProtocolMismatch);
            }

            if (!payloadValid)
            {
                return ApprovalDecision.Reject(ReasonInvalidPayload);
            }

            if (payload.HasUgsPlayerId)
            {
                for (int i = 0; i < seats.Length; i++)
                {
                    PlayerSlot seat = seats[i];
                    if (!seat.IsOccupied || !seat.UgsPlayerId.Equals(payload.UgsPlayerId))
                    {
                        continue;
                    }

                    if (seat.Connected)
                    {
                        return ApprovalDecision.Reject(ReasonAlreadyConnected);
                    }

                    if (!IsReservationExpired(seat, now, policy.ReconnectGraceSeconds))
                    {
                        return ApprovalDecision.Reconnect(seat.Id);
                    }

                    // Grace over: the seat is free again, for this player as for anyone (below).
                }
            }

            int offered = Math.Min(policy.PlayerSeats, seats.Length);
            for (int i = 0; i < offered; i++)
            {
                if (IsSeatFree(seats[i], now, policy.ReconnectGraceSeconds))
                {
                    return ApprovalDecision.AssignSeat((PlayerId)(i + 1));
                }
            }

            return ApprovalDecision.Reject(ReasonRoomFull);
        }

        /// <summary>A seat nobody holds, or one whose dropped player ran out of grace.</summary>
        public static bool IsSeatFree(in PlayerSlot seat, double now, float graceSeconds)
        {
            return !seat.IsOccupied || IsReservationExpired(seat, now, graceSeconds);
        }

        /// <summary>The seat waits for its dropped player and <paramref name="graceSeconds"/> already passed.</summary>
        public static bool IsReservationExpired(in PlayerSlot seat, double now, float graceSeconds)
        {
            return seat.IsAwaitingReconnect && now - seat.DisconnectedAt >= graceSeconds;
        }
    }
}
