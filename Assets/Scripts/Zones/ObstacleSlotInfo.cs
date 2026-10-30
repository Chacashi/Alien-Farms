using UnityEngine;

namespace AlienFarms.Zones
{
    /// <summary>Planner input for one obstacle slot. Plain value type, rebuilt in place every round.</summary>
    public struct ObstacleSlotInfo
    {
        /// <summary>Planar (X/Z) world position of the slot.</summary>
        public Vector2 Position;

        /// <summary>Index of the mirrored slot, or -1 for a single (unpaired) slot.</summary>
        public int Mirror;

        /// <summary>Bit i set = the slot owns an instance of variant i of the variant table.</summary>
        public uint VariantMask;

        /// <summary>Something (a player) occupies the slot this round; it must stay empty.</summary>
        public bool Blocked;

        public ObstacleSlotInfo(Vector2 position, int mirror, uint variantMask)
        {
            Position = position;
            Mirror = mirror;
            VariantMask = variantMask;
            Blocked = false;
        }
    }
}
