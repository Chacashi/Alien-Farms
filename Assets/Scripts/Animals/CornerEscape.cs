using UnityEngine;

namespace AlienFarms.Animals
{
    /// <summary>
    /// Hysteresis state for <see cref="AnimalSteering.SteerFlee"/>: once a cornered animal picks an edge to
    /// run along, it keeps that axis direction at least until <see cref="Until"/> and, while still cornered,
    /// until the axis gets blocked.
    /// Plain value type, passed by ref; the default value is "inactive".
    /// </summary>
    public struct CornerEscape
    {
        /// <summary>Unconfined axis direction being followed (unit X or Z), or zero when inactive.</summary>
        public Vector3 Direction { get; private set; }

        /// <summary>Time until which the escape direction is held.</summary>
        public float Until { get; private set; }

        public bool IsActive(float now) => now < Until && Direction.sqrMagnitude > 0f;

        public void Begin(Vector3 direction, float until)
        {
            Direction = direction;
            Until = until;
        }

        public void Clear()
        {
            Direction = Vector3.zero;
            Until = 0f;
        }
    }
}
