namespace AlienFarms.Zones
{
    /// <summary>Planner output for one slot: chosen table variant (-1 = empty) and yaw in degrees.</summary>
    public struct ObstaclePlacement
    {
        public const int None = -1;

        public int Variant;
        public float Yaw;

        public bool IsActive => Variant >= 0;

        public void Clear()
        {
            Variant = None;
            Yaw = 0f;
        }
    }
}
