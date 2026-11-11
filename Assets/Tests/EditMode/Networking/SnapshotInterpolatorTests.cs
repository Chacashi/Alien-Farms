using AlienFarms.Networking.Player;
using AlienFarms.Networking.Prediction;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Tests
{
    /// <summary>Snapshot ring buffer, interpolation at a render time, teleport epochs and snapshot serialization.</summary>
    public sealed class SnapshotInterpolatorTests
    {
        private const double Step = 0.02d;

        [Test]
        public void Buffer_FirstSnapshotResetsThenAppends_AndDropsOutOfOrder()
        {
            var buffer = new SnapshotBuffer(8);

            Assert.That(buffer.Add(Snapshot(2, Vector3.zero)), Is.EqualTo(SnapshotAddResult.Reset));
            Assert.That(buffer.Add(Snapshot(1, Vector3.zero)), Is.EqualTo(SnapshotAddResult.Rejected));
            Assert.That(buffer.Add(Snapshot(2, Vector3.zero)), Is.EqualTo(SnapshotAddResult.Rejected));
            Assert.That(buffer.Add(Snapshot(3, Vector3.zero)), Is.EqualTo(SnapshotAddResult.Added));
            Assert.That(buffer.Count, Is.EqualTo(2));
        }

        [Test]
        public void Buffer_OverwritesOldestWhenFull()
        {
            var buffer = new SnapshotBuffer(4);
            for (uint tick = 1; tick <= 6; tick++)
            {
                buffer.Add(Snapshot(tick, Vector3.zero));
            }

            Assert.That(buffer.Count, Is.EqualTo(4));
            Assert.That(buffer[0].Tick, Is.EqualTo(3u));
            Assert.That(buffer.TryGetNewest(out PlayerSnapshot newest), Is.True);
            Assert.That(newest.Tick, Is.EqualTo(6u));
        }

        [Test]
        public void Buffer_TryGetNewestOnEmptyFails()
        {
            Assert.That(new SnapshotBuffer(4).TryGetNewest(out _), Is.False);
        }

        [Test]
        public void Sample_EmptyBufferFails()
        {
            Assert.That(SnapshotInterpolator.TrySample(new SnapshotBuffer(4), 1d, out _), Is.False);
        }

        [Test]
        public void Sample_InterpolatesBetweenSurroundingSnapshots()
        {
            var buffer = new SnapshotBuffer(8);
            buffer.Add(Snapshot(1, new Vector3(0f, 0f, 0f), 350f));
            buffer.Add(Snapshot(2, new Vector3(2f, 0f, 0f), 10f));

            SnapshotInterpolator.TrySample(buffer, Time(1) + Step * 0.5d, out InterpolatedPose pose);

            Assert.That(pose.IsClamped, Is.False);
            Assert.That(pose.Position.x, Is.EqualTo(1f).Within(1e-4f));
            // Shortest way round: 350 -> 10 passes through 0.
            Assert.That(Mathf.DeltaAngle(pose.FacingYaw, 0f), Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void Sample_ClampsOutsideTheBufferedRange()
        {
            var buffer = new SnapshotBuffer(8);
            buffer.Add(Snapshot(1, Vector3.zero));
            buffer.Add(Snapshot(2, Vector3.one));

            SnapshotInterpolator.TrySample(buffer, Time(0), out InterpolatedPose before);
            SnapshotInterpolator.TrySample(buffer, Time(10), out InterpolatedPose after);

            Assert.That(before.IsClamped, Is.True);
            Assert.That(before.Position, Is.EqualTo(Vector3.zero));
            Assert.That(after.IsClamped, Is.True);
            Assert.That(after.Position, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void NewEpoch_ClearsHistorySoNothingSlides()
        {
            var buffer = new SnapshotBuffer(8);
            buffer.Add(Snapshot(1, Vector3.zero));
            buffer.Add(Snapshot(2, Vector3.zero));

            SnapshotAddResult result = buffer.Add(Snapshot(3, new Vector3(50f, 0f, 0f), 0f, 1));
            SnapshotInterpolator.TrySample(buffer, Time(2), out InterpolatedPose pose);

            Assert.That(result, Is.EqualTo(SnapshotAddResult.Reset));
            Assert.That(buffer.Count, Is.EqualTo(1));
            Assert.That(pose.Position.x, Is.EqualTo(50f));
        }

        [Test]
        public void LostTeleportSnapshot_IsDetectedByTheNextOne()
        {
            var buffer = new SnapshotBuffer(8);
            buffer.Add(Snapshot(1, Vector3.zero));
            buffer.Add(Snapshot(2, Vector3.zero));

            // Tick 3 started epoch 1 and was lost; tick 4 still carries epoch 1.
            SnapshotAddResult result = buffer.Add(Snapshot(4, new Vector3(50f, 0f, 0f), 0f, 1));
            SnapshotInterpolator.TrySample(buffer, Time(3), out InterpolatedPose pose);

            Assert.That(result, Is.EqualTo(SnapshotAddResult.Reset));
            Assert.That(buffer.Count, Is.EqualTo(1));
            Assert.That(pose.Position.x, Is.EqualTo(50f));
            Assert.That(buffer.Add(Snapshot(5, new Vector3(51f, 0f, 0f), 0f, 1)), Is.EqualTo(SnapshotAddResult.Added));
        }

        [Test]
        public void EpochWrapAround_StillCountsAsAChange()
        {
            var buffer = new SnapshotBuffer(8);
            buffer.Add(Snapshot(1, Vector3.zero, 0f, byte.MaxValue));

            Assert.That(buffer.Add(Snapshot(2, Vector3.one, 0f, 0)), Is.EqualTo(SnapshotAddResult.Reset));
        }

        [Test]
        public void Snapshot_RoundTripsThroughSerialization()
        {
            PlayerSnapshot snapshot = Snapshot(42, new Vector3(1.5f, 2f, -3f), 123f, 7);
            snapshot.Velocity = new Vector3(4f, 0f, 1f);
            snapshot.StunRemaining = 0.75f;
            snapshot.AckSeq = 99;
            snapshot.Flags = PlayerSnapshotFlags.Sprinting | PlayerSnapshotFlags.Aiming;

            PlayerSnapshot result;
            using (var writer = new FastBufferWriter(256, Allocator.Temp))
            {
                writer.WriteNetworkSerializable(snapshot);
                using (var reader = new FastBufferReader(writer.ToArray(), Allocator.Temp))
                {
                    reader.ReadNetworkSerializable(out result);
                }
            }

            Assert.That(result.Tick, Is.EqualTo(42u));
            Assert.That(result.ServerTime, Is.EqualTo(snapshot.ServerTime));
            Assert.That(result.Position, Is.EqualTo(snapshot.Position));
            Assert.That(result.Velocity, Is.EqualTo(snapshot.Velocity));
            Assert.That(result.FacingYaw, Is.EqualTo(123f));
            Assert.That(result.StunRemaining, Is.EqualTo(0.75f));
            Assert.That(result.AckSeq, Is.EqualTo(99u));
            Assert.That(result.TeleportEpoch, Is.EqualTo(7));
            Assert.That(result.Flags, Is.EqualTo(snapshot.Flags));
        }

        private static double Time(uint tick) => tick * Step;

        private static PlayerSnapshot Snapshot(uint tick, Vector3 position, float yaw = 0f, byte epoch = 0)
        {
            return new PlayerSnapshot { Tick = tick, ServerTime = Time(tick), Position = position, FacingYaw = yaw, TeleportEpoch = epoch };
        }
    }
}
