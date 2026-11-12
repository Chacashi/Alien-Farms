using System;
using AlienFarms.Core;

namespace AlienFarms.Networking.Player
{
    /// <summary>Resolved owner of a carriable object: the server, a seat, or a client not resolved yet.</summary>
    public readonly struct CarriableOwner : IEquatable<CarriableOwner>
    {
        private CarriableOwner(CarriableOwnerKind kind, PlayerId seat)
        {
            Kind = kind;
            Seat = seat;
        }

        public static CarriableOwner Server => new CarriableOwner(CarriableOwnerKind.Server, PlayerId.None);

        public static CarriableOwner Unresolved => new CarriableOwner(CarriableOwnerKind.Unresolved, PlayerId.None);

        public CarriableOwnerKind Kind { get; }

        /// <summary>The owning seat when <see cref="Kind"/> is <see cref="CarriableOwnerKind.Player"/>; None otherwise.</summary>
        public PlayerId Seat { get; }

        /// <summary>A seated player; <see cref="PlayerId.None"/> gives <see cref="Server"/>.</summary>
        public static CarriableOwner ForSeat(PlayerId seat) =>
            seat == PlayerId.None ? Server : new CarriableOwner(CarriableOwnerKind.Player, seat);

        public bool Equals(CarriableOwner other) => Kind == other.Kind && Seat == other.Seat;

        public override bool Equals(object obj) => obj is CarriableOwner other && Equals(other);

        public override int GetHashCode() => ((int)Kind << 8) | (int)Seat;

        public static bool operator ==(CarriableOwner left, CarriableOwner right) => left.Equals(right);

        public static bool operator !=(CarriableOwner left, CarriableOwner right) => !left.Equals(right);

        public override string ToString() => Kind == CarriableOwnerKind.Player ? $"{Kind}({Seat})" : Kind.ToString();
    }
}
