namespace AlienFarms.UI
{
    /// <summary>What the HUD connection banner shows for a seat (<see cref="ConnectionStatusRules.Resolve"/>).</summary>
    public enum ConnectionBanner
    {
        /// <summary>Nothing to show (connected, never dropped).</summary>
        None = 0,

        /// <summary>"Waiting for P2: n s", counting down while the seat is reserved.</summary>
        Reconnecting = 1,

        /// <summary>Toast: the player came back within the grace.</summary>
        Returned = 2,

        /// <summary>Toast: the player left on purpose.</summary>
        Left = 3,

        /// <summary>Toast: the grace ran out.</summary>
        TimedOut = 4
    }
}
