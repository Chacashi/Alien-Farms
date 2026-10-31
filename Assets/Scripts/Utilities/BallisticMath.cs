using UnityEngine;

namespace AlienFarms.Utilities
{
    /// <summary>
    /// Pure ballistic helpers shared by the throw and its preview. <see cref="Step"/> integrates exactly like PhysX
    /// (semi-implicit Euler), so a preview stepped with Time.fixedDeltaTime matches an undamped Rigidbody.
    /// </summary>
    public static class BallisticMath
    {
        private const float Epsilon = 0.0001f;

        /// <summary>
        /// Maps a camera pitch (positive looks down) to a launch elevation: looking up from
        /// <paramref name="referencePitch"/> raises the arc by <paramref name="influence"/> degrees per degree.
        /// </summary>
        public static float ElevationFromPitch(
            float cameraPitch, float referencePitch, float baseElevation, float influence, float min, float max)
        {
            return Mathf.Clamp(baseElevation + (referencePitch - cameraPitch) * influence, min, max);
        }

        /// <summary>
        /// Launch velocity of magnitude <paramref name="speed"/> along <paramref name="planarForward"/> (Y ignored)
        /// tilted up by <paramref name="elevationDeg"/>. A degenerate forward falls back to world forward.
        /// </summary>
        public static Vector3 LaunchVelocity(Vector3 planarForward, float elevationDeg, float speed)
        {
            planarForward.y = 0f;
            if (planarForward.sqrMagnitude < Epsilon)
            {
                planarForward = Vector3.forward;
            }

            planarForward.Normalize();
            float radians = elevationDeg * Mathf.Deg2Rad;
            Vector3 direction = planarForward * Mathf.Cos(radians) + Vector3.up * Mathf.Sin(radians);
            return direction * speed;
        }

        /// <summary>
        /// Launch speed factor that scales the flight distance by <paramref name="distanceMultiplier"/>: range grows with
        /// speed squared, so the factor is its square root (exact on flat ground, close when launched from a height).
        /// Non-positive multipliers return 0.
        /// </summary>
        public static float SpeedScaleForDistance(float distanceMultiplier)
        {
            return distanceMultiplier > 0f ? Mathf.Sqrt(distanceMultiplier) : 0f;
        }

        /// <summary>One semi-implicit Euler step: velocity first, then position with the new velocity.</summary>
        public static void Step(ref Vector3 position, ref Vector3 velocity, Vector3 gravity, float dt)
        {
            velocity += gravity * dt;
            position += velocity * dt;
        }

        /// <summary>
        /// Points needed to draw <paramref name="maxTime"/> seconds of flight with one point every
        /// <paramref name="stepsPerPoint"/> steps of <paramref name="dt"/>, including the origin. At least 2.
        /// </summary>
        public static int PreviewPointCount(float maxTime, float dt, int stepsPerPoint)
        {
            float pointInterval = Mathf.Max(dt, Epsilon) * Mathf.Max(1, stepsPerPoint);
            return Mathf.Max(2, Mathf.CeilToInt(Mathf.Max(0f, maxTime) / pointInterval) + 1);
        }
    }
}
