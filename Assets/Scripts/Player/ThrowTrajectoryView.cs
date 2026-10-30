using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Utilities;
using UnityEngine;

namespace AlienFarms.Player
{
    /// <summary>
    /// Draws the throw arc while aiming. Simulates with the same integrator and step as PhysX
    /// (<see cref="BallisticMath.Step"/>, Time.fixedDeltaTime, Physics.gravity), so the preview matches the real flight
    /// of an undamped body. The arc follows the BASE of the carried body and sweeps a sphere of its size resting on
    /// that base, so it stops where the body touches Ground/Environment: the same spot where the animal lands (its
    /// horizontal momentum is removed on landing). Runs after the camera's LateUpdate to use this frame's pitch and yaw.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class ThrowTrajectoryView : MonoBehaviour
    {
        // Fallback buffer size when no throw settings are available at Awake.
        private const int DefaultCapacity = 64;

        // Smallest sweep radius, so a body without a collider size still gets a usable cast (m).
        private const float MinSweepRadius = 0.05f;

        private const float MinSegmentLength = 0.0001f;

        [Header("References")]
        [SerializeField, Tooltip("Aim of this player (launch origin, velocity and settings).")]
        private PlayerAim _aim;

        [SerializeField, Tooltip("World-space LineRenderer on this object; disabled while not aiming.")]
        private LineRenderer _line;

        [SerializeField, Tooltip("Optional marker placed at the impact point (no collider).")]
        private Transform _impactMarker;

        [Header("Config")]
        [SerializeField, Tooltip("Layers that stop the arc (Ground, Environment). Never ZoneBarrier: thrown animals pass it.")]
        private LayerMask _collisionMask;

        private Vector3[] _points;

        private void Awake()
        {
            if (_line == null)
            {
                TryGetComponent(out _line);
            }

            ThrowSettings settings = _aim != null ? _aim.ActiveThrowSettings : null;
            _points = new Vector3[settings != null ? RequiredPoints(settings) : DefaultCapacity];
            Hide();
        }

        private void OnDisable()
        {
            Hide();
        }

        private void LateUpdate()
        {
            if (_aim == null || _line == null || !_aim.TryGetLaunch(out Vector3 origin, out Vector3 velocity))
            {
                Hide();
                return;
            }

            ThrowSettings settings = _aim.ActiveThrowSettings;
            int maxPoints = RequiredPoints(settings);
            if (maxPoints > _points.Length)
            {
                // Only when a type with a longer preview is carried for the first time; never every frame.
                _points = new Vector3[maxPoints];
            }

            float radius = SweepRadius(_aim.HeldThrowable);
            Vector3 lift = Vector3.up * radius;
            float dt = Time.fixedDeltaTime;
            int stepsPerPoint = settings.PreviewStepsPerPoint;
            Vector3 gravity = Physics.gravity;

            Vector3 position = origin;
            _points[0] = position;
            int count = 1;
            bool hasImpact = false;
            RaycastHit impact = default;

            while (count < maxPoints)
            {
                Vector3 previous = position;
                for (int i = 0; i < stepsPerPoint; i++)
                {
                    BallisticMath.Step(ref position, ref velocity, gravity, dt);
                }

                Vector3 segment = position - previous;
                float length = segment.magnitude;
                if (length > MinSegmentLength
                    && Physics.SphereCast(
                        previous + lift, radius, segment / length, out impact, length, _collisionMask, QueryTriggerInteraction.Ignore))
                {
                    // Base of the body at the moment its sphere touches.
                    _points[count++] = previous + segment * (impact.distance / length);
                    hasImpact = true;
                    break;
                }

                _points[count++] = position;
            }

            // SetPosition loop instead of SetPositions: the buffer may be longer than the drawn arc.
            _line.positionCount = count;
            for (int i = 0; i < count; i++)
            {
                _line.SetPosition(i, _points[i]);
            }

            if (!_line.enabled)
            {
                _line.enabled = true;
            }

            UpdateMarker(hasImpact, impact);
        }

        /// <summary>Sphere resting on the body's base: as wide as the body, but never taller than it.</summary>
        private static float SweepRadius(IThrowable throwable)
        {
            if (throwable == null)
            {
                return MinSweepRadius;
            }

            return Mathf.Max(MinSweepRadius, Mathf.Min(throwable.CollisionRadius, throwable.HalfHeight));
        }

        private void UpdateMarker(bool hasImpact, RaycastHit impact)
        {
            if (_impactMarker == null)
            {
                return;
            }

            if (hasImpact)
            {
                _impactMarker.SetPositionAndRotation(impact.point, Quaternion.FromToRotation(Vector3.up, impact.normal));
            }

            if (_impactMarker.gameObject.activeSelf != hasImpact)
            {
                _impactMarker.gameObject.SetActive(hasImpact);
            }
        }

        private void Hide()
        {
            if (_line != null && _line.enabled)
            {
                _line.enabled = false;
                _line.positionCount = 0;
            }

            if (_impactMarker != null && _impactMarker.gameObject.activeSelf)
            {
                _impactMarker.gameObject.SetActive(false);
            }
        }

        private static int RequiredPoints(ThrowSettings settings) =>
            BallisticMath.PreviewPointCount(settings.PreviewMaxTime, Time.fixedDeltaTime, settings.PreviewStepsPerPoint);

        // Editor-time default only; runtime never resolves layers by name.
        private void Reset()
        {
            _collisionMask = LayerMask.GetMask(LayerNames.Ground, LayerNames.Environment);
            TryGetComponent(out _line);
        }

        private void OnValidate()
        {
            if (_line == null)
            {
                TryGetComponent(out _line);
            }

            if (_aim == null)
            {
                Debug.LogWarning($"[{nameof(ThrowTrajectoryView)}] Missing PlayerAim on '{name}'.", this);
            }

            if (_collisionMask.value == 0)
            {
                Debug.LogWarning($"[{nameof(ThrowTrajectoryView)}] Collision mask on '{name}' is empty; the arc will never stop.", this);
            }

            int forbidden = LayerMask.GetMask(LayerNames.ZoneBarrier, LayerNames.Player, LayerNames.Animal);
            if ((_collisionMask.value & forbidden) != 0)
            {
                Debug.LogWarning($"[{nameof(ThrowTrajectoryView)}] Collision mask on '{name}' includes ZoneBarrier, Player or Animal; the arc would stop on the barrier (which thrown animals pass) or on the thrower itself.", this);
            }

            if (_line != null && !_line.useWorldSpace)
            {
                Debug.LogWarning($"[{nameof(ThrowTrajectoryView)}] LineRenderer on '{name}' must use world space.", this);
            }
        }
    }
}
