using AlienFarms.Utilities;
using AlienFarms.Zones;
using NUnit.Framework;
using UnityEngine;

namespace AlienFarms.Tests
{
    public sealed class ObstacleLayoutPlannerTests
    {
        private const uint AllVariants = 0b1111u;
        private const float Spacing = 5.5f;
        private const float AngleTolerance = 0.001f;

        // The prototype layout: 6 mirror pairs (x -> -x) + 2 singles on the center line.
        private static readonly Vector2[] s_prototypePositions =
        {
            new Vector2(-13f, 8.5f), new Vector2(13f, 8.5f),
            new Vector2(-6f, 9.5f), new Vector2(6f, 9.5f),
            new Vector2(-13.5f, 0f), new Vector2(13.5f, 0f),
            new Vector2(-6.5f, 0f), new Vector2(6.5f, 0f),
            new Vector2(-13f, -8.5f), new Vector2(13f, -8.5f),
            new Vector2(-6f, -9.5f), new Vector2(6f, -9.5f),
            new Vector2(0f, 9.5f),
            new Vector2(0f, -9.5f),
        };

        private static ObstacleVariantRule[] DefaultRules() => new[]
        {
            new ObstacleVariantRule(4, new[] { 0f, 45f, 90f, 135f }),
            new ObstacleVariantRule(2, new[] { 0f, 45f, 90f, 135f }),
            new ObstacleVariantRule(2, new[] { 0f, 90f, 180f, 270f }),
            new ObstacleVariantRule(1, new[] { 0f }),
        };

        private static ObstacleSlotInfo[] PrototypeSlots(uint mask = AllVariants)
        {
            var slots = new ObstacleSlotInfo[s_prototypePositions.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                int mirror = i < 12 ? (i % 2 == 0 ? i + 1 : i - 1) : -1;
                slots[i] = new ObstacleSlotInfo(s_prototypePositions[i], mirror, mask);
            }

            return slots;
        }

        private static ObstacleSlotInfo[] Singles(int count, float step, uint mask = AllVariants)
        {
            var slots = new ObstacleSlotInfo[count];
            for (int i = 0; i < count; i++)
            {
                slots[i] = new ObstacleSlotInfo(new Vector2(i * step, 0f), -1, mask);
            }

            return slots;
        }

        private static int Run(int seed, ObstacleSlotInfo[] slots, ObstacleVariantRule[] rules, int min, int max, float spacing, ObstaclePlacement[] output)
        {
            var planner = new ObstacleLayoutPlanner(slots.Length);
            return planner.Plan(new SystemRandomSource(seed), slots, rules, min, max, spacing, output);
        }

        private static int CountActive(ObstaclePlacement[] output)
        {
            int count = 0;
            for (int i = 0; i < output.Length; i++)
            {
                if (output[i].IsActive)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool ContainsAngle(float[] set, float angle)
        {
            for (int i = 0; i < set.Length; i++)
            {
                if (Mathf.Abs(Mathf.DeltaAngle(set[i], angle)) < AngleTolerance)
                {
                    return true;
                }
            }

            return false;
        }

        [Test]
        public void SameSeed_ProducesSameLayoutSequence()
        {
            ObstacleSlotInfo[] slots = PrototypeSlots();
            ObstacleVariantRule[] rules = DefaultRules();
            var plannerA = new ObstacleLayoutPlanner(slots.Length);
            var plannerB = new ObstacleLayoutPlanner(slots.Length);
            var rngA = new SystemRandomSource(42);
            var rngB = new SystemRandomSource(42);
            var a = new ObstaclePlacement[slots.Length];
            var b = new ObstaclePlacement[slots.Length];

            for (int round = 0; round < 5; round++)
            {
                int placedA = plannerA.Plan(rngA, slots, rules, 4, 8, Spacing, a);
                int placedB = plannerB.Plan(rngB, slots, rules, 4, 8, Spacing, b);

                Assert.That(placedA, Is.EqualTo(placedB));
                for (int i = 0; i < slots.Length; i++)
                {
                    Assert.That(a[i].Variant, Is.EqualTo(b[i].Variant));
                    Assert.That(a[i].Yaw, Is.EqualTo(b[i].Yaw));
                }
            }
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentLayouts()
        {
            ObstacleSlotInfo[] slots = PrototypeSlots();
            ObstacleVariantRule[] rules = DefaultRules();
            var reference = new ObstaclePlacement[slots.Length];
            var other = new ObstaclePlacement[slots.Length];
            Run(0, slots, rules, 4, 8, Spacing, reference);

            bool anyDifferent = false;
            for (int seed = 1; seed <= 20 && !anyDifferent; seed++)
            {
                Run(seed, slots, rules, 4, 8, Spacing, other);
                for (int i = 0; i < slots.Length; i++)
                {
                    if (other[i].Variant != reference[i].Variant || !Mathf.Approximately(other[i].Yaw, reference[i].Yaw))
                    {
                        anyDifferent = true;
                        break;
                    }
                }
            }

            Assert.That(anyDifferent, Is.True);
        }

        [Test]
        public void ActiveCount_StaysWithinMinMax()
        {
            ObstacleSlotInfo[] slots = PrototypeSlots();
            ObstacleVariantRule[] rules = DefaultRules();
            var output = new ObstaclePlacement[slots.Length];

            for (int seed = 0; seed < 200; seed++)
            {
                int placed = Run(seed, slots, rules, 4, 8, Spacing, output);

                Assert.That(placed, Is.InRange(4, 8), $"seed {seed}");
                Assert.That(CountActive(output), Is.EqualTo(placed), $"seed {seed}");
            }
        }

        [Test]
        public void MaxAboveSlotCount_IsClamped()
        {
            ObstacleSlotInfo[] slots = Singles(3, 10f);
            var output = new ObstaclePlacement[slots.Length];

            int placed = Run(1, slots, DefaultRules(), 5, 10, Spacing, output);

            Assert.That(placed, Is.EqualTo(3));
            Assert.That(CountActive(output), Is.EqualTo(3));
        }

        [Test]
        public void BlockedSlots_AreNeverActive()
        {
            ObstacleSlotInfo[] slots = PrototypeSlots();
            slots[0].Blocked = true;
            slots[13].Blocked = true;
            var output = new ObstaclePlacement[slots.Length];

            for (int seed = 0; seed < 200; seed++)
            {
                Run(seed, slots, DefaultRules(), 4, 8, Spacing, output);

                Assert.That(output[0].IsActive, Is.False, $"seed {seed}");
                // Its mirror goes with it: a pair is both or none.
                Assert.That(output[1].IsActive, Is.False, $"seed {seed}");
                Assert.That(output[13].IsActive, Is.False, $"seed {seed}");
            }
        }

        [Test]
        public void ActiveSlots_RespectMinSpacing()
        {
            // 1.5 m grid: plenty of slots closer than the spacing.
            var slots = new ObstacleSlotInfo[25];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = new ObstacleSlotInfo(new Vector2((i % 5) * 1.5f, (i / 5) * 1.5f), -1, AllVariants);
            }

            var output = new ObstaclePlacement[slots.Length];
            const float spacing = 3f;
            for (int seed = 0; seed < 100; seed++)
            {
                Run(seed, slots, DefaultRules(), 3, 10, spacing, output);
                for (int i = 0; i < slots.Length; i++)
                {
                    for (int j = i + 1; j < slots.Length; j++)
                    {
                        if (output[i].IsActive && output[j].IsActive)
                        {
                            float distance = (slots[i].Position - slots[j].Position).magnitude;
                            Assert.That(distance, Is.GreaterThanOrEqualTo(spacing), $"seed {seed}: {i}-{j}");
                        }
                    }
                }
            }
        }

        [Test]
        public void MirrorPairs_AreBothOrNone_WithSameVariantAndMirroredYaw()
        {
            ObstacleSlotInfo[] slots = PrototypeSlots();
            var output = new ObstaclePlacement[slots.Length];

            for (int seed = 0; seed < 200; seed++)
            {
                Run(seed, slots, DefaultRules(), 4, 8, Spacing, output);
                for (int i = 0; i < 12; i += 2)
                {
                    ObstaclePlacement a = output[i];
                    ObstaclePlacement b = output[i + 1];
                    Assert.That(a.IsActive, Is.EqualTo(b.IsActive), $"seed {seed}: pair {i}");
                    if (!a.IsActive)
                    {
                        continue;
                    }

                    Assert.That(b.Variant, Is.EqualTo(a.Variant), $"seed {seed}: pair {i}");
                    Assert.That(b.Yaw, Is.EqualTo(Mathf.Repeat(-a.Yaw, 360f)).Within(AngleTolerance), $"seed {seed}: pair {i}");
                }
            }
        }

        [Test]
        public void Pair_NeverOverflowsTarget()
        {
            // 4 far-apart pairs only, target fixed at 3: one pair fits, a second would overflow.
            var slots = new ObstacleSlotInfo[8];
            for (int i = 0; i < slots.Length; i++)
            {
                int mirror = i % 2 == 0 ? i + 1 : i - 1;
                slots[i] = new ObstacleSlotInfo(new Vector2((i % 2 == 0 ? -1f : 1f) * 10f, (i / 2) * 10f), mirror, AllVariants);
            }

            var output = new ObstaclePlacement[slots.Length];
            int placed = Run(1, slots, DefaultRules(), 3, 3, Spacing, output);

            Assert.That(placed, Is.EqualTo(2));
            Assert.That(CountActive(output), Is.EqualTo(2));
        }

        [Test]
        public void Pair_OverflowSkipped_SingleStillFillsTarget()
        {
            var slots = new[]
            {
                new ObstacleSlotInfo(new Vector2(-10f, 0f), 1, AllVariants),
                new ObstacleSlotInfo(new Vector2(10f, 0f), 0, AllVariants),
                new ObstacleSlotInfo(new Vector2(-10f, 10f), 3, AllVariants),
                new ObstacleSlotInfo(new Vector2(10f, 10f), 2, AllVariants),
                new ObstacleSlotInfo(new Vector2(0f, 20f), -1, AllVariants),
            };

            var output = new ObstaclePlacement[slots.Length];
            for (int seed = 0; seed < 50; seed++)
            {
                int placed = Run(seed, slots, DefaultRules(), 3, 3, Spacing, output);
                Assert.That(placed, Is.EqualTo(3), $"seed {seed}");
                Assert.That(output[4].IsActive, Is.True, $"seed {seed}");
            }
        }

        [Test]
        public void VariantOutsideMask_IsNeverChosen()
        {
            // Slots only own variants 0 and 2.
            ObstacleSlotInfo[] slots = Singles(6, 10f, 0b0101u);
            var output = new ObstaclePlacement[slots.Length];

            for (int seed = 0; seed < 200; seed++)
            {
                Run(seed, slots, DefaultRules(), 6, 6, Spacing, output);
                for (int i = 0; i < slots.Length; i++)
                {
                    Assert.That(output[i].Variant, Is.EqualTo(0).Or.EqualTo(2), $"seed {seed}");
                }
            }
        }

        [Test]
        public void MirrorPair_UsesOnlyVariantsBothSlotsOwn()
        {
            var slots = new[]
            {
                new ObstacleSlotInfo(new Vector2(-10f, 0f), 1, 0b0011u),
                new ObstacleSlotInfo(new Vector2(10f, 0f), 0, 0b0110u),
            };

            var output = new ObstaclePlacement[slots.Length];
            for (int seed = 0; seed < 100; seed++)
            {
                Run(seed, slots, DefaultRules(), 2, 2, Spacing, output);
                Assert.That(output[0].Variant, Is.EqualTo(1), $"seed {seed}");
                Assert.That(output[1].Variant, Is.EqualTo(1), $"seed {seed}");
            }
        }

        [Test]
        public void ZeroWeightVariant_IsNeverChosen()
        {
            ObstacleSlotInfo[] slots = Singles(6, 10f);
            ObstacleVariantRule[] rules = DefaultRules();
            rules[0] = new ObstacleVariantRule(0, new[] { 0f });
            var output = new ObstaclePlacement[slots.Length];

            for (int seed = 0; seed < 200; seed++)
            {
                Run(seed, slots, rules, 6, 6, Spacing, output);
                for (int i = 0; i < slots.Length; i++)
                {
                    Assert.That(output[i].Variant, Is.Not.EqualTo(0), $"seed {seed}");
                }
            }
        }

        [Test]
        public void Yaw_ComesFromAllowedSetOrItsMirror()
        {
            ObstacleSlotInfo[] slots = PrototypeSlots();
            ObstacleVariantRule[] rules = DefaultRules();
            var output = new ObstaclePlacement[slots.Length];

            for (int seed = 0; seed < 200; seed++)
            {
                Run(seed, slots, rules, 4, 8, Spacing, output);
                for (int i = 0; i < slots.Length; i++)
                {
                    if (!output[i].IsActive)
                    {
                        continue;
                    }

                    float[] allowed = rules[output[i].Variant].Yaws;
                    float yaw = output[i].Yaw;
                    // Pairs are (even, odd) for i < 12: the lower index and the singles roll from the exact set,
                    // the higher index of a pair carries the mirrored yaw.
                    bool isMirroredMember = i < 12 && i % 2 == 1;
                    float expected = isMirroredMember ? -yaw : yaw;
                    Assert.That(ContainsAngle(allowed, expected), Is.True, $"seed {seed}: slot {i} yaw {yaw}");
                }
            }
        }

        [TestCase(5)]
        [TestCase(7)]
        public void OddTarget_IsReached_WhenASingleIsAvailable(int target)
        {
            ObstacleSlotInfo[] slots = PrototypeSlots();
            var output = new ObstaclePlacement[slots.Length];

            for (int seed = 0; seed < 300; seed++)
            {
                int placed = Run(seed, slots, DefaultRules(), target, target, Spacing, output);
                Assert.That(placed, Is.EqualTo(target), $"seed {seed}");
            }
        }

        [Test]
        public void ActiveCount_Distribution_ReachesEveryTargetInRange()
        {
            ObstacleSlotInfo[] slots = PrototypeSlots();
            var output = new ObstaclePlacement[slots.Length];
            var histogram = new int[slots.Length + 1];

            for (int seed = 0; seed < 1000; seed++)
            {
                histogram[Run(seed, slots, DefaultRules(), 4, 8, Spacing, output)]++;
            }

            for (int count = 0; count < histogram.Length; count++)
            {
                if (count >= 4 && count <= 8)
                {
                    // Uniform target roll over 5 values: ~200 each.
                    Assert.That(histogram[count], Is.GreaterThan(120), $"count {count}");
                }
                else
                {
                    Assert.That(histogram[count], Is.EqualTo(0), $"count {count}");
                }
            }
        }

        [Test]
        public void OddTarget_WithoutSingles_FallsOneShort()
        {
            ObstacleSlotInfo[] slots = PrototypeSlots();
            slots[12].Blocked = true;
            slots[13].Blocked = true;
            var output = new ObstaclePlacement[slots.Length];

            for (int seed = 0; seed < 50; seed++)
            {
                Assert.That(Run(seed, slots, DefaultRules(), 5, 5, Spacing, output), Is.EqualTo(4), $"seed {seed}");
            }
        }

        [Test]
        public void ChiralVariant_IsOnlyPlacedOnSingles()
        {
            ObstacleSlotInfo[] slots = PrototypeSlots();
            ObstacleVariantRule[] rules = DefaultRules();
            rules[2] = new ObstacleVariantRule(50, rules[2].Yaws, false);
            var output = new ObstaclePlacement[slots.Length];
            bool seenOnSingle = false;

            for (int seed = 0; seed < 200; seed++)
            {
                Run(seed, slots, rules, 4, 8, Spacing, output);
                for (int i = 0; i < slots.Length; i++)
                {
                    if (output[i].Variant != 2)
                    {
                        continue;
                    }

                    Assert.That(slots[i].Mirror, Is.EqualTo(-1), $"seed {seed}: chiral variant on paired slot {i}");
                    seenOnSingle = true;
                }
            }

            Assert.That(seenOnSingle, Is.True);
        }

        [Test]
        public void EmptyYawSet_UsesZero()
        {
            ObstacleSlotInfo[] slots = Singles(1, 0f);
            var rules = new[] { new ObstacleVariantRule(1, new float[0]) };
            var output = new ObstaclePlacement[1];

            int placed = Run(3, slots, rules, 1, 1, Spacing, output);

            Assert.That(placed, Is.EqualTo(1));
            Assert.That(output[0].Yaw, Is.EqualTo(0f));
        }

        [Test]
        public void VariantPicks_FollowWeights()
        {
            ObstacleSlotInfo[] slots = Singles(1, 0f);
            ObstacleVariantRule[] rules = DefaultRules();
            var output = new ObstaclePlacement[1];
            var planner = new ObstacleLayoutPlanner(1);
            var rng = new SystemRandomSource(2024);
            var counts = new int[rules.Length];
            const int samples = 10000;

            for (int i = 0; i < samples; i++)
            {
                planner.Plan(rng, slots, rules, 1, 1, Spacing, output);
                counts[output[0].Variant]++;
            }

            const float totalWeight = 9f;
            for (int v = 0; v < rules.Length; v++)
            {
                float expected = rules[v].Weight / totalWeight;
                Assert.That(counts[v] / (float)samples, Is.EqualTo(expected).Within(0.03f), $"variant {v}");
            }
        }

        [Test]
        public void ImpossibleConstraints_ReturnBelowMinWithoutThrowing()
        {
            ObstacleSlotInfo[] slots = PrototypeSlots();
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].Blocked = i != 12;
            }

            var output = new ObstaclePlacement[slots.Length];
            int placed = 0;
            Assert.DoesNotThrow(() => placed = Run(5, slots, DefaultRules(), 4, 8, Spacing, output));
            Assert.That(placed, Is.EqualTo(1));

            // Spacing larger than the whole zone: only one obstacle fits.
            ObstacleSlotInfo[] free = Singles(5, 2f);
            var freeOutput = new ObstaclePlacement[free.Length];
            Assert.That(Run(5, free, DefaultRules(), 4, 5, 100f, freeOutput), Is.EqualTo(1));

            // No variant available at all.
            ObstacleSlotInfo[] noMask = Singles(5, 10f, 0u);
            var noMaskOutput = new ObstaclePlacement[noMask.Length];
            Assert.That(Run(5, noMask, DefaultRules(), 4, 5, Spacing, noMaskOutput), Is.EqualTo(0));
        }

        [Test]
        public void Plan_DoesNotAllocate()
        {
            ObstacleSlotInfo[] slots = PrototypeSlots();
            ObstacleVariantRule[] rules = DefaultRules();
            var output = new ObstaclePlacement[slots.Length];
            var planner = new ObstacleLayoutPlanner(slots.Length);
            IRandomSource rng = new SystemRandomSource(9);
            planner.Plan(rng, slots, rules, 4, 8, Spacing, output);

            TestDelegate plan = () => { planner.Plan(rng, slots, rules, 4, 8, Spacing, output); };
            Assert.That(
                plan,
                UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not));
        }
    }
}
