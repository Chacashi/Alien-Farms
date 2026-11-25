namespace AlienFarms.Core
{
    public readonly struct FriendRequestEntry
    {
        public string UgsPlayerId { get; }
        public string DisplayName { get; }
        public bool Incoming { get; }

        public FriendRequestEntry(string ugsPlayerId, string displayName, bool incoming)
        {
            UgsPlayerId = ugsPlayerId;
            DisplayName = displayName;
            Incoming = incoming;
        }
    }
}
