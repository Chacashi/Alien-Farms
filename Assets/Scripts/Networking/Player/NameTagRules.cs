using AlienFarms.Core;
using AlienFarms.Data;
using UnityEngine;

namespace AlienFarms.Networking.Player
{
    /// <summary>
    /// Pure rules of <see cref="NetworkNameTag"/>: when an avatar's tag shows, which name and color it uses and the debug
    /// "client N" line (course topic 9: the reconnected player gets a new client id).
    /// </summary>
    public static class NameTagRules
    {
        public const string ClientDetailPrefix = "client ";

        /// <summary>
        /// The tag shows on spawned avatars with a seat, while the settings allow it, and never over this machine's own
        /// avatar (its owner sees through its eyes).
        /// </summary>
        public static bool IsVisible(bool isSpawned, bool showNameTags, bool isLocalOwner, PlayerId seat)
        {
            return isSpawned && showNameTags && !isLocalOwner && seat != PlayerId.None;
        }

        /// <summary>The replicated (sanitized) name, else the seat tag ("P1" / "P2").</summary>
        public static string ResolveName(string displayName, PlayerId seat)
        {
            return string.IsNullOrEmpty(displayName) ? CarriableOwnerRules.SeatTag(seat) : displayName;
        }

        /// <summary>Primary color of the character, else <paramref name="fallback"/>.</summary>
        public static Color ResolveColor(CharacterDefinition character, Color fallback)
        {
            return character != null ? character.Theme.Primary : fallback;
        }

        /// <summary>"client N" when <paramref name="showClientId"/>, else null (allocates: call on changes only).</summary>
        public static string ClientDetail(bool showClientId, ulong clientId)
        {
            return showClientId ? ClientDetailPrefix + clientId.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
        }
    }
}
