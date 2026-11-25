namespace AlienFarms.Core
{
    public readonly struct FriendEntry
    {
        public string UgsPlayerId { get; }
        public string DisplayName { get; }
        public FriendAvailability Availability { get; }
        public string ActivityKey { get; }
        public long LastSeenUnixMs { get; }

        public FriendEntry(string ugsPlayerId, string displayName, FriendAvailability availability, string activityKey, long lastSeenUnixMs)
        {
            UgsPlayerId = ugsPlayerId;
            DisplayName = displayName;
            Availability = availability;
            ActivityKey = activityKey;
            LastSeenUnixMs = lastSeenUnixMs;
        }
    }
}
