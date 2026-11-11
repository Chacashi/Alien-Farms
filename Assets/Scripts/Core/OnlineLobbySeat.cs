using System;

namespace AlienFarms.Core
{
    /// <summary>One player seat of the online room as seen by the menu (published by the network lobby).</summary>
    public readonly struct OnlineLobbySeat : IEquatable<OnlineLobbySeat>
    {
        /// <summary><see cref="CharacterIndex"/> value meaning "no character picked".</summary>
        public const int NoCharacter = -1;

        public OnlineLobbySeat(PlayerId id, int characterIndex, string displayName, bool isLocal, bool isHost)
        {
            Id = id;
            CharacterIndex = characterIndex < 0 ? NoCharacter : characterIndex;
            DisplayName = displayName;
            IsLocal = isLocal;
            IsHost = isHost;
        }

        public PlayerId Id { get; }

        /// <summary>Roster index of the picked character, <see cref="NoCharacter"/> when none. Picking counts as ready.</summary>
        public int CharacterIndex { get; }

        public string DisplayName { get; }

        /// <summary>True for the seat held by this machine.</summary>
        public bool IsLocal { get; }

        /// <summary>True for the seat held by the host.</summary>
        public bool IsHost { get; }

        public bool HasCharacter => CharacterIndex != NoCharacter;

        public bool Equals(OnlineLobbySeat other)
        {
            return Id == other.Id
                && CharacterIndex == other.CharacterIndex
                && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
                && IsLocal == other.IsLocal
                && IsHost == other.IsHost;
        }

        public override bool Equals(object obj)
        {
            return obj is OnlineLobbySeat other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)Id, CharacterIndex, DisplayName, IsLocal, IsHost);
        }
    }
}
