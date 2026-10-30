namespace AlienFarms.Animals
{
    /// <summary>
    /// Hysteresis state for <see cref="ObstacleAvoidance.Avoid"/>: once an animal picks a side to go around an
    /// obstacle, it keeps that side for that same obstacle until <see cref="Until"/>. The side survives expiry (it is
    /// reused when no hint is available) until <see cref="Clear"/>.
    /// Plain value type, passed by ref; the default value is "inactive, no side".
    /// </summary>
    public struct AvoidanceSide
    {
        /// <summary>+1 or -1 (along the obstacle tangent), or 0 when none was chosen yet.</summary>
        public int Side { get; private set; }

        /// <summary>Time until which the side is held.</summary>
        public float Until { get; private set; }

        /// <summary>Id of the obstacle (collider) the side was chosen for.</summary>
        public int ObstacleId { get; private set; }

        public bool IsActive(float now) => now < Until && Side != 0;

        /// <summary>Active and chosen for <paramref name="obstacleId"/>.</summary>
        public bool Holds(float now, int obstacleId) => IsActive(now) && ObstacleId == obstacleId;

        public void Begin(int side, float until, int obstacleId)
        {
            Side = side >= 0 ? 1 : -1;
            Until = until;
            ObstacleId = obstacleId;
        }

        public void Clear()
        {
            Side = 0;
            Until = 0f;
            ObstacleId = 0;
        }
    }
}
