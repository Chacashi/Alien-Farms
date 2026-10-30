using AlienFarms.Core;
using UnityEngine;

namespace AlienFarms.Zones
{
    /// <summary>
    /// Scene registry of the player pens (lives on the 'Zones' root). Animals ask it, through
    /// <see cref="IZoneResolver"/>, which pen they landed in.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerZoneRegistry : MonoBehaviour, IZoneResolver
    {
        private const int RequiredZones = 2;

        [Header("References")]
        [SerializeField, Tooltip("One PlayerZone per player (Player1 and Player2).")]
        private PlayerZone[] _zones = new PlayerZone[RequiredZones];

        public bool TryGetZoneAt(Vector3 position, out PlayerId owner, out Bounds area)
        {
            if (_zones != null)
            {
                for (int i = 0; i < _zones.Length; i++)
                {
                    PlayerZone zone = _zones[i];
                    if (zone != null && zone.Owner != PlayerId.None && zone.Contains(position))
                    {
                        owner = zone.Owner;
                        area = zone.Bounds;
                        return true;
                    }
                }
            }

            owner = PlayerId.None;
            area = default;
            return false;
        }

        private void OnValidate()
        {
            if (_zones == null || _zones.Length == 0)
            {
                Debug.LogWarning($"[{nameof(PlayerZoneRegistry)}] No player zones assigned on '{name}'.", this);
                return;
            }

            int player1 = 0;
            int player2 = 0;
            for (int i = 0; i < _zones.Length; i++)
            {
                PlayerZone zone = _zones[i];
                if (zone == null)
                {
                    Debug.LogWarning($"[{nameof(PlayerZoneRegistry)}] Player zone {i} is empty on '{name}'.", this);
                    continue;
                }

                if (zone.Owner == PlayerId.Player1)
                {
                    player1++;
                }
                else if (zone.Owner == PlayerId.Player2)
                {
                    player2++;
                }
            }

            if (player1 != 1 || player2 != 1)
            {
                Debug.LogWarning($"[{nameof(PlayerZoneRegistry)}] '{name}' needs exactly one zone for Player1 and one for Player2.", this);
            }
        }
    }
}
