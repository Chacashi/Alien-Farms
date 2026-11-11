using System;
using AlienFarms.Animals;
using AlienFarms.Core;
using Unity.Netcode;

namespace AlienFarms.Networking.Animals
{
    /// <summary>
    /// Replicated gameplay state of an animal: its <see cref="AnimalState"/>, who carries it, whose pen holds it and why
    /// it last left a player's hands (clients raise the same drop GameEvent the server did). 4 bytes.
    /// </summary>
    public struct AnimalNetState : INetworkSerializable, IEquatable<AnimalNetState>
    {
        private byte _state;
        private byte _holder;
        private byte _zoneOwner;
        private byte _lastDrop;

        public AnimalNetState(AnimalState state, PlayerId holder, PlayerId zoneOwner, DropReason lastDrop)
        {
            _state = (byte)state;
            _holder = (byte)holder;
            _zoneOwner = (byte)zoneOwner;
            _lastDrop = (byte)lastDrop;
        }

        public AnimalState State => (AnimalState)_state;

        public PlayerId Holder => (PlayerId)_holder;

        public PlayerId ZoneOwner => (PlayerId)_zoneOwner;

        public DropReason LastDrop => (DropReason)_lastDrop;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _state);
            serializer.SerializeValue(ref _holder);
            serializer.SerializeValue(ref _zoneOwner);
            serializer.SerializeValue(ref _lastDrop);
        }

        public bool Equals(AnimalNetState other) =>
            _state == other._state && _holder == other._holder && _zoneOwner == other._zoneOwner && _lastDrop == other._lastDrop;

        public override bool Equals(object obj) => obj is AnimalNetState other && Equals(other);

        public override int GetHashCode() => _state | (_holder << 8) | (_zoneOwner << 16) | (_lastDrop << 24);
    }
}
