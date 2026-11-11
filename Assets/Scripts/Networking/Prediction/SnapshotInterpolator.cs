using AlienFarms.Networking.Player;
using UnityEngine;

namespace AlienFarms.Networking.Prediction
{
    /// <summary>
    /// Samples a <see cref="SnapshotBuffer"/> at a render time (server clock): linear between the two snapshots around it,
    /// clamped to the oldest / newest outside the range (no extrapolation). Teleports never blend: the buffer only holds
    /// snapshots of one teleport epoch.
    /// </summary>
    public static class SnapshotInterpolator
    {
        /// <summary>False only when the buffer is empty.</summary>
        public static bool TrySample(SnapshotBuffer buffer, double renderTime, out InterpolatedPose pose)
        {
            pose = default;
            if (buffer == null || buffer.Count == 0)
            {
                return false;
            }

            PlayerSnapshot oldest = buffer[0];
            if (buffer.Count == 1 || renderTime <= oldest.ServerTime)
            {
                pose = FromSnapshot(oldest, true);
                return true;
            }

            PlayerSnapshot newest = buffer[buffer.Count - 1];
            if (renderTime >= newest.ServerTime)
            {
                pose = FromSnapshot(newest, true);
                return true;
            }

            for (int i = 0; i < buffer.Count - 1; i++)
            {
                PlayerSnapshot from = buffer[i];
                PlayerSnapshot to = buffer[i + 1];
                if (renderTime >= to.ServerTime)
                {
                    continue;
                }

                double span = to.ServerTime - from.ServerTime;
                float t = span > 0d ? (float)((renderTime - from.ServerTime) / span) : 1f;
                pose = new InterpolatedPose
                {
                    Position = Vector3.LerpUnclamped(from.Position, to.Position, t),
                    Velocity = Vector3.LerpUnclamped(from.Velocity, to.Velocity, t),
                    FacingYaw = Mathf.LerpAngle(from.FacingYaw, to.FacingYaw, t),
                    StunRemaining = Mathf.Lerp(from.StunRemaining, to.StunRemaining, t),
                    Flags = from.Flags,
                    IsClamped = false
                };
                return true;
            }

            pose = FromSnapshot(newest, true);
            return true;
        }

        private static InterpolatedPose FromSnapshot(in PlayerSnapshot snapshot, bool clamped)
        {
            return new InterpolatedPose
            {
                Position = snapshot.Position,
                Velocity = snapshot.Velocity,
                FacingYaw = snapshot.FacingYaw,
                StunRemaining = snapshot.StunRemaining,
                Flags = snapshot.Flags,
                IsClamped = clamped
            };
        }
    }
}
