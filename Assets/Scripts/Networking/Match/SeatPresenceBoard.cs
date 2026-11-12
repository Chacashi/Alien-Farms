using System;
using AlienFarms.Core;
using Unity.Netcode;

namespace AlienFarms.Networking.Match
{
    /// <summary>
    /// Presence of both player seats plus the spectator place, replicated by <see cref="NetworkSeatPresence"/>. Written
    /// whole by the server (rebuilt from the seat table), so it stays a small fixed-size value (45 bytes).
    /// </summary>
    public struct SeatPresenceBoard : INetworkSerializable, IEquatable<SeatPresenceBoard>
    {
        private SeatPresence _player1;
        private SeatPresence _player2;
        private ulong _spectatorClientId;
        private bool _hasSpectator;

        public SeatPresenceBoard(in SeatPresence player1, in SeatPresence player2, bool hasSpectator, ulong spectatorClientId)
        {
            _player1 = player1;
            _player2 = player2;
            _hasSpectator = hasSpectator;
            _spectatorClientId = hasSpectator ? spectatorClientId : 0UL;
        }

        public SeatPresence Player1 => _player1;

        public SeatPresence Player2 => _player2;

        public bool HasSpectator => _hasSpectator;

        /// <summary>NGO client of the spectator; 0 when <see cref="HasSpectator"/> is false.</summary>
        public ulong SpectatorClientId => _spectatorClientId;

        /// <summary>Presence of <paramref name="id"/>'s seat (<see cref="SeatPresence.Empty"/> for an invalid id).</summary>
        public SeatPresence Get(PlayerId id)
        {
            switch (id)
            {
                case PlayerId.Player1:
                    return _player1;
                case PlayerId.Player2:
                    return _player2;
                default:
                    return SeatPresence.Empty;
            }
        }

        /// <summary>Copy with <paramref name="id"/>'s seat replaced (unchanged for an invalid id).</summary>
        public SeatPresenceBoard With(PlayerId id, in SeatPresence presence)
        {
            switch (id)
            {
                case PlayerId.Player1:
                    return new SeatPresenceBoard(presence, _player2, _hasSpectator, _spectatorClientId);
                case PlayerId.Player2:
                    return new SeatPresenceBoard(_player1, presence, _hasSpectator, _spectatorClientId);
                default:
                    return this;
            }
        }

        /// <summary>Copy with the spectator place set (or cleared with <paramref name="hasSpectator"/> false).</summary>
        public SeatPresenceBoard WithSpectator(bool hasSpectator, ulong clientId) =>
            new SeatPresenceBoard(_player1, _player2, hasSpectator, clientId);

        /// <summary>True when <paramref name="clientId"/> is the spectator.</summary>
        public bool IsSpectator(ulong clientId) => _hasSpectator && _spectatorClientId == clientId;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            _player1.NetworkSerialize(serializer);
            _player2.NetworkSerialize(serializer);
            serializer.SerializeValue(ref _hasSpectator);
            serializer.SerializeValue(ref _spectatorClientId);
        }

        public bool Equals(SeatPresenceBoard other) =>
            _player1.Equals(other._player1)
            && _player2.Equals(other._player2)
            && _hasSpectator == other._hasSpectator
            && _spectatorClientId == other._spectatorClientId;

        public override bool Equals(object obj) => obj is SeatPresenceBoard other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = _player1.GetHashCode();
                hash = (hash * 397) ^ _player2.GetHashCode();
                hash = (hash * 397) ^ (_hasSpectator ? 1 : 0);
                return (hash * 397) ^ _spectatorClientId.GetHashCode();
            }
        }
    }
}
