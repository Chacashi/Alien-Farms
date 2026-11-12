using System;

namespace AlienFarms.Core
{
    /// <summary>
    /// Last pen inventory count raised per player and animal type, so a view enabled late (or re-enabled) can catch up
    /// without waiting for the next <see cref="GameEvents.OnPenInventoryChanged"/>. Written only by
    /// <see cref="GameEvents.RaisePenInventoryChanged"/> and cleared with the rest of the game event statics.
    /// </summary>
    public static class PenInventorySnapshot
    {
        /// <summary>Type indices at or above this value are ignored.</summary>
        public const int MaxTypeCount = byte.MaxValue;

        private const int InitialTypeCapacity = 8;
        private const int PlayerSlots = (int)PlayerId.Player2 + 1;

        private static readonly int[][] Counts = CreateCounts();

        /// <summary>Last count cached for the player and type, or 0 when nothing was raised for it.</summary>
        public static int Get(PlayerId playerId, int typeIndex)
        {
            int player = (int)playerId;
            if (player <= (int)PlayerId.None || player >= PlayerSlots || typeIndex < 0)
            {
                return 0;
            }

            int[] counts = Counts[player];
            return typeIndex < counts.Length ? counts[typeIndex] : 0;
        }

        /// <summary>Number of type slots currently cached for the player (indices beyond it read as 0).</summary>
        internal static int TypeCapacity(PlayerId playerId)
        {
            int player = (int)playerId;
            return player > (int)PlayerId.None && player < PlayerSlots ? Counts[player].Length : 0;
        }

        internal static void Set(PlayerId playerId, int typeIndex, int count)
        {
            int player = (int)playerId;
            if (player <= (int)PlayerId.None || player >= PlayerSlots || typeIndex < 0 || typeIndex >= MaxTypeCount)
            {
                return;
            }

            int[] counts = Counts[player];
            if (typeIndex >= counts.Length)
            {
                // Rare: only when a new, higher type index shows up.
                int newLength = Math.Min(MaxTypeCount, Math.Max(counts.Length * 2, typeIndex + 1));
                Array.Resize(ref counts, newLength);
                Counts[player] = counts;
            }

            counts[typeIndex] = count < 0 ? 0 : count;
        }

        internal static void Reset()
        {
            for (int i = 0; i < Counts.Length; i++)
            {
                Array.Clear(Counts[i], 0, Counts[i].Length);
            }
        }

        private static int[][] CreateCounts()
        {
            var counts = new int[PlayerSlots][];
            for (int i = 0; i < counts.Length; i++)
            {
                counts[i] = new int[InitialTypeCapacity];
            }

            return counts;
        }
    }
}
