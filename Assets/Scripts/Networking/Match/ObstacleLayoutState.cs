using System;
using AlienFarms.Zones;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Match
{
    /// <summary>
    /// The obstacle layout of the Animal Zone, one packed slot per <see cref="ObstaclePlacement"/> (slot order):
    /// bits 10-15 = variant + 1 (0 = empty slot), bits 0-9 = yaw in 1/1024 turns (~0.35 deg). Fixed size, no allocations.
    /// </summary>
    public struct ObstacleLayoutState : INetworkSerializable, IEquatable<ObstacleLayoutState>
    {
        /// <summary>Most slots a layout can carry.</summary>
        public const int Capacity = 31;

        private const int YawBits = 10;
        private const int YawSteps = 1 << YawBits;
        private const int YawMask = YawSteps - 1;
        private const int MaxVariant = (ushort.MaxValue >> YawBits) - 1;
        private const float FullTurn = 360f;

        private FixedList64Bytes<ushort> _slots;

        public int Count => _slots.Length;

        /// <summary>Packs the first <paramref name="count"/> placements (at most <see cref="Capacity"/>).</summary>
        public static ObstacleLayoutState FromPlacements(ObstaclePlacement[] placements, int count)
        {
            var state = new ObstacleLayoutState();
            if (placements == null)
            {
                return state;
            }

            int length = Mathf.Min(Mathf.Min(count, placements.Length), Capacity);
            for (int i = 0; i < length; i++)
            {
                state._slots.Add(Pack(placements[i]));
            }

            return state;
        }

        /// <summary>Unpacks into <paramref name="destination"/>; returns the slots written (0 if it is too short).</summary>
        public int CopyTo(ObstaclePlacement[] destination)
        {
            int length = _slots.Length;
            if (destination == null || destination.Length < length)
            {
                return 0;
            }

            for (int i = 0; i < length; i++)
            {
                destination[i] = Unpack(_slots[i]);
            }

            return length;
        }

        public static ushort Pack(ObstaclePlacement placement)
        {
            if (!placement.IsActive || placement.Variant > MaxVariant)
            {
                return 0;
            }

            int yaw = Mathf.RoundToInt(Mathf.Repeat(placement.Yaw, FullTurn) / FullTurn * YawSteps) & YawMask;
            return (ushort)(((placement.Variant + 1) << YawBits) | yaw);
        }

        public static ObstaclePlacement Unpack(ushort packed)
        {
            var placement = new ObstaclePlacement();
            int code = packed >> YawBits;
            if (code == 0)
            {
                placement.Clear();
                return placement;
            }

            placement.Variant = code - 1;
            placement.Yaw = (packed & YawMask) * (FullTurn / YawSteps);
            return placement;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            byte count = (byte)_slots.Length;
            serializer.SerializeValue(ref count);

            if (serializer.IsReader)
            {
                _slots.Clear();
                for (int i = 0; i < count; i++)
                {
                    ushort value = 0;
                    serializer.SerializeValue(ref value);
                    if (i < Capacity)
                    {
                        _slots.Add(value);
                    }
                }

                return;
            }

            for (int i = 0; i < count; i++)
            {
                ushort value = _slots[i];
                serializer.SerializeValue(ref value);
            }
        }

        public bool Equals(ObstacleLayoutState other)
        {
            if (_slots.Length != other._slots.Length)
            {
                return false;
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != other._slots[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj) => obj is ObstacleLayoutState other && Equals(other);

        public override int GetHashCode()
        {
            int hash = _slots.Length;
            for (int i = 0; i < _slots.Length; i++)
            {
                hash = (hash * 31) + _slots[i];
            }

            return hash;
        }
    }
}
