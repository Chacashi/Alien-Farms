using AlienFarms.Core;
using Unity.Collections;
using UnityEngine;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Server-side record of one player seat. Reconnection fields (<see cref="UgsPlayerId"/>, <see cref="DisconnectedAt"/>,
    /// <see cref="SavedPosition"/>, <see cref="SavedFacingYaw"/>) are reserved for week 2 and not used yet.
    /// </summary>
    public struct PlayerSlot
    {
        public PlayerId Id;
        public ulong ClientId;
        public FixedString32Bytes DisplayName;
        public byte CharacterIndex;
        public bool Connected;
        public FixedString64Bytes UgsPlayerId;
        public double DisconnectedAt;
        public Vector3 SavedPosition;
        public float SavedFacingYaw;

        public bool IsOccupied => Id != PlayerId.None;
    }
}
