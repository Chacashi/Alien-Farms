namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Whether the UGS player id a client declared in its <see cref="ConnectionPayload"/> belongs to the host's online
    /// session (<see cref="ApprovalRules.CheckMembership"/>).
    /// </summary>
    public enum SessionMembership
    {
        /// <summary>Nothing to verify: no online session (direct dev connection) or the host's own client.</summary>
        NotRequired,

        /// <summary>The id is a member of the current session.</summary>
        Member,

        /// <summary>Not listed (yet): the host's copy of the session may lag behind the join. Wait and check again.</summary>
        Unconfirmed,

        /// <summary>An online session without a UGS id, or the id was never confirmed: reject.</summary>
        Rejected
    }
}
