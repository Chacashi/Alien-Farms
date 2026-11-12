namespace AlienFarms.Core
{
    /// <summary>Online presence of a player seat (byte-sized: it is replicated in the seat presence board).</summary>
    public enum PlayerConnectionStatus : byte
    {
        /// <summary>The seat owner is connected (also raised when it comes back after a drop).</summary>
        Connected = 0,

        /// <summary>The connection dropped without notice: the seat is reserved during the reconnection grace.</summary>
        Reconnecting = 1,

        /// <summary>The seat owner left on purpose: the seat was released at once.</summary>
        Left = 2,

        /// <summary>The grace ran out without the seat owner coming back: the seat was released.</summary>
        TimedOut = 3
    }
}
