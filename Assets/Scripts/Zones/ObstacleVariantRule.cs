namespace AlienFarms.Zones
{
    /// <summary>Planner rule for one entry of the variant table: relative weight and allowed yaws (degrees).</summary>
    public readonly struct ObstacleVariantRule
    {
        public readonly int Weight;

        /// <summary>Allowed yaws; empty means 0. Owned by the rule, never modified after construction.</summary>
        public readonly float[] Yaws;

        /// <summary>
        /// The shape equals its own mirror image, so a mirrored slot (yaw -θ) looks like a true reflection.
        /// Chiral variants are only placed on single slots.
        /// </summary>
        public readonly bool IsMirrorSymmetric;

        public ObstacleVariantRule(int weight, float[] yaws, bool isMirrorSymmetric = true)
        {
            Weight = weight;
            Yaws = yaws;
            IsMirrorSymmetric = isMirrorSymmetric;
        }
    }
}
