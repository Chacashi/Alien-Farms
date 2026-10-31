using AlienFarms.Core;
using AlienFarms.Data;

namespace AlienFarms.UI
{
    /// <summary>Plain C# map from <see cref="PlayerId"/> to the character assigned to that player (views keep one each).</summary>
    public sealed class PlayerCharacterCache
    {
        private const int MaxPlayers = 2;

        private readonly CharacterDefinition[] _characters = new CharacterDefinition[MaxPlayers];

        /// <summary>Stores the character of <paramref name="id"/>; ignored for <see cref="PlayerId.None"/>.</summary>
        public void Set(PlayerId id, CharacterDefinition character)
        {
            int index = ToIndex(id);
            if (index >= 0)
            {
                _characters[index] = character;
            }
        }

        /// <summary>Character of <paramref name="id"/>, or null if none was assigned.</summary>
        public CharacterDefinition Get(PlayerId id)
        {
            int index = ToIndex(id);
            return index >= 0 ? _characters[index] : null;
        }

        public void Clear()
        {
            for (int i = 0; i < _characters.Length; i++)
            {
                _characters[i] = null;
            }
        }

        private static int ToIndex(PlayerId id)
        {
            int index = (int)id - 1;
            return index >= 0 && index < MaxPlayers ? index : -1;
        }
    }
}
