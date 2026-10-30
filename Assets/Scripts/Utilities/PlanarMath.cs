using UnityEngine;

namespace AlienFarms.Utilities
{
    /// <summary>Pure geometric helpers on the horizontal (XZ) plane.</summary>
    public static class PlanarMath
    {
        private const float Epsilon = 0.0001f;

        /// <summary>
        /// Planar unit direction from <paramref name="center"/> to <paramref name="target"/>. When both coincide on the
        /// plane, the planar <paramref name="fallback"/> is used, and <see cref="Vector3.forward"/> if that is zero too.
        /// </summary>
        public static Vector3 KnockbackDirection(Vector3 center, Vector3 target, Vector3 fallback)
        {
            Vector3 direction = new Vector3(target.x - center.x, 0f, target.z - center.z);
            if (direction.sqrMagnitude > Epsilon)
            {
                return direction.normalized;
            }

            direction = new Vector3(fallback.x, 0f, fallback.z);
            return direction.sqrMagnitude > Epsilon ? direction.normalized : Vector3.forward;
        }
    }
}
