using Unity.Netcode;

namespace AlienFarms.Networking.Player
{
    /// <summary>
    /// The last <see cref="MaxCommands"/> commands of a player, newest first, sent in every unreliable input packet so a
    /// lost packet is recovered by the next ones. Fixed fields, no arrays: no allocations to build or read it.
    /// </summary>
    public struct InputCommandBatch : INetworkSerializable
    {
        public const int MaxCommands = 3;

        private byte _count;
        private PlayerInputCommand _command0;
        private PlayerInputCommand _command1;
        private PlayerInputCommand _command2;

        public int Count => _count;

        /// <summary>Adds <paramref name="command"/> as the newest; the oldest falls off when full.</summary>
        public void Push(in PlayerInputCommand command)
        {
            _command2 = _command1;
            _command1 = _command0;
            _command0 = command;
            if (_count < MaxCommands)
            {
                _count++;
            }
        }

        /// <summary>Command at <paramref name="index"/> (0 = newest). Default for an index outside <see cref="Count"/>.</summary>
        public PlayerInputCommand Get(int index)
        {
            if (index < 0 || index >= _count)
            {
                return default;
            }

            switch (index)
            {
                case 0:
                    return _command0;
                case 1:
                    return _command1;
                default:
                    return _command2;
            }
        }

        public void Clear()
        {
            this = default;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _count);
            if (serializer.IsReader && _count > MaxCommands)
            {
                _count = MaxCommands;
            }

            if (_count > 0)
            {
                serializer.SerializeValue(ref _command0);
            }

            if (_count > 1)
            {
                serializer.SerializeValue(ref _command1);
            }

            if (_count > 2)
            {
                serializer.SerializeValue(ref _command2);
            }
        }
    }
}
