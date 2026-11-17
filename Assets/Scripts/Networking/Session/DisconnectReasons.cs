namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// The only disconnect / rejection reasons the game sends, as stable keys the UI maps to text. NGO fills
    /// DisconnectReason with raw transport text when the server gave none, so clients show only these keys
    /// (<see cref="Normalize"/>): a drop without a game reason is <see cref="ConnectionLost"/> (the client may rejoin;
    /// if the host really went away, that rejoin fails with "host left").
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
        /// <summary>The host refused to give a reserved seat back to the reconnecting client.</summary>
        public const string ReconnectDenied = "reconnect_denied";
        /// <summary>
        /// Not sent by the server: what a drop without a game reason (transport timeout, raw transport text) normalizes to.
        /// </summary>
        public const string ConnectionLost = "connection_lost";

        public static bool IsKnown(string reason)
        {
            return reason == InvalidPayload
                || reason == ProtocolMismatch
                || reason == RoomFull
                || reason == AlreadyConnected
                || reason == HostDisconnected
                || reason == NotSessionMember
                || reason == ReconnectDenied
                || reason == ConnectionLost;
        }

        /// <summary><paramref name="reason"/> when it is a known key; otherwise <see cref="ConnectionLost"/>.</summary>
        public static string Normalize(string reason)
        {
            return IsKnown(reason) ? reason : ConnectionLost;
        }

        /// <summary>
        /// Reason of a pure client's drop: <paramref name="reason"/> normalized, except that a drop without a game reason
        /// after the host announced it was leaving (<see cref="LeaveNoticeMessenger.HostAnnouncedLeaving"/>) is
        /// <see cref="HostDisconnected"/>: the host's own reason message can be lost with the closing connection.
        /// </summary>
        public static string Resolve(string reason, bool hostAnnouncedLeaving)
        {
            string normalized = Normalize(reason);
            return hostAnnouncedLeaving && normalized == ConnectionLost ? HostDisconnected : normalized;
        }
    }
}
