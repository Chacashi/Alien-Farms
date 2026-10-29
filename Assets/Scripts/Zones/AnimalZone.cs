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

        public bool Contains(Vector3 position) => ZoneBounds.ContainsPlanar(Bounds, position);

        private Bounds ComputeBounds()
        {
            if (_box == null)
            {
                TryGetComponent(out _box);
            }

            return ZoneBounds.Compute(_box, transform);
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
