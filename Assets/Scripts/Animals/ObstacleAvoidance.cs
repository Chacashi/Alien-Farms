using UnityEngine;

namespace AlienFarms.Animals
{
    /// <summary>
    /// Pure planar obstacle avoidance: given the desired direction and a probe hit in front of the animal, slides
    /// along the obstacle tangent and adds a small repulsion that grows as the hit gets closer. Stateless apart from
    /// the <see cref="AvoidanceSide"/> passed by ref; allocation-free and EditMode-testable.
    /// </summary>
    public static class ObstacleAvoidance
    {
        /// <summary>Repulsion along the hit normal at zero distance, as a fraction of the desired magnitude.</summary>
        public const float RepelWeight = 0.5f;

        /// <summary>A tangent shorter than this fraction of the desired magnitude means "head-on" (a side is picked).</summary>
        public const float HeadOnRatio = 0.2f;

        /// <summary>A remembered side is only forced while the tangent stays shorter than this fraction (hysteresis).</summary>
        public const float HoldRatio = 0.5f;

        private const float Epsilon = 0.0001f;

        /// <summary>
        /// Returns the planar steering for <paramref name="desired"/> (Y ignored) after a probe hit on obstacle
        /// <paramref name="obstacleId"/> with normal <paramref name="hitNormal"/> at <paramref name="hitDistance"/> of
        /// <paramref name="probeDistance"/>.
        /// <list type="bullet">
        /// <item>Moving away from the obstacle: unchanged.</item>
        /// <item>Oblique: slides along the natural tangent.</item>
        /// <item>Head-on: turns perpendicular, to the side of <paramref name="sideHint"/>, else the stored side, else +1,
        /// and remembers that side for <paramref name="holdTime"/> (only on entry, never renewed per call).</item>
        /// <item>While the memory holds for this same obstacle and the tangent is still short (&lt; <see cref="HoldRatio"/>),
        /// the remembered side is forced. A different obstacle or a clearly oblique hit uses its natural tangent.</item>
        /// <item>A non-zero <paramref name="sideHint"/> (away from a threat) is never contradicted: a tangent pointing
        /// against it is replaced by the hint's side.</item>
        /// </list>
        /// The result never points into the obstacle, never exceeds |desired| and has Y = 0.
        /// </summary>
        public static Vector3 Avoid(
            Vector3 desired,
            Vector3 hitNormal,
            float hitDistance,
            float probeDistance,
            int obstacleId,
            Vector3 sideHint,
            float now,
            float holdTime,
            ref AvoidanceSide memory)
        {
            desired.y = 0f;
            float magnitude = desired.magnitude;
            if (magnitude < Epsilon || !TryPlanarNormal(hitNormal, out Vector3 normal))
            {
                return desired;
            }

            float into = Vector3.Dot(desired, normal);
            if (into >= 0f)
            {
                return desired;
            }

            Vector3 tangent = desired - into * normal;
            float tangentMagnitude = tangent.magnitude;
            Vector3 perpendicular = Perpendicular(normal);

            int side;
            if (memory.Holds(now, obstacleId) && tangentMagnitude < HoldRatio * magnitude)
            {
                side = memory.Side;
            }
            else if (tangentMagnitude < HeadOnRatio * magnitude)
            {
                side = SideFromHint(perpendicular, sideHint, memory.Side);
                memory.Begin(side, now + holdTime, obstacleId);
            }
            else
            {
                side = Vector3.Dot(tangent, perpendicular) >= 0f ? 1 : -1;
            }

            // Fleeing: never go around towards the threat.
            float hintScore = Vector3.Dot(perpendicular, new Vector3(sideHint.x, 0f, sideHint.z));
            if (side * hintScore < -Epsilon)
            {
                side = -side;
                memory.Begin(side, now + holdTime, obstacleId);
            }

            // In the plane the tangent is always parallel to the perpendicular: the side alone defines the slide.
            return Compose(perpendicular * side, normal, hitDistance, probeDistance, magnitude);
        }

        /// <summary>
        /// Goes around the obstacle on the given <paramref name="side"/> (perpendicular to the planar normal) at the
        /// magnitude of <paramref name="desired"/>, with the same distance-based repulsion as <see cref="Avoid"/>.
        /// </summary>
        public static Vector3 Detour(Vector3 desired, Vector3 hitNormal, float hitDistance, float probeDistance, int side)
        {
            desired.y = 0f;
            float magnitude = desired.magnitude;
            if (magnitude < Epsilon || !TryPlanarNormal(hitNormal, out Vector3 normal))
            {
                return desired;
            }

            return Compose(Perpendicular(normal) * (side >= 0 ? 1f : -1f), normal, hitDistance, probeDistance, magnitude);
        }

        /// <summary>+1 or -1: which side of the obstacle (along its tangent) <paramref name="direction"/> goes to.</summary>
        public static int SideOf(Vector3 direction, Vector3 hitNormal)
        {
            if (!TryPlanarNormal(hitNormal, out Vector3 normal))
            {
                return 1;
            }

            return Vector3.Dot(direction, Perpendicular(normal)) >= 0f ? 1 : -1;
        }

        private static Vector3 Compose(Vector3 slideDirection, Vector3 normal, float hitDistance, float probeDistance, float magnitude)
        {
            float closeness = probeDistance > Epsilon ? 1f - Mathf.Clamp01(hitDistance / probeDistance) : 1f;
            Vector3 result = slideDirection * magnitude + normal * (RepelWeight * closeness * magnitude);
            result = Vector3.ClampMagnitude(result, magnitude);
            result.y = 0f;
            return result;
        }

        private static bool TryPlanarNormal(Vector3 hitNormal, out Vector3 normal)
        {
            normal = new Vector3(hitNormal.x, 0f, hitNormal.z);
            if (normal.sqrMagnitude < Epsilon * Epsilon)
            {
                return false;
            }

            normal.Normalize();
            return true;
        }

        private static Vector3 Perpendicular(Vector3 normal) => new Vector3(normal.z, 0f, -normal.x);

        private static int SideFromHint(Vector3 perpendicular, Vector3 sideHint, int storedSide)
        {
            float score = perpendicular.x * sideHint.x + perpendicular.z * sideHint.z;
            if (score > Epsilon)
            {
                return 1;
            }

            if (score < -Epsilon)
            {
                return -1;
            }

            return storedSide != 0 ? storedSide : 1;
        }
    }
}
