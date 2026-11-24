using System;
using AlienFarms.Networking.Interest;

namespace AlienFarms.Networking.Diagnostics
{
    public readonly struct InterestStatsSample : IEquatable<InterestStatsSample>
    {
        public bool IsHost { get; }
        public bool IsActive { get; }
        public float EnterRadius { get; }
        public float ExitRadius { get; }
        public int ReviewIntervalTicks { get; }
        public KnownObjectsCounts Known { get; }
        public InterestCounters Remote { get; }
        public int TotalAnimals { get; }
        public int TotalBombs { get; }
        public int WindowSeconds { get; }
        public long RxKilobytes { get; }
        public long RxKilobytesPerSecondTenths { get; }

        public InterestStatsSample(bool isHost, bool isActive, float enterRadius, float exitRadius,
            int reviewIntervalTicks, KnownObjectsCounts known, InterestCounters remote, int totalAnimals,
            int totalBombs, int windowSeconds, long rxKilobytes, long rxKilobytesPerSecondTenths)
        {
            IsHost = isHost;
            IsActive = isActive;
            EnterRadius = enterRadius;
            ExitRadius = exitRadius;
            ReviewIntervalTicks = reviewIntervalTicks;
            Known = known;
            Remote = remote;
            TotalAnimals = totalAnimals;
            TotalBombs = totalBombs;
            WindowSeconds = windowSeconds;
            RxKilobytes = rxKilobytes;
            RxKilobytesPerSecondTenths = rxKilobytesPerSecondTenths;
        }

        public bool Equals(InterestStatsSample other) =>
            IsHost == other.IsHost && IsActive == other.IsActive && EnterRadius.Equals(other.EnterRadius) &&
            ExitRadius.Equals(other.ExitRadius) && ReviewIntervalTicks == other.ReviewIntervalTicks &&
            Known.Animals == other.Known.Animals && Known.Bombs == other.Known.Bombs && Known.Other == other.Known.Other &&
            Remote.Shows == other.Remote.Shows && Remote.Hides == other.Remote.Hides &&
            Remote.VisibleAnimals == other.Remote.VisibleAnimals && Remote.VisibleBombs == other.Remote.VisibleBombs &&
            TotalAnimals == other.TotalAnimals && TotalBombs == other.TotalBombs && WindowSeconds == other.WindowSeconds &&
            RxKilobytes == other.RxKilobytes && RxKilobytesPerSecondTenths == other.RxKilobytesPerSecondTenths;

        public override bool Equals(object obj) => obj is InterestStatsSample other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(
            HashCode.Combine(IsHost, IsActive, EnterRadius, ExitRadius, ReviewIntervalTicks),
            HashCode.Combine(Known.Animals, Known.Bombs, Known.Other),
            HashCode.Combine(Remote.Shows, Remote.Hides, Remote.VisibleAnimals, Remote.VisibleBombs),
            HashCode.Combine(TotalAnimals, TotalBombs, WindowSeconds, RxKilobytes, RxKilobytesPerSecondTenths));
    }
}
