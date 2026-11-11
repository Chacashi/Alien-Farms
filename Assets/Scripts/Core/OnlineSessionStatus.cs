namespace AlienFarms.Core
{
    /// <summary>Lifecycle of the online session as seen by this machine (see <see cref="OnlineSessionStatusRules"/>).</summary>
    public enum OnlineSessionStatus
    {
        /// <summary>Unity Gaming Services not initialized or not signed in.</summary>
        SignedOut = 0,
        SigningIn = 1,
        /// <summary>Signed in and in no session: Create / Join are allowed.</summary>
        Ready = 2,
        Creating = 3,
        Joining = 4,
        /// <summary>Member of a session with the network running (host or client).</summary>
        InSession = 5,
        Leaving = 6
    }
}
