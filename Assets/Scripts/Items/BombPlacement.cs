using AlienFarms.Utilities;
using UnityEngine;

namespace AlienFarms.Items
{
    /// <summary>Pure placement helper for bomb spawns.</summary>
    public static class BombPlacement
    {
        /// <summary>
        /// Uniform random point on the XZ plane of <paramref name="area"/> shrunk by <paramref name="edgeMargin"/> on
        /// every side (an axis narrower than twice the margin collapses to its center). Y is the area center height.
        /// </summary>
        public static Vector3 RandomPointInArea(Bounds area, float edgeMargin, IRandomSource rng)
        {
            Vector3 center = area.center;
            Vector3 extents = area.extents;
            float margin = Mathf.Max(0f, edgeMargin);
            float halfX = Mathf.Max(0f, extents.x - margin);
            float halfZ = Mathf.Max(0f, extents.z - margin);

            float x = Mathf.Lerp(center.x - halfX, center.x + halfX, rng.Value01());
            float z = Mathf.Lerp(center.z - halfZ, center.z + halfZ, rng.Value01());
            return new Vector3(x, center.y, z);
        }
    }
}
