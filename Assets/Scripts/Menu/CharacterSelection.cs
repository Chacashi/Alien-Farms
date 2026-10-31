using AlienFarms.Core;

namespace AlienFarms.Menu
{
    /// <summary>
    /// Plain C# model of the character select screen: which players joined, which character each one hovers and which
    /// characters are locked (confirmed). A character can be locked by a single player; locked characters are skipped
    /// by the other players' cursors.
    /// </summary>
    public sealed class CharacterSelection
    {
        public const int NoCharacter = -1;

        private readonly int _characterCount;
        private readonly bool[] _joined;
        private readonly int[] _hover;
        private readonly int[] _confirmed;

        public CharacterSelection(int characterCount, int maxPlayers = 2)
        {
            _characterCount = characterCount < 0 ? 0 : characterCount;
            int players = maxPlayers < 1 ? 1 : maxPlayers;
            _joined = new bool[players];
            _hover = new int[players];
            _confirmed = new int[players];
            for (int i = 0; i < players; i++)
            {
                _hover[i] = NoCharacter;
                _confirmed[i] = NoCharacter;
            }
        }

        public int CharacterCount => _characterCount;

        public int MaxPlayers => _joined.Length;

        public int JoinedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _joined.Length; i++)
                {
                    if (_joined[i])
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>Every player slot is joined and confirmed.</summary>
        public bool AllConfirmed
        {
            get
            {
                for (int i = 0; i < _joined.Length; i++)
                {
                    if (!_joined[i] || _confirmed[i] == NoCharacter)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool IsJoined(PlayerId id)
        {
            int index = ToIndex(id);
            return index >= 0 && _joined[index];
        }

        public bool IsConfirmed(PlayerId id)
        {
            int index = ToIndex(id);
            return index >= 0 && _confirmed[index] != NoCharacter;
        }

        /// <summary>Character index hovered by <paramref name="id"/> (also while confirmed), or <see cref="NoCharacter"/>.</summary>
        public int GetHover(PlayerId id)
        {
            int index = ToIndex(id);
            return index >= 0 ? _hover[index] : NoCharacter;
        }

        /// <summary>Character index locked by <paramref name="id"/>, or <see cref="NoCharacter"/>.</summary>
        public int GetConfirmedIndex(PlayerId id)
        {
            int index = ToIndex(id);
            return index >= 0 ? _confirmed[index] : NoCharacter;
        }

        /// <summary>Player that locked <paramref name="characterIndex"/>, or None.</summary>
        public PlayerId LockedBy(int characterIndex)
        {
            for (int i = 0; i < _confirmed.Length; i++)
            {
                if (_confirmed[i] != NoCharacter && _confirmed[i] == characterIndex)
                {
                    return (PlayerId)(i + 1);
                }
            }

            return PlayerId.None;
        }

        /// <summary>Joins <paramref name="id"/>, hovering the first free character from its own index (P1 -> 0, P2 -> 1).</summary>
        public bool Join(PlayerId id)
        {
            int index = ToIndex(id);
            if (index < 0 || _joined[index] || _characterCount == 0)
            {
                return false;
            }

            _joined[index] = true;
            _confirmed[index] = NoCharacter;
            _hover[index] = FindFree(index, index % _characterCount, 1, includeStart: true);
            return true;
        }

        /// <summary>Removes <paramref name="id"/> and frees its lock.</summary>
        public bool Leave(PlayerId id)
        {
            int index = ToIndex(id);
            if (index < 0 || !_joined[index])
            {
                return false;
            }

            _joined[index] = false;
            _hover[index] = NoCharacter;
            _confirmed[index] = NoCharacter;
            return true;
        }

        /// <summary>
        /// Moves the cursor of a joined, unconfirmed player one step in <paramref name="direction"/> (cyclic), skipping
        /// characters locked by others. Returns true if the hover changed.
        /// </summary>
        public bool Move(PlayerId id, int direction)
        {
            int index = ToIndex(id);
            if (index < 0 || !_joined[index] || _confirmed[index] != NoCharacter || direction == 0)
            {
                return false;
            }

            int current = _hover[index];
            int next = FindFree(index, current, direction > 0 ? 1 : -1, includeStart: false);
            if (next == NoCharacter || next == current)
            {
                return false;
            }

            _hover[index] = next;
            return true;
        }

        /// <summary>
        /// Locks the hovered character for <paramref name="id"/>. Fails if it is locked by someone else. Other unconfirmed
        /// players hovering it are moved to a free character.
        /// </summary>
        public bool Confirm(PlayerId id)
        {
            int index = ToIndex(id);
            if (index < 0 || !_joined[index] || _confirmed[index] != NoCharacter)
            {
                return false;
            }

            int character = _hover[index];
            if (character == NoCharacter || IsLockedByOther(index, character))
            {
                return false;
            }

            _confirmed[index] = character;
            for (int i = 0; i < _joined.Length; i++)
            {
                if (i != index && _joined[i] && _confirmed[i] == NoCharacter && _hover[i] == character)
                {
                    int free = FindFree(i, character, 1, includeStart: false);
                    _hover[i] = free != NoCharacter ? free : character;
                }
            }

            return true;
        }

        /// <summary>Unlocks the character confirmed by <paramref name="id"/>. False if it was not confirmed.</summary>
        public bool Cancel(PlayerId id)
        {
            int index = ToIndex(id);
            if (index < 0 || _confirmed[index] == NoCharacter)
            {
                return false;
            }

            _confirmed[index] = NoCharacter;
            return true;
        }

        /// <summary>
        /// First character not locked by a player other than <paramref name="playerIndex"/>, walking from
        /// <paramref name="start"/> in <paramref name="step"/> (cyclic). Falls back to <paramref name="start"/> when none is free.
        /// </summary>
        private int FindFree(int playerIndex, int start, int step, bool includeStart)
        {
            if (_characterCount == 0)
            {
                return NoCharacter;
            }

            int origin = start < 0 ? 0 : start;
            int first = includeStart ? 0 : 1;
            for (int offset = first; offset <= _characterCount; offset++)
            {
                int candidate = Wrap(origin + offset * step);
                if (!IsLockedByOther(playerIndex, candidate))
                {
                    return candidate;
                }
            }

            return start < 0 ? NoCharacter : start;
        }

        private bool IsLockedByOther(int playerIndex, int character)
        {
            for (int i = 0; i < _confirmed.Length; i++)
            {
                if (i != playerIndex && _confirmed[i] == character)
                {
                    return true;
                }
            }

            return false;
        }

        private int Wrap(int value)
        {
            int result = value % _characterCount;
            return result < 0 ? result + _characterCount : result;
        }

        private int ToIndex(PlayerId id)
        {
            int index = (int)id - 1;
            return index >= 0 && index < _joined.Length ? index : -1;
        }
    }
}
