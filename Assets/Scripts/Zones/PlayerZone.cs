using AlienFarms.Core;
using UnityEngine;

namespace AlienFarms.Zones
{
    /// <summary>
    /// A player's pen. Its BoxCollider defines the capture area; a child barrier (layer ZoneBarrier) keeps the rival
    /// and captured animals out/in, while the owner is allowed through via <see cref="GrantAccess"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class PlayerZone : MonoBehaviour
    {
        private static readonly Color s_player1Color = new Color(0.23f, 0.48f, 1f, 0.35f);
        private static readonly Color s_player2Color = new Color(1f, 0.25f, 0.25f, 0.35f);

        [Header("Config")]
        [SerializeField, Tooltip("Player that scores by releasing animals inside this zone.")]
        private PlayerId _owner = PlayerId.Player1;

        [Header("References")]
        [SerializeField, Tooltip("Box that defines the capture area (trigger on this object).")]
        private BoxCollider _area;

        [SerializeField, Tooltip("Solid collider (child 'Barrier', layer ZoneBarrier, not a trigger) at the zone entrance.")]
        private Collider _barrier;

        private Bounds _bounds;
        private bool _hasBounds;

        public PlayerId Owner => _owner;

        /// <summary>World-space bounds of the area. Cached at runtime (the zone is static); computed fresh in edit mode.</summary>
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

        /// <summary>True when X/Z of <paramref name="position"/> lie inside the zone (edges inclusive).</summary>
        public bool Contains(Vector3 position) => ZoneBounds.ContainsPlanar(Bounds, position);

        /// <summary>
        /// Lets <paramref name="playerCollider"/> pass through the barrier. Both colliders must stay enabled:
        /// disabling either one may reset the ignore pair, so call again if that ever happens.
        /// </summary>
        public void GrantAccess(Collider playerCollider)
        {
            if (playerCollider == null || _barrier == null)
            {
                return;
            }

            Physics.IgnoreCollision(_barrier, playerCollider, true);
        }

        private Bounds ComputeBounds()
        {
            if (_area == null)
            {
                TryGetComponent(out _area);
            }

            return ZoneBounds.Compute(_area, transform);
        }

        private void OnValidate()
        {
            if (_area == null)
            {
                TryGetComponent(out _area);
            }

            if (_owner == PlayerId.None)
            {
                Debug.LogWarning($"[{nameof(PlayerZone)}] Owner is None on '{name}'.", this);
            }

            if (_barrier == null)
            {
                Debug.LogWarning($"[{nameof(PlayerZone)}] Missing barrier collider on '{name}'.", this);
                return;
            }

            if (_barrier.isTrigger)
            {
                Debug.LogWarning($"[{nameof(PlayerZone)}] Barrier '{_barrier.name}' must not be a trigger.", this);
            }

            int barrierLayer = LayerMask.NameToLayer(LayerNames.ZoneBarrier);
            if (_barrier.gameObject.layer != barrierLayer)
            {
                Debug.LogWarning($"[{nameof(PlayerZone)}] Barrier '{_barrier.name}' should be on layer '{LayerNames.ZoneBarrier}'.", this);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Bounds bounds = ComputeBounds();
            Gizmos.color = _owner == PlayerId.Player2 ? s_player2Color : s_player1Color;
            Gizmos.DrawCube(bounds.center, bounds.size);
        }
    }
}
