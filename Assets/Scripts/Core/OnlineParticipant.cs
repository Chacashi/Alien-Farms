using System;

namespace AlienFarms.Core
{
    /// <summary>One member of the online session as listed by Sessions (host first, then by join order).</summary>
    public readonly struct OnlineParticipant : IEquatable<OnlineParticipant>
    {
        public OnlineParticipant(string ugsPlayerId, string displayName, bool isHost, bool isLocal, bool isSpectator)
        {
            UgsPlayerId = ugsPlayerId;
            DisplayName = displayName;
            IsHost = isHost;
            IsLocal = isLocal;
            IsSpectator = isSpectator;
        }

        /// <summary>Unity Authentication player id.</summary>
        public string UgsPlayerId { get; }

        public string DisplayName { get; }

        public bool IsHost { get; }

        /// <summary>True for the player signed in on this machine.</summary>
        public bool IsLocal { get; }

        /// <summary>Joined after both player seats were taken (third connection).</summary>
        public bool IsSpectator { get; }

        public bool Equals(OnlineParticipant other)
        {
            return string.Equals(UgsPlayerId, other.UgsPlayerId, StringComparison.Ordinal)
                && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
                && IsHost == other.IsHost
                && IsLocal == other.IsLocal
                && IsSpectator == other.IsSpectator;
        }

        public override bool Equals(object obj)
        {
            return obj is OnlineParticipant other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(UgsPlayerId, DisplayName, IsHost, IsLocal, IsSpectator);
        }
    }
}
