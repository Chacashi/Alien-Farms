using AlienFarms.Core;
using Unity.Collections;
using UnityEngine;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Server-side record of one player seat (plan: PlayerId, UgsPlayerId, ClientId, Name, Character, Connected,
    /// DisconnectedAt, SavedPose). A seat whose client dropped keeps <see cref="Connected"/> false and
    /// <see cref="DisconnectedAt"/> while it waits for the same <see cref="UgsPlayerId"/> to reconnect
    /// (<see cref="ServerSlotDirectory.MarkDisconnected"/>, used from 2.7).
    /// </summary>
    public struct PlayerSlot
    {
        /// <summary><see cref="CharacterIndex"/> value meaning "no character picked".</summary>
        public const byte NoCharacter = byte.MaxValue;

        public PlayerId Id;
        public ulong ClientId;
        public FixedString32Bytes DisplayName;
        public byte CharacterIndex;
        public bool Connected;
        public FixedString64Bytes UgsPlayerId;

        /// <summary>Server clock (s) when the client dropped; meaningful only while not <see cref="Connected"/>.</summary>
        public double DisconnectedAt;

        public bool HasSavedPose;
        public Vector3 SavedPosition;
        public float SavedFacingYaw;

        public bool IsOccupied => Id != PlayerId.None;

        public bool HasCharacter => CharacterIndex != NoCharacter;

        /// <summary>Held by a client that dropped and may still come back.</summary>
        public bool IsAwaitingReconnect => IsOccupied && !Connected;
    }
}
