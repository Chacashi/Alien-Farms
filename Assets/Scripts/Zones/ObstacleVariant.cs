using AlienFarms.Core;
using AlienFarms.Data;
using UnityEngine;

namespace AlienFarms.Zones
{
    /// <summary>
    /// Root of one obstacle variant prefab (pivot at ground level). Pre-instantiated, inactive, under an
    /// <see cref="ObstacleSlot"/>; the slot only toggles it and sets its yaw. Its geometry lives on the Environment layer.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ObstacleVariant : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Variant type (must be listed in the layout settings' variant table).")]
        private ObstacleVariantData _data;

        public ObstacleVariantData Data => _data;

        private void OnValidate()
        {
            if (_data == null)
            {
                Debug.LogWarning($"[{nameof(ObstacleVariant)}] Missing ObstacleVariantData on '{name}'.", this);
            }

            int environment = LayerMask.NameToLayer(LayerNames.Environment);
            if (environment >= 0 && gameObject.layer != environment)
            {
                Debug.LogWarning($"[{nameof(ObstacleVariant)}] '{name}' should be on layer '{LayerNames.Environment}' (whole hierarchy).", this);
            }

            // Toggled and rotated at runtime: static batching would freeze it.
            if (gameObject.isStatic)
            {
                Debug.LogWarning($"[{nameof(ObstacleVariant)}] '{name}' must not be marked static.", this);
            }
        }
    }
}
