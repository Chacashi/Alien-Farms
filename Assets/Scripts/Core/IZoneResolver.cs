using UnityEngine;

namespace AlienFarms.Core
{
    /// <summary>Answers which player zone (if any) contains a world position.</summary>
    public interface IZoneResolver
    {
        /// <summary>
        /// True when <paramref name="position"/> lies inside a player zone (planar test); returns its owner and
        /// world-space area.
        /// </summary>
        bool TryGetZoneAt(Vector3 position, out PlayerId owner, out Bounds area);
    }
}
