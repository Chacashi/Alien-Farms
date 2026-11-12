using System;
using AlienFarms.Networking.Pooling;

namespace AlienFarms.Networking.Diagnostics
{
    /// <summary>
    /// One reading of the online debug stats view: pool counters of this machine and, when a transport is running, its
    /// receive side (packets per second, receive-queue usage) and the longest frame since the session started. Compared
    /// with the previous reading so the label is rebuilt only when a number changes.
    /// </summary>
    public readonly struct NetworkStatsSample : IEquatable<NetworkStatsSample>
    {
        public NetworkStatsSample(NetworkPoolStats animals, NetworkPoolStats bombs)
            : this(animals, bombs, false, 0, 0, 0, 0, 0)
        {
        }

        public NetworkStatsSample(
            NetworkPoolStats animals,
            NetworkPoolStats bombs,
            bool hasTransport,
            int rxPacketsPerSecond,
            int rxQueueMeanTenths,
            int rxQueueMax,
            int queueCapacity,
            int longestFrameMs)
        {
            Animals = animals;
            Bombs = bombs;
            HasTransport = hasTransport;
            RxPacketsPerSecond = rxPacketsPerSecond;
            RxQueueMeanTenths = rxQueueMeanTenths;
            RxQueueMax = rxQueueMax;
            QueueCapacity = queueCapacity;
            LongestFrameMs = longestFrameMs;
        }

        public NetworkPoolStats Animals { get; }

        public NetworkPoolStats Bombs { get; }

        /// <summary>False offline or with a transport other than UnityTransport: the network lines are omitted.</summary>
        public bool HasTransport { get; }

        public int RxPacketsPerSecond { get; }

        /// <summary>Mean packets in the receive queue per driver update, in tenths (lifetime of the driver).</summary>
        public int RxQueueMeanTenths { get; }

        /// <summary>Most packets the receive queue held in one update (lifetime of the driver).</summary>
        public int RxQueueMax { get; }

        /// <summary>Receive-queue capacity (UnityTransport Max Packet Queue Size).</summary>
        public int QueueCapacity { get; }

        /// <summary>Longest frame of this machine since the transport started (ms): a stall lets packets pile up.</summary>
        public int LongestFrameMs { get; }

        public bool Equals(NetworkStatsSample other) =>
            Animals == other.Animals
            && Bombs == other.Bombs
            && HasTransport == other.HasTransport
            && RxPacketsPerSecond == other.RxPacketsPerSecond
            && RxQueueMeanTenths == other.RxQueueMeanTenths
            && RxQueueMax == other.RxQueueMax
            && QueueCapacity == other.QueueCapacity
            && LongestFrameMs == other.LongestFrameMs;

        public override bool Equals(object obj) => obj is NetworkStatsSample other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(Animals, Bombs, HasTransport, RxPacketsPerSecond, RxQueueMeanTenths, RxQueueMax, QueueCapacity, LongestFrameMs);

        public static bool operator ==(NetworkStatsSample left, NetworkStatsSample right) => left.Equals(right);

        public static bool operator !=(NetworkStatsSample left, NetworkStatsSample right) => !left.Equals(right);
    }
}
