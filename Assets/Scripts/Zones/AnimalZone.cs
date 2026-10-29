using UnityEngine;

namespace AlienFarms.Zones
{
    /// <summary>
    /// Central area where animals live. Exposes the world-space bounds of its BoxCollider so animals
    /// can be confined to it. Animals receive plain <see cref="UnityEngine.Bounds"/> and never depend on this type.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class AnimalZone : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Box that defines the zone area (usually a trigger on this object).")]
        private BoxCollider _box;

        private Bounds _bounds;
        private bool _hasBounds;

        /// <summary>World-space bounds of the zone. Cached at runtime (the zone is static); computed fresh in edit mode.</summary>
        public Bounds Bounds
        {
            get
            {
                if (!Application.isPlaying)
                {
                    return ComputeBounds();
                }

                // Lazy so other components can read it from their Awake regardless of execution order.
                if (!_hasBounds)
                {
                    _bounds = ComputeBounds();
                    _hasBounds = true;
                }

                return _bounds;
            }
        }

        private void Awake()
        {
            if (!_hasBounds)
            {
                _bounds = ComputeBounds();
                _hasBounds = true;
            }
        }

        public bool Contains(Vector3 position)
        {
            Bounds bounds = Bounds;
            return position.x >= bounds.min.x && position.x <= bounds.max.x
                && position.z >= bounds.min.z && position.z <= bounds.max.z;
        }

        /// <summary>Clamps X/Z of <paramref name="position"/> inside the zone shrunk by <paramref name="margin"/>.</summary>
        public Vector3 ClampInside(Vector3 position, float margin)
        {
            Bounds bounds = Bounds;
            Vector3 center = bounds.center;
            float halfX = Mathf.Max(0f, bounds.extents.x - margin);
            float halfZ = Mathf.Max(0f, bounds.extents.z - margin);
            position.x = Mathf.Clamp(position.x, center.x - halfX, center.x + halfX);
            position.z = Mathf.Clamp(position.z, center.z - halfZ, center.z + halfZ);
            return position;
        }

        private Bounds ComputeBounds()
        {
            if (_box == null)
            {
                TryGetComponent(out _box);
            }

            if (_box == null)
            {
                return new Bounds(transform.position, Vector3.zero);
            }

            // collider.bounds is empty while the collider/object is disabled; fall back to the box definition.
            if (_box.enabled && _box.gameObject.activeInHierarchy)
            {
                return _box.bounds;
            }

            Vector3 center = _box.transform.TransformPoint(_box.center);
            Vector3 size = Vector3.Scale(_box.size, _box.transform.lossyScale);
            return new Bounds(center, new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z)));
        }

        private void OnValidate()
        {
            if (_box == null)
            {
                TryGetComponent(out _box);
            }
        }
    }
}
