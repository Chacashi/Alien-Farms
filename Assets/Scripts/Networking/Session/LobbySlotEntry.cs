using System;
using AlienFarms.Core;
using Unity.Collections;
using Unity.Netcode;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// One seat of the online room as replicated by <see cref="NetworkLobbyState"/>. Element of a NetworkList, so it is
    /// unmanaged and equatable. <see cref="CharacterIndex"/> is <see cref="PlayerSlot.NoCharacter"/> while the seat
    /// has no pick; picking counts as ready.
    /// </summary>
    public struct LobbySlotEntry : INetworkSerializable, IEquatable<LobbySlotEntry>
    {
        private byte _id;
        private ulong _clientId;
        private byte _characterIndex;
        private FixedString32Bytes _displayName;

        public LobbySlotEntry(PlayerId id, ulong clientId, byte characterIndex, in FixedString32Bytes displayName)
        {
            _id = (byte)id;
            _clientId = clientId;
            _characterIndex = characterIndex;
            _displayName = displayName;
        }

        public PlayerId Id => (PlayerId)_id;

        public ulong ClientId => _clientId;

        /// <summary>Roster index of the pick, <see cref="PlayerSlot.NoCharacter"/> when none.</summary>
        public byte CharacterIndex => _characterIndex;

        public FixedString32Bytes DisplayName => _displayName;

        public bool HasCharacter => _characterIndex != PlayerSlot.NoCharacter;

        /// <summary>Copy of this entry with another pick (<see cref="PlayerSlot.NoCharacter"/> releases it).</summary>
        public LobbySlotEntry WithCharacter(byte characterIndex)
        {
            return new LobbySlotEntry(Id, _clientId, characterIndex, _displayName);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _id);
            serializer.SerializeValue(ref _clientId);
            serializer.SerializeValue(ref _characterIndex);
            serializer.SerializeValue(ref _displayName);
        }

        public bool Equals(LobbySlotEntry other)
        {
            return _id == other._id
                && _clientId == other._clientId
                && _characterIndex == other._characterIndex
                && _displayName.Equals(other._displayName);
        }

        public override bool Equals(object obj)
        {
            return obj is LobbySlotEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_id, _clientId, _characterIndex, _displayName.GetHashCode());
        }
    }
}
