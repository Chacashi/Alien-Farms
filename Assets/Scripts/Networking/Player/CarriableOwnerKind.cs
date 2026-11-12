namespace AlienFarms.Networking.Player
{
    /// <summary>Who a carriable object belongs to, as shown by its ownership tint and debug label.</summary>
    public enum CarriableOwnerKind : byte
    {
        /// <summary>Owned by the server and nobody claims it (free, penned, dropped).</summary>
        Server = 0,

        /// <summary>Belongs to a seated player (see <see cref="CarriableOwner.Seat"/>).</summary>
        Player = 1,

        /// <summary>Owned by a client whose avatar is not known on this machine yet (late join).</summary>
        Unresolved = 2
    }
}
