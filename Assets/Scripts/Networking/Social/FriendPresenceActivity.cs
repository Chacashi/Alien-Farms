using System;

namespace AlienFarms.Networking.Social
{
    [Serializable]
    public sealed class FriendPresenceActivity
    {
        public const string Hub = "hub";
        public const string Room = "room";
        public const string Match = "match";

        public string state;
    }
}
