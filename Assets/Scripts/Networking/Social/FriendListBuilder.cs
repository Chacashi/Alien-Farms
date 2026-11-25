using System;
using System.Collections.Generic;
using AlienFarms.Core;

namespace AlienFarms.Networking.Social
{
    public static class FriendListBuilder
    {
        /// <summary>Writes the highest-ranked friends into the destination, up to its capacity.</summary>
        public static int Build(IReadOnlyList<FriendEntry> source, FriendEntry[] destination)
        {
            int count = 0;
            int capacity = destination.Length;
            if (source != null && capacity > 0)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    FriendEntry entry = source[i];
                    if (string.IsNullOrWhiteSpace(entry.UgsPlayerId))
                    {
                        continue;
                    }

                    // Keep only the best entries as we scan, including when the input exceeds capacity.
                    int index = Math.Min(count, capacity - 1);
                    if (count == capacity && Compare(entry, destination[index]) >= 0)
                    {
                        continue;
                    }

                    while (index > 0 && Compare(entry, destination[index - 1]) < 0)
                    {
                        destination[index] = destination[index - 1];
                        index--;
                    }

                    destination[index] = entry;
                    count = Math.Min(count + 1, capacity);
                }
            }

            Array.Clear(destination, count, capacity - count);
            return count;
        }

        public static int BuildRequests(IReadOnlyList<FriendRequestEntry> incoming,
            IReadOnlyList<FriendRequestEntry> outgoing, FriendRequestEntry[] destination)
        {
            int count = CopyRequests(incoming, destination, 0);
            count = CopyRequests(outgoing, destination, count);
            Array.Clear(destination, count, destination.Length - count);
            return count;
        }

        private static int CopyRequests(IReadOnlyList<FriendRequestEntry> source, FriendRequestEntry[] destination, int count)
        {
            if (source == null)
            {
                return count;
            }

            for (int i = 0; i < source.Count && count < destination.Length; i++)
            {
                FriendRequestEntry entry = source[i];
                if (!string.IsNullOrWhiteSpace(entry.UgsPlayerId))
                {
                    destination[count++] = entry;
                }
            }

            return count;
        }

        private static int Compare(FriendEntry left, FriendEntry right)
        {
            int byAvailability = Rank(left.Availability).CompareTo(Rank(right.Availability));
            if (byAvailability != 0)
            {
                return byAvailability;
            }

            int byName = StringComparer.OrdinalIgnoreCase.Compare(
                string.IsNullOrWhiteSpace(left.DisplayName) ? left.UgsPlayerId : left.DisplayName,
                string.IsNullOrWhiteSpace(right.DisplayName) ? right.UgsPlayerId : right.DisplayName);
            return byName != 0 ? byName : StringComparer.Ordinal.Compare(left.UgsPlayerId, right.UgsPlayerId);
        }

        private static int Rank(FriendAvailability availability) => availability switch
        {
            FriendAvailability.Online => 0,
            FriendAvailability.Busy => 1,
            FriendAvailability.Away => 2,
            _ => 3
        };
    }
}
