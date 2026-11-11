using AlienFarms.Core;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Outcome of <see cref="ApprovalRules.Evaluate"/>: how the client is admitted (and to which seat), or rejected with a
    /// reason key sent to the client.
    /// </summary>
    public readonly struct ApprovalDecision
    {
        private ApprovalDecision(ApprovalOutcome outcome, PlayerId seat, string reason)
        {
            Outcome = outcome;
            Seat = seat;
            Reason = reason;
        }

        public ApprovalOutcome Outcome { get; }

        /// <summary>Seat taken or recovered; <see cref="PlayerId.None"/> for a spectator or a rejection.</summary>
        public PlayerId Seat { get; }

        /// <summary>Stable reason key (<see cref="DisconnectReasons"/>); null when approved.</summary>
        public string Reason { get; }

        public bool Approved => Outcome != ApprovalOutcome.Rejected;

        public static ApprovalDecision AssignSeat(PlayerId seat) => new ApprovalDecision(ApprovalOutcome.AssignSeat, seat, null);

        public static ApprovalDecision Reconnect(PlayerId seat) => new ApprovalDecision(ApprovalOutcome.Reconnect, seat, null);

        public static ApprovalDecision Spectator() => new ApprovalDecision(ApprovalOutcome.Spectator, PlayerId.None, null);

        public static ApprovalDecision Reject(string reason) => new ApprovalDecision(ApprovalOutcome.Rejected, PlayerId.None, reason);
    }
}
