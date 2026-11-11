namespace AlienFarms.Networking.Session
{
    /// <summary>Outcome of <see cref="ApprovalRules.Evaluate"/>: approved, or rejected with a reason key sent to the client.</summary>
    public readonly struct ApprovalDecision
    {
        private ApprovalDecision(bool approved, string reason)
        {
            Approved = approved;
            Reason = reason;
        }

        public bool Approved { get; }

        /// <summary>Stable reason key (see <see cref="ApprovalRules"/> constants); null when approved.</summary>
        public string Reason { get; }

        public static ApprovalDecision Approve() => new ApprovalDecision(true, null);

        public static ApprovalDecision Reject(string reason) => new ApprovalDecision(false, reason);
    }
}
