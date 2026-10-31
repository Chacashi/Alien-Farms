using AlienFarms.Core;

namespace AlienFarms.Data
{
    /// <summary>Plain C# store of the players' pre-match choices, one <see cref="SessionSlot"/> per <see cref="PlayerId"/>.</summary>
    public sealed class SessionData
    {
        public const int MaxPlayers = 2;

        // Index = (int)PlayerId - 1. default(SessionSlot) = empty (Id None).
        private readonly SessionSlot[] _slots = new SessionSlot[MaxPlayers];

        /// <summary>Stores <paramref name="slot"/> under its player, replacing any previous one. Ignores invalid ids.</summary>
        public bool Set(SessionSlot slot)
        {
            int index = ToIndex(slot.Id);
            if (index < 0)
            {
                return false;
            }

            _slots[index] = slot;
            return true;
        }

        public bool TryGet(PlayerId id, out SessionSlot slot)
        {
            int index = ToIndex(id);
            slot = index >= 0 ? _slots[index] : default;
            return slot.IsValid;
        }

        /// <summary>True when the first <paramref name="requiredPlayers"/> players all have a slot.</summary>
        public bool IsComplete(int requiredPlayers)
        {
            if (requiredPlayers <= 0 || requiredPlayers > MaxPlayers)
            {
                return false;
            }

            for (int i = 0; i < requiredPlayers; i++)
            {
                if (!_slots[i].IsValid)
                {
                    return false;
                }
            }

            return true;
        }

        public void Clear()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i] = default;
            }
        }

        private static int ToIndex(PlayerId id)
        {
            int index = (int)id - 1;
            return index >= 0 && index < MaxPlayers ? index : -1;
        }
    }
}
