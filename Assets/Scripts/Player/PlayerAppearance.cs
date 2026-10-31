using AlienFarms.Data;
using UnityEngine;

namespace AlienFarms.Player
{
    /// <summary>
    /// Applies the player's character visual by activating one of the pre-built variants under the Visual child
    /// (no instantiation) and deactivating the rest.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerAppearance : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Character visual variants. The first one is used when no variant matches.")]
        private CharacterVisual[] _variants = new CharacterVisual[0];

        /// <summary>Activates the variant of <paramref name="character"/>; falls back to the first variant (with a warning).</summary>
        public void ApplyCharacter(CharacterDefinition character)
        {
            if (_variants == null || _variants.Length == 0)
            {
                return;
            }

            int selected = -1;
            for (int i = 0; i < _variants.Length; i++)
            {
                if (character != null && _variants[i].Character == character)
                {
                    selected = i;
                    break;
                }
            }

            if (selected < 0)
            {
                Debug.LogWarning($"[{nameof(PlayerAppearance)}] No visual for character '{(character != null ? character.name : "null")}' on '{name}'; using the first variant.", this);
                selected = 0;
            }

            for (int i = 0; i < _variants.Length; i++)
            {
                GameObject root = _variants[i].Root;
                if (root != null && root.activeSelf != (i == selected))
                {
                    root.SetActive(i == selected);
                }
            }
        }

        private void OnValidate()
        {
            if (_variants == null || _variants.Length == 0)
            {
                Debug.LogWarning($"[{nameof(PlayerAppearance)}] No character visual variants on '{name}'.", this);
                return;
            }

            for (int i = 0; i < _variants.Length; i++)
            {
                if (_variants[i].Character == null || _variants[i].Root == null)
                {
                    Debug.LogWarning($"[{nameof(PlayerAppearance)}] Variant {i} on '{name}' is missing its character or root.", this);
                }
            }
        }
    }
}
