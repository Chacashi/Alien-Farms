using System;
using AlienFarms.Core;
using AlienFarms.Items;
using Unity.Netcode;

namespace AlienFarms.Networking.Items
{
    /// <summary>Replicated gameplay state of a bomb: its <see cref="BombState"/>, who carries it and who threw it. 3 bytes.</summary>
    public struct BombNetState : INetworkSerializable, IEquatable<BombNetState>
    {
        private byte _state;
        private byte _holder;
        private byte _thrower;

        public BombNetState(BombState state, PlayerId holder, PlayerId thrower)
        {
            _state = (byte)state;
            _holder = (byte)holder;
            _thrower = (byte)thrower;
        }

        public BombState State => (BombState)_state;

        public PlayerId Holder => (PlayerId)_holder;

        public PlayerId Thrower => (PlayerId)_thrower;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _state);
            serializer.SerializeValue(ref _holder);
            serializer.SerializeValue(ref _thrower);
        }

        public bool Equals(BombNetState other) =>
            _state == other._state && _holder == other._holder && _thrower == other._thrower;

        public override bool Equals(object obj) => obj is BombNetState other && Equals(other);

        public override int GetHashCode() => _state | (_holder << 8) | (_thrower << 16);
    }
}
