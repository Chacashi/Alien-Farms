using System;
using AlienFarms.Utilities;
using UnityEngine;

namespace AlienFarms.Zones
{
    /// <summary>
    /// Pure obstacle layout picker (no Unity objects, no allocations after construction, EditMode-testable).
    /// Decides which slots get an obstacle, which variant and which yaw. Mirror pairs are placed as one unit
    /// (both or none, same variant, mirrored yaw) so the layout is fair for both players.
    /// </summary>
    public sealed class ObstacleLayoutPlanner
    {
        private const float FullTurn = 360f;
        private const int MaxVariants = 32;

        // Unit = the lower slot index of a mirror pair, or a single slot.
        private readonly int[] _units;

        public ObstacleLayoutPlanner(int slotCount)
        {
            if (slotCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slotCount));
            }

            _units = new int[slotCount];
        }

        /// <summary>Slots this planner was built for (upper bound of <c>slots.Length</c>).</summary>
        public int Capacity => _units.Length;

        /// <summary>
        /// Fills <paramref name="output"/> (one entry per slot; Variant -1 = empty) and returns how many slots got an
        /// obstacle. The target count is rolled in [<paramref name="minActive"/>, <paramref name="maxActive"/>] (clamped to
        /// the slot count); the result may stay below the minimum when blocked slots, spacing or masks make it impossible.
        /// Mirror slot <c>j</c> of slot <c>i</c> counts as a pair only when <c>slots[j].Mirror == i</c>.
        /// </summary>
        public int Plan(
            IRandomSource rng,
            ObstacleSlotInfo[] slots,
            ObstacleVariantRule[] variants,
            int minActive,
            int maxActive,
            float minSpacing,
            ObstaclePlacement[] output)
        {
            if (rng == null || slots == null || variants == null || output == null)
            {
                throw new ArgumentNullException(rng == null ? nameof(rng) : slots == null ? nameof(slots) : variants == null ? nameof(variants) : nameof(output));
            }

            int slotCount = slots.Length;
            if (slotCount > _units.Length || output.Length < slotCount)
            {
                throw new ArgumentException($"Planner capacity {_units.Length} / output {output.Length} too small for {slotCount} slots.");
            }

            for (int i = 0; i < slotCount; i++)
            {
                output[i].Clear();
            }

            int min = Mathf.Clamp(minActive, 0, slotCount);
            int max = Mathf.Clamp(Mathf.Max(maxActive, min), 0, slotCount);
            int target = rng.Range(min, max + 1);
            if (target <= 0)
            {
                return 0;
            }

            int unitCount = BuildUnits(slots);
            Shuffle(rng, unitCount);

            float minSpacingSqr = minSpacing > 0f ? minSpacing * minSpacing : 0f;
            int variantCount = Mathf.Min(variants.Length, MaxVariants);
            int placed = 0;

            // Pass 1 keeps parity: a single is only taken while the remainder is odd, so singles never use up the
            // room an odd target needs (pairs fill even remainders).
            for (int u = 0; u < unitCount && placed < target; u++)
            {
                int a = _units[u];
                bool isSingle = PartnerOf(slots, a) < 0;
                if (isSingle && ((target - placed) & 1) == 0)
                {
                    continue;
                }

                placed += TryPlace(rng, slots, variants, variantCount, minSpacingSqr, output, a, target - placed);
            }

            // Pass 2: fill whatever is left with any unit that still fits (e.g. not enough pairs for an even remainder).
            for (int u = 0; u < unitCount && placed < target; u++)
            {
                int a = _units[u];
                if (!output[a].IsActive)
                {
                    placed += TryPlace(rng, slots, variants, variantCount, minSpacingSqr, output, a, target - placed);
                }
            }

            return placed;
        }

        /// <summary>Places the unit starting at slot <paramref name="a"/> if it fits; returns the slots used (0, 1 or 2).</summary>
        private static int TryPlace(
            IRandomSource rng,
            ObstacleSlotInfo[] slots,
            ObstacleVariantRule[] variants,
            int variantCount,
            float minSpacingSqr,
            ObstaclePlacement[] output,
            int a,
            int remaining)
        {
            int slotCount = slots.Length;
            int b = PartnerOf(slots, a);
            int size = b >= 0 ? 2 : 1;

            // A pair that would overflow the target is skipped; singles further on may still fit.
            if (size > remaining)
            {
                return 0;
            }

            if (slots[a].Blocked || (b >= 0 && slots[b].Blocked))
            {
                return 0;
            }

            if (!IsSpaced(slots, output, slotCount, a, minSpacingSqr)
                || (b >= 0 && (!IsSpaced(slots, output, slotCount, b, minSpacingSqr)
                    || (slots[a].Position - slots[b].Position).sqrMagnitude < minSpacingSqr)))
            {
                return 0;
            }

            uint mask = slots[a].VariantMask;
            if (b >= 0)
            {
                mask &= slots[b].VariantMask;
            }

            // Chiral variants would not be a true mirror image on the paired slot.
            int variant = PickVariant(rng, variants, variantCount, mask, b >= 0);
            if (variant < 0)
            {
                return 0;
            }

            float[] yaws = variants[variant].Yaws;
            float yaw = yaws != null && yaws.Length > 0 ? yaws[rng.Range(0, yaws.Length)] : 0f;

            output[a].Variant = variant;
            output[a].Yaw = yaw;
            if (b >= 0)
            {
                output[b].Variant = variant;
                output[b].Yaw = Mathf.Repeat(-yaw, FullTurn);
            }

            return size;
        }

        private int BuildUnits(ObstacleSlotInfo[] slots)
        {
            int count = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                int partner = PartnerOf(slots, i);
                // Each pair is listed once, through its lower index.
                if (partner < 0 || partner > i)
                {
                    _units[count++] = i;
                }
            }

            return count;
        }

        /// <summary>Reciprocal mirror of <paramref name="index"/>, or -1 when it is a single slot.</summary>
        private static int PartnerOf(ObstacleSlotInfo[] slots, int index)
        {
            int mirror = slots[index].Mirror;
            if (mirror < 0 || mirror == index || mirror >= slots.Length || slots[mirror].Mirror != index)
            {
                return -1;
            }

            return mirror;
        }

        private void Shuffle(IRandomSource rng, int count)
        {
            // Fisher-Yates.
            for (int i = count - 1; i > 0; i--)
            {
                int j = rng.Range(0, i + 1);
                int temp = _units[i];
                _units[i] = _units[j];
                _units[j] = temp;
            }
        }

        private static bool IsSpaced(ObstacleSlotInfo[] slots, ObstaclePlacement[] output, int slotCount, int index, float minSpacingSqr)
        {
            if (minSpacingSqr <= 0f)
            {
                return true;
            }

            Vector2 position = slots[index].Position;
            for (int i = 0; i < slotCount; i++)
            {
                if (output[i].IsActive && (slots[i].Position - position).sqrMagnitude < minSpacingSqr)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Weighted pick among the variants in <paramref name="mask"/> with weight &gt; 0 (mirror-symmetric only for a pair); -1 if none.</summary>
        private static int PickVariant(IRandomSource rng, ObstacleVariantRule[] variants, int variantCount, uint mask, bool forPair)
        {
            if (forPair)
            {
                for (int i = 0; i < variantCount; i++)
                {
                    if (!variants[i].IsMirrorSymmetric)
                    {
                        mask &= ~(1u << i);
                    }
                }
            }

            int total = 0;
            for (int i = 0; i < variantCount; i++)
            {
                if ((mask & (1u << i)) != 0 && variants[i].Weight > 0)
                {
                    total += variants[i].Weight;
                }
            }

            if (total <= 0)
            {
                return -1;
            }

            int roll = rng.Range(0, total);
            for (int i = 0; i < variantCount; i++)
            {
                if ((mask & (1u << i)) == 0 || variants[i].Weight <= 0)
                {
                    continue;
                }

                roll -= variants[i].Weight;
                if (roll < 0)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
