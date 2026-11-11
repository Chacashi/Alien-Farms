using AlienFarms.Networking.Player;
using UnityEngine;

namespace AlienFarms.Networking.Prediction
{
    /// <summary>Pose sampled by <see cref="SnapshotInterpolator"/> at a render time.</summary>
    public struct InterpolatedPose
    {
        public Vector3 Position;
        public Vector3 Velocity;

        /// <summary>Planar facing (deg around Y, 0 = +Z).</summary>
        public float FacingYaw;
        public float StunRemaining;

        /// <summary>Flags of the older snapshot of the pair (the only one when clamped).</summary>
        public PlayerSnapshotFlags Flags;

        /// <summary>True when the render time was outside the buffered range and the pose was clamped to an end.</summary>
        public bool IsClamped;

        public Vector3 FacingDirection
        {
            get
            {
                float radians = FacingYaw * Mathf.Deg2Rad;
                return new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
            }
        }
    }
}
