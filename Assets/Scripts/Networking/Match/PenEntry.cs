using System;
using Unity.Netcode;

namespace AlienFarms.Networking.Match
{
    /// <summary>
    /// One line of a player's pen ledger: how many animals of a type (index in the spawn settings' entries) the pen
    /// holds. Element of a NetworkList, so it is unmanaged and equatable.
    /// </summary>
    public struct PenEntry : INetworkSerializable, IEquatable<PenEntry>
    {
        private byte _animalTypeIndex;
        private ushort _count;

        public PenEntry(int animalTypeIndex, int count)
        {
            _animalTypeIndex = (byte)Math.Max(0, Math.Min(byte.MaxValue, animalTypeIndex));
            _count = (ushort)Math.Max(0, Math.Min(ushort.MaxValue, count));
        }

        public int AnimalTypeIndex => _animalTypeIndex;

        public int Count => _count;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _animalTypeIndex);
            serializer.SerializeValue(ref _count);
        }

        public bool Equals(PenEntry other) => _animalTypeIndex == other._animalTypeIndex && _count == other._count;

        public override bool Equals(object obj) => obj is PenEntry other && Equals(other);

        public override int GetHashCode() => _animalTypeIndex | (_count << 8);
    }
}
