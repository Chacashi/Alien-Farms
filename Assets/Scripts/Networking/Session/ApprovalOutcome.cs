namespace AlienFarms.Networking.Session
{
    /// <summary>What an <see cref="ApprovalDecision"/> lets the connecting client do.</summary>
    public enum ApprovalOutcome
    {
        /// <summary>Not admitted; <see cref="ApprovalDecision.Reason"/> says why.</summary>
        Rejected = 0,

        /// <summary>Takes the free (or expired) seat <see cref="ApprovalDecision.Seat"/>.</summary>
        AssignSeat = 1,

        /// <summary>Returns to its own seat <see cref="ApprovalDecision.Seat"/>, kept while it was disconnected.</summary>
        Reconnect = 2,

        /// <summary>Joins without a seat or avatar, only watching.</summary>
        Spectator = 3
    }
}
