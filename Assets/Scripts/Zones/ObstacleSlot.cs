using UnityEngine;

namespace AlienFarms.Zones
{
    /// <summary>
    /// A scene-placed spot that may hold one obstacle per round. All its variants are pre-instantiated children,
    /// inactive by default; <see cref="Apply"/> enables at most one and sets its yaw. Positions are never changed by code.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ObstacleSlot : MonoBehaviour
    {
        // Gizmo circle resolution.
        private const int GizmoSegments = 24;

        // "Unknown" so the first Apply sets every child explicitly.
        private const int UnknownVariant = -2;

        [Header("Layout")]
        [SerializeField, Tooltip("Mirrored slot across the center (x -> -x); must point back to this slot. Empty = single slot.")]
        private ObstacleSlot _mirror;

        [SerializeField, Min(0f), Tooltip("A player within this radius blocks the slot for the round (m).")]
        private float _clearanceRadius = 2.5f;

        [Header("References")]
        [SerializeField, Tooltip("Pre-instantiated variant children (inactive in the editor).")]
        private ObstacleVariant[] _variants = new ObstacleVariant[0];

        private int _activeVariant = UnknownVariant;

        public Vector2 PlanarPosition
        {
            get
            {
                Vector3 position = transform.position;
                return new Vector2(position.x, position.z);
            }
        }

        public float ClearanceRadius => _clearanceRadius;
        public ObstacleSlot Mirror => _mirror;
        public int VariantCount => _variants != null ? _variants.Length : 0;

        /// <summary>Local index of the enabled variant, or -1 when empty.</summary>
        public int ActiveVariant => _activeVariant < 0 ? -1 : _activeVariant;

        public ObstacleVariant GetVariant(int index) => _variants[index];

        /// <summary>
        /// Enables the child at <paramref name="localVariantIndex"/> (-1 = all off) and sets its local yaw.
        /// Children are only toggled when the variant changes.
        /// </summary>
        public void Apply(int localVariantIndex, float yaw)
        {
            int count = VariantCount;
            if (localVariantIndex >= count)
            {
                localVariantIndex = -1;
            }

            if (localVariantIndex != _activeVariant)
            {
                for (int i = 0; i < count; i++)
                {
                    ObstacleVariant variant = _variants[i];
                    if (variant == null)
                    {
                        continue;
                    }

                    bool active = i == localVariantIndex;
                    if (variant.gameObject.activeSelf != active)
                    {
                        variant.gameObject.SetActive(active);
                    }
                }

                _activeVariant = localVariantIndex;
            }

            if (localVariantIndex >= 0 && _variants[localVariantIndex] != null)
            {
                _variants[localVariantIndex].transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            }
        }

        private void OnValidate()
        {
            if (_mirror == this)
            {
                Debug.LogWarning($"[{nameof(ObstacleSlot)}] '{name}' mirrors itself; leave Mirror empty for a single slot.", this);
            }
            else if (_mirror != null && _mirror.Mirror != this)
            {
                Debug.LogWarning($"[{nameof(ObstacleSlot)}] '{name}' mirrors '{_mirror.name}' but not the other way round; both are treated as singles.", this);
            }

            if (_variants == null || _variants.Length == 0)
            {
                Debug.LogWarning($"[{nameof(ObstacleSlot)}] '{name}' has no variants.", this);
                return;
            }

            for (int i = 0; i < _variants.Length; i++)
            {
                ObstacleVariant variant = _variants[i];
                if (variant == null)
                {
                    Debug.LogWarning($"[{nameof(ObstacleSlot)}] '{name}' variant {i} is empty.", this);
                    continue;
                }

                for (int j = 0; j < i; j++)
                {
                    if (_variants[j] == variant)
                    {
                        Debug.LogWarning($"[{nameof(ObstacleSlot)}] '{name}' lists '{variant.name}' twice.", this);
                        break;
                    }
                }

                if (!Application.isPlaying && variant.gameObject.activeSelf)
                {
                    Debug.LogWarning($"[{nameof(ObstacleSlot)}] Variant '{variant.name}' of '{name}' should be inactive in the editor.", this);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Vector3 center = transform.position;
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.8f);
            Vector3 previous = center + new Vector3(_clearanceRadius, 0f, 0f);
            for (int i = 1; i <= GizmoSegments; i++)
            {
                float angle = i * (2f * Mathf.PI / GizmoSegments);
                Vector3 next = center + new Vector3(Mathf.Cos(angle) * _clearanceRadius, 0f, Mathf.Sin(angle) * _clearanceRadius);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }

            if (_mirror != null)
            {
                Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.6f);
                Gizmos.DrawLine(center, _mirror.transform.position);
            }
        }
    }
}
