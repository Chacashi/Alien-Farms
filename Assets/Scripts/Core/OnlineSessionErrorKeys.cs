namespace AlienFarms.Core
{
    /// <summary>
    /// Localization keys (UI table) of the errors the online session reports through the session channel. The only
    /// error strings the online flow produces; the UI maps them to text. Null / empty means "no error".
    /// </summary>
    public static class OnlineSessionErrorKeys
    {
        public const string SignInFailed = "online.error.sign_in_failed";
        public const string ServicesUnavailable = "online.error.services_unavailable";
        public const string NoConnection = "online.error.no_connection";
        public const string RateLimited = "online.error.rate_limited";
        public const string InvalidCode = "online.error.invalid_code";
        public const string SessionNotFound = "online.error.session_not_found";
        public const string SessionFull = "online.error.session_full";
        public const string ProtocolMismatch = "online.error.protocol_mismatch";
        /// <summary>The host refused this player because the same account is already connected to the room.</summary>
        public const string AlreadyConnected = "online.error.already_connected";
        /// <summary>The host could not confirm this player as a member of its online session.</summary>
        public const string NotSessionMember = "online.error.not_session_member";
        public const string HostLeft = "online.error.host_left";
        public const string RelayFailed = "online.error.relay_failed";
        public const string ConnectionFailed = "online.error.connection_failed";
        /// <summary>Join refused without a specific reason (e.g. invalid code).</summary>
        public const string JoinFailed = "online.error.join_failed";
        public const string AlreadyInSession = "online.error.already_in_session";
        public const string Busy = "online.error.busy";
        public const string NotConfigured = "online.error.not_configured";
        public const string Unknown = "online.error.unknown";

        // Online room (lobby).
        /// <summary>The requested character is already picked by the other player.</summary>
        public const string CharacterTaken = "online.error.character_taken";
        /// <summary>The request came from a participant without a player seat (spectator).</summary>
        public const string NotSeated = "online.error.not_seated";
        /// <summary>Start requested while the room does not allow it (missing player or pick).</summary>
        public const string CannotStart = "online.error.cannot_start";
        /// <summary>The gameplay scene could not be loaded after starting.</summary>
        public const string StartFailed = "online.error.start_failed";
    }
}
