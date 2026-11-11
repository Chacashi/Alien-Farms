using AlienFarms.Networking.Player;
using AlienFarms.Networking.Prediction;
using NUnit.Framework;
using UnityEngine;

namespace AlienFarms.Networking.Tests
{
    /// <summary>Snapshot ring buffer edges: capacity floor, ordering after wrap, rejects, clear and epoch resets.</summary>
    public sealed class SnapshotBufferTests
    {
        private const double Step = 0.02d;

        [TestCase(-5)]
        [TestCase(0)]
        [TestCase(1)]
        public void Constructor_ClampsCapacityToTwo(int capacity)
        {
            var buffer = new SnapshotBuffer(capacity);

            Assert.That(buffer.Capacity, Is.EqualTo(2));
            Assert.That(buffer.Count, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_DefaultCapacity()
        {
            Assert.That(new SnapshotBuffer().Capacity, Is.EqualTo(SnapshotBuffer.DefaultCapacity));
        }

        [Test]
        public void Indexer_StaysOldestFirstAndStrictlyIncreasingAfterManyWraps()
        {
            var buffer = new SnapshotBuffer(5);
            for (uint tick = 1; tick <= 23; tick++)
            {
                buffer.Add(Snapshot(tick));
            }

            Assert.That(buffer.Count, Is.EqualTo(5));
            for (int i = 0; i < buffer.Count; i++)
            {
                Assert.That(buffer[i].Tick, Is.EqualTo(19u + (uint)i));
            }
        }

        [Test]
        public void Add_AcceptsTickGaps()
        {
            var buffer = new SnapshotBuffer(4);
            buffer.Add(Snapshot(1));

            Assert.That(buffer.Add(Snapshot(10)), Is.EqualTo(SnapshotAddResult.Added));
            Assert.That(buffer[1].Tick, Is.EqualTo(10u));
        }

        [Test]
        public void Add_RejectedDoesNotMutateTheBuffer()
        {
            var buffer = new SnapshotBuffer(4);
            buffer.Add(Snapshot(5, 1f));
            buffer.Add(Snapshot(6, 2f));

            Assert.That(buffer.Add(Snapshot(6, 99f)), Is.EqualTo(SnapshotAddResult.Rejected));
            Assert.That(buffer.Add(Snapshot(3, 99f)), Is.EqualTo(SnapshotAddResult.Rejected));

            Assert.That(buffer.Count, Is.EqualTo(2));
            Assert.That(buffer[0].Position.x, Is.EqualTo(1f));
            Assert.That(buffer[1].Position.x, Is.EqualTo(2f));
        }

        [Test]
        public void Add_RejectedWhileFullKeepsTheRing()
        {
            var buffer = new SnapshotBuffer(3);
            for (uint tick = 1; tick <= 4; tick++)
            {
                buffer.Add(Snapshot(tick));
            }

            Assert.That(buffer.Add(Snapshot(2)), Is.EqualTo(SnapshotAddResult.Rejected));
            Assert.That(buffer.Count, Is.EqualTo(3));
            Assert.That(buffer[0].Tick, Is.EqualTo(2u));
            Assert.That(buffer[2].Tick, Is.EqualTo(4u));
        }

        [Test]
        public void Add_LateSnapshotFromOldEpochIsRejectedAndDoesNotReset()
        {
            var buffer = new SnapshotBuffer(4);
            buffer.Add(Snapshot(1, 0f, 0));
            buffer.Add(Snapshot(3, 50f, 1));

            // Tick 2 (pre-teleport epoch) arrives after the teleport: older tick wins, no reset back.
            Assert.That(buffer.Add(Snapshot(2, 0f, 0)), Is.EqualTo(SnapshotAddResult.Rejected));
            Assert.That(buffer.Count, Is.EqualTo(1));
            Assert.That(buffer[0].TeleportEpoch, Is.EqualTo(1));
        }

        [Test]
        public void Add_EpochResetAfterWrapRestartsAtIndexZero()
        {
            var buffer = new SnapshotBuffer(3);
            for (uint tick = 1; tick <= 7; tick++)
            {
                buffer.Add(Snapshot(tick));
            }

            Assert.That(buffer.Add(Snapshot(8, 9f, 2)), Is.EqualTo(SnapshotAddResult.Reset));
            Assert.That(buffer.Add(Snapshot(9, 10f, 2)), Is.EqualTo(SnapshotAddResult.Added));

            Assert.That(buffer.Count, Is.EqualTo(2));
            Assert.That(buffer[0].Tick, Is.EqualTo(8u));
            Assert.That(buffer[1].Tick, Is.EqualTo(9u));
            Assert.That(buffer.TryGetNewest(out PlayerSnapshot newest), Is.True);
            Assert.That(newest.Position.x, Is.EqualTo(10f));
        }

        [Test]
        public void Clear_EmptiesAndNextAddIsAReset()
        {
            var buffer = new SnapshotBuffer(4);
            buffer.Add(Snapshot(10));
            buffer.Add(Snapshot(11));

            buffer.Clear();

            Assert.That(buffer.Count, Is.EqualTo(0));
            Assert.That(buffer.TryGetNewest(out _), Is.False);
            Assert.That(SnapshotInterpolator.TrySample(buffer, 1d, out _), Is.False);
            // History is gone, so even an older tick is accepted as a fresh start.
            Assert.That(buffer.Add(Snapshot(3)), Is.EqualTo(SnapshotAddResult.Reset));
            Assert.That(buffer.Count, Is.EqualTo(1));
        }

        [Test]
        public void Clear_AfterWrapRestoresFullCapacity()
        {
            var buffer = new SnapshotBuffer(3);
            for (uint tick = 1; tick <= 5; tick++)
            {
                buffer.Add(Snapshot(tick));
            }

            buffer.Clear();
            for (uint tick = 20; tick <= 22; tick++)
            {
                buffer.Add(Snapshot(tick));
            }

            Assert.That(buffer.Count, Is.EqualTo(3));
            Assert.That(buffer[0].Tick, Is.EqualTo(20u));
            Assert.That(buffer[2].Tick, Is.EqualTo(22u));
        }

        [Test]
        public void TryGetNewest_TracksTheLastAcceptedSnapshot()
        {
            var buffer = new SnapshotBuffer(2);
            buffer.Add(Snapshot(1));
            buffer.Add(Snapshot(2));
            buffer.Add(Snapshot(3));
            buffer.Add(Snapshot(1));

            Assert.That(buffer.TryGetNewest(out PlayerSnapshot newest), Is.True);
            Assert.That(newest.Tick, Is.EqualTo(3u));
            Assert.That(buffer[0].Tick, Is.EqualTo(2u));
        }

        private static PlayerSnapshot Snapshot(uint tick, float x = 0f, byte epoch = 0)
        {
            return new PlayerSnapshot
            {
                Tick = tick,
                ServerTime = tick * Step,
                Position = new Vector3(x, 0f, 0f),
                TeleportEpoch = epoch
            };
        }
    }
}
