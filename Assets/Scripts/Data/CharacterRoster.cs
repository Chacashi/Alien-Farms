using UnityEngine;

namespace AlienFarms.Data
{
    /// <summary>Ordered list of selectable characters (character select order).</summary>
    [CreateAssetMenu(fileName = "CharacterRoster", menuName = "Alien Farms/Character Roster")]
    public sealed class CharacterRoster : ScriptableObject
    {
        [SerializeField, Tooltip("Selectable characters in display order.")]
        private CharacterDefinition[] _characters = new CharacterDefinition[0];

        public int Count => _characters != null ? _characters.Length : 0;

        /// <summary>Character at <paramref name="index"/>, or null when out of range.</summary>
        public CharacterDefinition Get(int index)
        {
            return index >= 0 && index < Count ? _characters[index] : null;
        }

        /// <summary>Index of <paramref name="character"/> in the roster, or -1.</summary>
        public int IndexOf(CharacterDefinition character)
        {
            if (character == null)
            {
                return -1;
            }

            for (int i = 0; i < Count; i++)
            {
                if (_characters[i] == character)
                {
                    return i;
                }
            }

            return -1;
        }

        private void OnValidate()
        {
            for (int i = 0; i < Count; i++)
            {
                if (_characters[i] == null)
                {
                    Debug.LogWarning($"[{nameof(CharacterRoster)}] Entry {i} is empty on '{name}'.", this);
                }
            }
        }
    }
}
