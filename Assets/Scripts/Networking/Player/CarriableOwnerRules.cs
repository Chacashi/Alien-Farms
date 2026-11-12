using AlienFarms.Core;

namespace AlienFarms.Networking.Player
{
    /// <summary>
    /// Pure mapping from NGO ownership to what the tint / debug label / log show. On a host the server's client id is
    /// also P1's: what the host carries stays server-owned, so "owned by the server" means P1 only while P1 claims it.
    /// </summary>
    public static class CarriableOwnerRules
    {
        public const string ServerTag = "Server";
        public const string Player1Tag = "P1";
        public const string Player2Tag = "P2";
        public const string UnresolvedTag = "?";

        /// <summary>Number of distinct owner tags (<see cref="TagIndex"/> is below this).</summary>
        public const int TagCount = 4;

        /// <param name="ownerClientId">NGO owner of the object.</param>
        /// <param name="serverClientId">NGO server client id (<c>NetworkManager.ServerClientId</c>).</param>
        /// <param name="claimant">Seat that carries (or, for an armed bomb, threw) the object; None when nobody does.</param>
        /// <param name="ownerClientSeat">Seat whose avatar is owned by <paramref name="ownerClientId"/> on this machine; None when unknown or none.</param>
        public static CarriableOwner Resolve(ulong ownerClientId, ulong serverClientId, PlayerId claimant, PlayerId ownerClientSeat)
        {
            if (ownerClientId != serverClientId)
            {
                return ownerClientSeat != PlayerId.None ? CarriableOwner.ForSeat(ownerClientSeat) : CarriableOwner.Unresolved;
            }

            if (claimant == PlayerId.None)
            {
                return CarriableOwner.Server;
            }

            // Server-owned but claimed: the host's own player keeps server ownership of what it carries. Without the
            // host's avatar known here yet (late join) it cannot be told apart: unresolved until it is.
            if (ownerClientSeat == PlayerId.None)
            {
                return CarriableOwner.Unresolved;
            }

            return ownerClientSeat == claimant ? CarriableOwner.ForSeat(claimant) : CarriableOwner.Server;
        }

        /// <summary>
        /// The label shows while somebody carries the object, or whenever the server does not own it. On a pure client
        /// a claimed but server-owned object is a transient (ownership and replicated state arrive in separate messages:
        /// on grab and on release) and stays hidden instead of flashing "Owner: Server" for a tick.
        /// </summary>
        public static bool IsLabelShown(CarriableOwner owner, PlayerId claimant, bool isPureClient)
        {
            if (owner.Kind == CarriableOwnerKind.Server)
            {
                return claimant != PlayerId.None && !isPureClient;
            }

            return true;
        }

        /// <summary>
        /// Seat to describe the previous owner with: its avatar looked up now, else the seat cached before the change
        /// (the avatar may already be the server's after a disconnect), else the replicated claimant.
        /// </summary>
        public static PlayerId PreviousOwnerSeat(PlayerId lookedUp, PlayerId cached, PlayerId claimant) =>
            lookedUp != PlayerId.None ? lookedUp : cached != PlayerId.None ? cached : claimant;

        /// <summary>Stable index of the owner's tag: 0 Server, 1 P1, 2 P2, 3 unresolved.</summary>
        public static int TagIndex(CarriableOwner owner)
        {
            switch (owner.Kind)
            {
                case CarriableOwnerKind.Player:
                    return owner.Seat == PlayerId.Player1 ? 1 : owner.Seat == PlayerId.Player2 ? 2 : 3;
                case CarriableOwnerKind.Server:
                    return 0;
                default:
                    return 3;
            }
        }

        public static string Tag(CarriableOwner owner)
        {
            switch (TagIndex(owner))
            {
                case 0:
                    return ServerTag;
                case 1:
                    return Player1Tag;
                case 2:
                    return Player2Tag;
                default:
                    return UnresolvedTag;
            }
        }

        public static string SeatTag(PlayerId seat) =>
            seat == PlayerId.Player1 ? Player1Tag : seat == PlayerId.Player2 ? Player2Tag : UnresolvedTag;

        /// <summary>
        /// Log text for an NGO client id (allocates: only for ownership-change logs). Examples: "P2 (client 1)",
        /// "Server/P1 (client 0)" on a host, "Server (client 0)" without a seat, "? (client 7)" for an unknown client.
        /// </summary>
        public static string DescribeClient(ulong clientId, ulong serverClientId, PlayerId seat)
        {
            if (clientId == serverClientId)
            {
                return seat != PlayerId.None
                    ? $"{ServerTag}/{SeatTag(seat)} (client {clientId})"
                    : $"{ServerTag} (client {clientId})";
            }

            return $"{SeatTag(seat)} (client {clientId})";
        }
    }
}
