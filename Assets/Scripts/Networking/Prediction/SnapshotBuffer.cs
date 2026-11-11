using AlienFarms.Networking.Player;

namespace AlienFarms.Networking.Prediction
{
    /// <summary>
    /// Fixed ring of received <see cref="PlayerSnapshot"/>s, oldest first and strictly increasing by tick. Out-of-order or
    /// duplicate snapshots are dropped; when full the oldest is overwritten. A snapshot from another
    /// <see cref="PlayerSnapshot.TeleportEpoch"/> clears the history, so all buffered snapshots share one epoch and
    /// nothing ever interpolates across a teleport.
    /// </summary>
    public sealed class SnapshotBuffer
    {
        public const int DefaultCapacity = 32;

        private readonly PlayerSnapshot[] _items;
        private int _start;
        private int _count;

        public SnapshotBuffer(int capacity = DefaultCapacity)
        {
            _items = new PlayerSnapshot[capacity < 2 ? 2 : capacity];
        }

        public int Count => _count;

        public int Capacity => _items.Length;

        /// <summary>Snapshot at <paramref name="index"/> (0 = oldest). Precondition: 0 &lt;= index &lt; <see cref="Count"/>.</summary>
        public PlayerSnapshot this[int index] => _items[(_start + index) % _items.Length];

        /// <summary>The most recent snapshot; false when the buffer is empty.</summary>
        public bool TryGetNewest(out PlayerSnapshot snapshot)
        {
            if (_count == 0)
            {
                snapshot = default;
                return false;
            }

            snapshot = this[_count - 1];
            return true;
        }

        public SnapshotAddResult Add(in PlayerSnapshot snapshot)
        {
            bool hasNewest = TryGetNewest(out PlayerSnapshot newest);
            if (hasNewest && snapshot.Tick <= newest.Tick)
            {
                return SnapshotAddResult.Rejected;
            }

            bool reset = !hasNewest || snapshot.TeleportEpoch != newest.TeleportEpoch;
            if (reset)
            {
                _start = 0;
                _count = 0;
            }

            if (_count == _items.Length)
            {
                _items[_start] = snapshot;
                _start = (_start + 1) % _items.Length;
            }
            else
            {
                _items[(_start + _count) % _items.Length] = snapshot;
                _count++;
            }

            return reset ? SnapshotAddResult.Reset : SnapshotAddResult.Added;
        }

        public void Clear()
        {
            _start = 0;
            _count = 0;
        }
    }
}
