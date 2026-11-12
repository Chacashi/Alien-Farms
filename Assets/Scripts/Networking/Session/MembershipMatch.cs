namespace AlienFarms.Networking.Session
{
    /// <summary>How a joined session (read through a probe) relates to the join code asked for (<see cref="MembershipRules.Classify"/>).</summary>
    public enum MembershipMatch
    {
        /// <summary>The session could not be identified (no or malformed code): neither reconnect to it nor leave it.</summary>
        Unknown = 0,

        /// <summary>The session of the code: reconnect to it.</summary>
        Same = 1,

        /// <summary>Another session: a stale membership, left before joining by code again.</summary>
        Other = 2
    }
}
