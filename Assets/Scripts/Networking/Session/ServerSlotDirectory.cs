using AlienFarms.Core;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Server-side seat table: which client holds Player1 / Player2. Plain C#, owned by the approval handler and shared
    /// with the roster through the runtime anchor. Version 1.5: a seat is freed as soon as its client disconnects.
    /// </summary>
    public sealed class ServerSlotDirectory
    {
        public const int Capacity = 2;

        // Index = (int)PlayerId - 1.
        private readonly PlayerSlot[] _slots = new PlayerSlot[Capacity];

        /// <summary>Seats currently held by a connected client.</summary>
        public int ConnectedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _slots.Length; i++)
                {
                    if (_slots[i].IsOccupied && _slots[i].Connected)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>
        /// Gives <paramref name="clientId"/> the first free seat (Player1 first). False when it already has one or all are
        /// taken. Since 2.2 the seat starts without a character (<see cref="ConnectionPayload.NoCharacter"/>): the payload
        /// pick is ignored and the online room sets it through <see cref="TrySetCharacter"/>.
        /// </summary>
        public bool TryAssign(ulong clientId, in ConnectionPayload payload, out PlayerId id)
        {
            id = PlayerId.None;
            if (TryGetByClient(clientId, out _))
            {
                return false;
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].IsOccupied)
                {
                    continue;
                }

                id = (PlayerId)(i + 1);
                _slots[i] = new PlayerSlot
                {
                    Id = id,
                    ClientId = clientId,
                    DisplayName = payload.DisplayName,
                    CharacterIndex = ConnectionPayload.NoCharacter,
                    Connected = true,
                    UgsPlayerId = payload.UgsPlayerId
                };
                return true;
            }

            return false;
        }

        /// <summary>Sets the character of the seat <paramref name="id"/>. False when that seat is empty.</summary>
        public bool TrySetCharacter(PlayerId id, byte characterIndex)
        {
            int index = (int)id - 1;
            if (index < 0 || index >= _slots.Length || !_slots[index].IsOccupied)
            {
                return false;
            }

            _slots[index].CharacterIndex = characterIndex;
            return true;
        }

        public bool TryGetByClient(ulong clientId, out PlayerSlot slot)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].IsOccupied && _slots[i].ClientId == clientId)
                {
                    slot = _slots[i];
                    return true;
                }
            }

            slot = default;
            return false;
        }

        public bool TryGet(PlayerId id, out PlayerSlot slot)
        {
            int index = (int)id - 1;
            slot = index >= 0 && index < _slots.Length ? _slots[index] : default;
            return slot.IsOccupied;
        }

        /// <summary>Frees the seat of <paramref name="clientId"/>. False when it held none.</summary>
        public bool Release(ulong clientId, out PlayerId id)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].IsOccupied && _slots[i].ClientId == clientId)
                {
                    id = _slots[i].Id;
                    _slots[i] = default;
                    return true;
                }
            }

            id = PlayerId.None;
            return false;
        }

        public void Clear()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i] = default;
            }
        }
    }
}
