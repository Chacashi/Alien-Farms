using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Player
{
    /// <summary>
    /// Authoritative state of one player after a server physics step, sent unreliably every step. <see cref="ServerTime"/>
    /// is on the NGO server clock, so clients render it at ServerTime - InterpolationDelay.
    /// </summary>
    public struct PlayerSnapshot : INetworkSerializable
    {
        /// <summary>Server physics step counter of this player (monotonic, starts at 1).</summary>
        public uint Tick;
        public double ServerTime;
        public Vector3 Position;
        public Vector3 Velocity;

        /// <summary>Planar facing (deg around Y, 0 = +Z).</summary>
        public float FacingYaw;
        public float StunRemaining;

        /// <summary>Last owner command the server applied (0 = none).</summary>
        public uint AckSeq;

        /// <summary>
        /// Bumped by the server on every instant move (respawn). Carried by every snapshot, so a teleport is still seen
        /// when the unreliable snapshot that started the epoch is lost. Wraps around (only changes matter).
        /// </summary>
        public byte TeleportEpoch;
        public PlayerSnapshotFlags Flags;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            byte flags = (byte)Flags;
            serializer.SerializeValue(ref Tick);
            serializer.SerializeValue(ref ServerTime);
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Velocity);
            serializer.SerializeValue(ref FacingYaw);
            serializer.SerializeValue(ref StunRemaining);
            serializer.SerializeValue(ref AckSeq);
            serializer.SerializeValue(ref TeleportEpoch);
            serializer.SerializeValue(ref flags);
            Flags = (PlayerSnapshotFlags)flags;
        }
    }
}
