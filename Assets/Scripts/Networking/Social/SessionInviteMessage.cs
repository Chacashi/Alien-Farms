using System;

namespace AlienFarms.Networking.Social
{
    [Serializable]
    public sealed class SessionInviteMessage
    {
        public const string ExpectedType = "af.invite";
        public const int CurrentVersion = 1;

        public string type = ExpectedType;
        public int version = CurrentVersion;
        public string code;
        public string sessionId;
        public string hostName;
        public long sentAtUnixMs;
    }
}
