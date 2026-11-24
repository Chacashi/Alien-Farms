using System;
using AlienFarms.Core;
using Unity.Netcode;

namespace AlienFarms.Networking.Match
{
    /// <summary>
    /// Presence of both player seats, replicated by <see cref="NetworkSeatPresence"/>. Written
    /// whole by the server (rebuilt from the seat table), so it stays a small fixed-size value (36 bytes).
    /// </summary>
    public struct SeatPresenceBoard : INetworkSerializable, IEquatable<SeatPresenceBoard>
    {
        private SeatPresence _player1;
        private SeatPresence _player2;

        public SeatPresenceBoard(in SeatPresence player1, in SeatPresence player2)
        {
            _player1 = player1;
            _player2 = player2;
        }

        public SeatPresence Player1 => _player1;

        public SeatPresence Player2 => _player2;


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
                    return new SeatPresenceBoard(presence, _player2);
                case PlayerId.Player2:
                    return new SeatPresenceBoard(_player1, presence);
                default:
                    return this;
            }
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            _player1.NetworkSerialize(serializer);
            _player2.NetworkSerialize(serializer);
        }

        public bool Equals(SeatPresenceBoard other) =>
            _player1.Equals(other._player1)
            && _player2.Equals(other._player2);

        public override bool Equals(object obj) => obj is SeatPresenceBoard other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = _player1.GetHashCode();
                return (hash * 397) ^ _player2.GetHashCode();
            }
        }
    }
}
