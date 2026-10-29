using UnityEngine;

namespace AlienFarms.Zones
{
    /// <summary>Shared helpers to read zone areas from BoxColliders and test planar containment.</summary>
    public static class ZoneBounds
    {
        /// <summary>
        /// World-space bounds of <paramref name="box"/>. Works while the collider or its object is disabled
        /// (collider.bounds is empty then). Without a box, returns an empty bounds at <paramref name="fallback"/>.
        /// </summary>
        public static Bounds Compute(BoxCollider box, Transform fallback)
        {
            if (box == null)
            {
                return new Bounds(fallback != null ? fallback.position : Vector3.zero, Vector3.zero);
            }

            if (box.enabled && box.gameObject.activeInHierarchy)
            {
                return box.bounds;
            }

            Transform t = box.transform;
            Vector3 center = t.TransformPoint(box.center);
            Vector3 size = Vector3.Scale(box.size, t.lossyScale);
            return new Bounds(center, new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z)));
        }

        /// <summary>True when X/Z of <paramref name="point"/> lie inside <paramref name="bounds"/> (edges inclusive, Y ignored).</summary>
        public static bool ContainsPlanar(Bounds bounds, Vector3 point)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            return point.x >= min.x && point.x <= max.x
                && point.z >= min.z && point.z <= max.z;
        }
    }
}
