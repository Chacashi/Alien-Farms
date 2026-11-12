using System;
using AlienFarms.Core;
using Unity.Netcode;

namespace AlienFarms.Networking.Match
{
    /// <summary>
    /// Replicated presence of one player seat: who holds it and its <see cref="PlayerConnectionStatus"/>. A seat held for
    /// a dropped player is <see cref="PlayerConnectionStatus.Reconnecting"/> until <see cref="GraceEndTime"/> (server
    /// time). A free seat keeps how it was last released (Left / TimedOut), or Connected when it was never released.
    /// Fixed 18 bytes on the wire.
    /// </summary>
    public struct SeatPresence : INetworkSerializable, IEquatable<SeatPresence>
    {
        private bool _isSeated;
        private ulong _clientId;
        private byte _status;
        private double _graceEndTime;

        private SeatPresence(bool isSeated, ulong clientId, PlayerConnectionStatus status, double graceEndTime)
        {
            _isSeated = isSeated;
            _clientId = clientId;
            _status = (byte)status;
            _graceEndTime = graceEndTime;
        }

        /// <summary>A seat nobody ever held (or held since the last release was forgotten).</summary>
        public static SeatPresence Empty => default;

        /// <summary>True while a client holds the seat or the seat waits for its dropped client.</summary>
        public bool IsSeated => _isSeated;

        /// <summary>NGO client of the seat (the dropped one while reconnecting); 0 when not seated.</summary>
        public ulong ClientId => _clientId;

        public PlayerConnectionStatus Status => (PlayerConnectionStatus)_status;

        /// <summary>Server time (s) when the reservation expires; 0 unless <see cref="PlayerConnectionStatus.Reconnecting"/>.</summary>
        public double GraceEndTime => _graceEndTime;

        public static SeatPresence Connected(ulong clientId) =>
            new SeatPresence(true, clientId, PlayerConnectionStatus.Connected, 0d);

        public static SeatPresence Reconnecting(ulong clientId, double graceEndTime) =>
            new SeatPresence(true, clientId, PlayerConnectionStatus.Reconnecting, graceEndTime);

        /// <summary>A free seat whose last owner went away with <paramref name="status"/> (Left or TimedOut).</summary>
        public static SeatPresence Released(PlayerConnectionStatus status) =>
            new SeatPresence(false, 0UL, status, 0d);

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _isSeated);
            serializer.SerializeValue(ref _clientId);
            serializer.SerializeValue(ref _status);
            serializer.SerializeValue(ref _graceEndTime);
        }

        public bool Equals(SeatPresence other) =>
            _isSeated == other._isSeated
            && _clientId == other._clientId
            && _status == other._status
            && _graceEndTime.Equals(other._graceEndTime);

        public override bool Equals(object obj) => obj is SeatPresence other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = _clientId.GetHashCode();
                hash = (hash * 397) ^ _status;
                hash = (hash * 397) ^ (_isSeated ? 1 : 0);
                return (hash * 397) ^ _graceEndTime.GetHashCode();
            }
        }
    }
}
