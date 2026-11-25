namespace AlienFarms.Core
{
    public readonly struct SessionInviteOffer
    {
        public string FromUgsPlayerId { get; }
        public string FromName { get; }
        public string Code { get; }
        public string SessionId { get; }
        public double ExpiresAtRealtime { get; }

        public SessionInviteOffer(string fromUgsPlayerId, string fromName, string code, string sessionId, double expiresAtRealtime)
        {
            FromUgsPlayerId = fromUgsPlayerId;
            FromName = fromName;
            Code = code;
            SessionId = sessionId;
            ExpiresAtRealtime = expiresAtRealtime;
        }
    }
}
