namespace AlienFarms.Networking.Session
{
    /// <summary>Outcome of a character request in the online room (see <see cref="LobbyRules.EvaluateRequest"/>).</summary>
    public enum LobbyRequestResult : byte
    {
        Accepted = 0,
        Released = 1,
        Unchanged = 2,
        RejectedNotSeated = 3,
        RejectedOutOfRange = 4,
        RejectedTaken = 5,
        RejectedClosed = 6,

        /// <summary>Asked for another character while holding one: release it first (pick = ready).</summary>
        RejectedLocked = 7
    }
}
