using UnityEngine;

namespace AlienFarms.Animals
{
    /// <summary>
    /// Pure planar (X/Z) steering helpers that keep animals inside a <see cref="Bounds"/> shrunk by a margin
    /// (the "inner rectangle"). Stateless, allocation-free and testable in EditMode.
    /// </summary>
    public static class AnimalSteering
    {
        /// <summary>Fixed inward component applied on an axis where the position is outside the inner rectangle.</summary>
        public const float InwardPush = 0.5f;

        /// <summary>Confinement that keeps less than this fraction of the desired direction means "cornered".</summary>
        public const float CorneredRatio = 0.5f;

        private const float CorneredRatioSqr = CorneredRatio * CorneredRatio;
        private const float Epsilon = 0.0001f;

        private static readonly Vector3[] s_axisDirections =
        {
            Vector3.right, Vector3.left, Vector3.forward, Vector3.back
        };

        /// <summary>
        /// Confines a planar <paramref name="direction"/> (magnitude &lt;= 1 expected) per axis:
        /// <list type="bullet">
        /// <item>Inside the inner rectangle, a component pointing outwards is attenuated so that
        /// <c>position + component * lookahead</c> never passes the edge; it reaches 0 at the edge.
        /// Components pointing inwards, or far enough from the edge, are unchanged.</item>
        /// <item>Outside (or exactly on) the inner rectangle on an axis, that component is replaced by a fixed
        /// inward push of <see cref="InwardPush"/>, independent of the input.</item>
        /// </list>
        /// <paramref name="lookahead"/> is the stopping distance at the current speed (speed² / (2·accel)).
        /// Y of the result is always 0; no component grows in magnitude except the fixed push.
        /// </summary>
        public static Vector3 ConfineDirection(Vector3 position, Vector3 direction, Bounds bounds, float margin, float lookahead)
        {
            GetInnerRect(bounds, margin, out float minX, out float maxX, out float minZ, out float maxZ);

            float x = ConfineAxis(position.x, direction.x, minX, maxX, lookahead);
            float z = ConfineAxis(position.z, direction.z, minZ, maxZ, lookahead);
            return new Vector3(x, 0f, z);
        }

        /// <summary>
        /// Fallback when the confined direction collapses (e.g. cornered): returns the unconfined unit axis
        /// (±X or ±Z, i.e. along an edge) that is still free and best moves away from the threat,
        /// or zero if every axis is blocked.
        /// </summary>
        public static Vector3 EscapeAlongEdge(Vector3 position, Vector3 awayFromThreat, Bounds bounds, float margin, float lookahead)
        {
            Vector3 best = Vector3.zero;
            float bestScore = float.MinValue;
            for (int i = 0; i < s_axisDirections.Length; i++)
            {
                Vector3 axis = s_axisDirections[i];
                if (!IsAxisFree(position, axis, bounds, margin, lookahead))
                {
                    continue;
                }

                float score = Vector3.Dot(axis, awayFromThreat);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = axis;
                }
            }

            return best;
        }

        /// <summary>
        /// Flee steering with corner handling. Confines <paramref name="fleeDirection"/>; if confinement removes
        /// at least half of its magnitude the animal is considered cornered and starts running along the free edge
        /// that best escapes the threat. Hysteresis via <paramref name="escape"/>: the chosen axis is held for at
        /// least <paramref name="holdTime"/> seconds, and afterwards kept (never reversed) while the animal is still
        /// cornered; it is dropped once the plain flee direction is no longer cornered, and re-picked only when the
        /// axis gets blocked by the far edge.
        /// Returns a planar confined direction (magnitude about 1 or less).
        /// </summary>
        public static Vector3 SteerFlee(
            Vector3 position,
            Vector3 fleeDirection,
            Vector3 awayFromThreat,
            Bounds bounds,
            float margin,
            float lookahead,
            float now,
            float holdTime,
            ref CornerEscape escape)
        {
            bool hasEscape = escape.Direction.sqrMagnitude > Epsilon;
            bool escapeAxisFree = hasEscape && IsAxisFree(position, escape.Direction, bounds, margin, lookahead);

            // Minimum hold: keep running along the chosen edge while that axis is free.
            if (escape.IsActive(now) && escapeAxisFree)
            {
                return ConfineDirection(position, escape.Direction, bounds, margin, lookahead);
            }

            fleeDirection.y = 0f;
            Vector3 confined = ConfineDirection(position, fleeDirection, bounds, margin, lookahead);
            float desiredSqr = fleeDirection.sqrMagnitude;
            bool cornered = desiredSqr >= Epsilon && confined.sqrMagnitude < CorneredRatioSqr * desiredSqr;
            if (!cornered)
            {
                escape.Clear();
                return confined;
            }

            // Still cornered after the hold: never flip to the opposite edge direction (that is what made the
            // corner an attractor); keep the current axis until it gets blocked by the far edge.
            if (escapeAxisFree)
            {
                escape.Begin(escape.Direction, now + holdTime);
                return ConfineDirection(position, escape.Direction, bounds, margin, lookahead);
            }

            Vector3 axis = EscapeAlongEdge(position, awayFromThreat, bounds, margin, lookahead);
            if (axis.sqrMagnitude < Epsilon)
            {
                return confined;
            }

            escape.Begin(axis, now + holdTime);
            return ConfineDirection(position, axis, bounds, margin, lookahead);
        }

        /// <summary>An axis is free when confining it keeps at least half of it pointing the same way.</summary>
        private static bool IsAxisFree(Vector3 position, Vector3 axis, Bounds bounds, float margin, float lookahead)
        {
            Vector3 confined = ConfineDirection(position, axis, bounds, margin, lookahead);
            return Vector3.Dot(confined, axis) >= CorneredRatio;
        }

        /// <summary>Random point within <paramref name="radius"/> of <paramref name="around"/>, clamped inside the inner rectangle.</summary>
        public static Vector3 RandomPointInside(Vector3 around, float radius, Bounds bounds, float margin)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            Vector3 point = new Vector3(around.x + offset.x, around.y, around.z + offset.y);
            return ClampInside(point, bounds, margin);
        }

        /// <summary>Clamps X/Z of <paramref name="point"/> to the inner rectangle; Y is preserved.</summary>
        public static Vector3 ClampInside(Vector3 point, Bounds bounds, float margin)
        {
            GetInnerRect(bounds, margin, out float minX, out float maxX, out float minZ, out float maxZ);
            point.x = Mathf.Clamp(point.x, minX, maxX);
            point.z = Mathf.Clamp(point.z, minZ, maxZ);
            return point;
        }

        private static float ConfineAxis(float position, float component, float min, float max, float lookahead)
        {
            if (position <= min)
            {
                return InwardPush;
            }

            if (position >= max)
            {
                return -InwardPush;
            }

            // Distance to the edge the component points at; inward or zero components are untouched.
            float distance;
            if (component < 0f)
            {
                distance = position - min;
            }
            else if (component > 0f)
            {
                distance = max - position;
            }
            else
            {
                return 0f;
            }

            float travel = Mathf.Abs(component) * lookahead;
            if (travel <= distance)
            {
                return component;
            }

            // Scale so the projected position lands exactly on the edge: goes to 0 as distance -> 0.
            float scaled = lookahead > Epsilon ? distance / lookahead : 0f;
            return Mathf.Sign(component) * scaled;
        }

        private static void GetInnerRect(Bounds bounds, float margin, out float minX, out float maxX, out float minZ, out float maxZ)
        {
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;

            // A margin larger than the bounds collapses that axis to its center line.
            float halfX = Mathf.Max(0f, extents.x - margin);
            float halfZ = Mathf.Max(0f, extents.z - margin);

            minX = center.x - halfX;
            maxX = center.x + halfX;
            minZ = center.z - halfZ;
            maxZ = center.z + halfZ;
        }
    }
}
