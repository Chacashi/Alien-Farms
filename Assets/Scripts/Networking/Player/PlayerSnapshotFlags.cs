using System;

namespace AlienFarms.Networking.Player
{
    /// <summary>Bit flags of a <see cref="PlayerSnapshot"/> (teleports travel as <see cref="PlayerSnapshot.TeleportEpoch"/>).</summary>
    [Flags]
    public enum PlayerSnapshotFlags : byte
    {
        None = 0,

        Sprinting = 1 << 0,

        Aiming = 1 << 1
    }
}
