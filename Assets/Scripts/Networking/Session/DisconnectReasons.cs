namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// The only disconnect / rejection reasons the game sends, as stable keys the UI maps to text. NGO fills
    /// DisconnectReason with raw transport text when the server gave none, so clients show only these keys
    /// (<see cref="Normalize"/>).
    /// </summary>
    public static class DisconnectReasons
    {
        public const string InvalidPayload = "invalid_payload";
        public const string ProtocolMismatch = "protocol_mismatch";
        public const string RoomFull = "room_full";
        /// <summary>The same Unity Gaming Services player is already connected to this session (another device / window).</summary>
        public const string AlreadyConnected = "already_connected";
        public const string HostDisconnected = "host_disconnected";
        /// <summary>The declared UGS player id is not a member of the host's online session.</summary>
        public const string NotSessionMember = "not_session_member";

        public static bool IsKnown(string reason)
        {
            return reason == InvalidPayload
                || reason == ProtocolMismatch
                || reason == RoomFull
                || reason == AlreadyConnected
                || reason == HostDisconnected
                || reason == NotSessionMember;
        }

        /// <summary><paramref name="reason"/> when it is a known key; otherwise <see cref="HostDisconnected"/>.</summary>
        public static string Normalize(string reason)
        {
            return IsKnown(reason) ? reason : HostDisconnected;
        }
    }
}
