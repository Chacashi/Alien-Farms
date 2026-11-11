using AlienFarms.Animals;
using AlienFarms.Core;
using AlienFarms.Items;
using AlienFarms.Networking.Animals;
using AlienFarms.Networking.Items;
using AlienFarms.Networking.Match;
using AlienFarms.Zones;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode;

namespace AlienFarms.Networking.Tests
{
    /// <summary>Serialization and equality of the replicated gameplay states (animals, bombs, score, obstacles, pens).</summary>
    public sealed class ReplicatedStateTests
    {
        [Test]
        public void AnimalNetState_RoundTrips()
        {
            var state = new AnimalNetState(AnimalState.Carried, PlayerId.Player2, PlayerId.None, DropReason.Thrown);

            AnimalNetState result = RoundTrip(state);

            Assert.That(result.State, Is.EqualTo(AnimalState.Carried));
            Assert.That(result.Holder, Is.EqualTo(PlayerId.Player2));
            Assert.That(result.ZoneOwner, Is.EqualTo(PlayerId.None));
            Assert.That(result.LastDrop, Is.EqualTo(DropReason.Thrown));
            Assert.That(result, Is.EqualTo(state));
        }

        [Test]
        public void AnimalNetState_EqualityCoversEveryField()
        {
            var a = new AnimalNetState(AnimalState.Captured, PlayerId.None, PlayerId.Player1, DropReason.Released);

            Assert.That(a.Equals(new AnimalNetState(AnimalState.Captured, PlayerId.None, PlayerId.Player1, DropReason.Released)), Is.True);
            Assert.That(a.Equals(new AnimalNetState(AnimalState.Captured, PlayerId.None, PlayerId.Player2, DropReason.Released)), Is.False);
            Assert.That(a.Equals(new AnimalNetState(AnimalState.Captured, PlayerId.None, PlayerId.Player1, DropReason.Thrown)), Is.False);
            Assert.That(a.Equals(new AnimalNetState(AnimalState.Wandering, PlayerId.None, PlayerId.Player1, DropReason.Released)), Is.False);
            Assert.That(default(AnimalNetState).State, Is.EqualTo(AnimalState.Inactive));
        }

        [Test]
        public void BombNetState_RoundTrips()
        {
            var state = new BombNetState(BombState.Armed, PlayerId.None, PlayerId.Player1);

            BombNetState result = RoundTrip(state);

            Assert.That(result.State, Is.EqualTo(BombState.Armed));
            Assert.That(result.Holder, Is.EqualTo(PlayerId.None));
            Assert.That(result.Thrower, Is.EqualTo(PlayerId.Player1));
            Assert.That(result, Is.EqualTo(state));
            Assert.That(result.Equals(new BombNetState(BombState.Armed, PlayerId.None, PlayerId.Player2)), Is.False);
        }

        [Test]
        public void ScoreBoard_RoundTripsAndUpdatesOnePlayer()
        {
            ScoreBoard board = new ScoreBoard(3, 7).With(PlayerId.Player1, 12).With(PlayerId.None, 99);

            ScoreBoard result = RoundTrip(board);

            Assert.That(result.Player1, Is.EqualTo(12));
            Assert.That(result.Player2, Is.EqualTo(7));
            Assert.That(result.Get(PlayerId.Player2), Is.EqualTo(7));
            Assert.That(result.Get(PlayerId.None), Is.EqualTo(0));
            Assert.That(result, Is.EqualTo(board));
        }

        [Test]
        public void ScoreBoard_ResultMatchesTheMatchRule()
        {
            MatchResult win = new ScoreBoard(5, 2).ToResult();
            MatchResult loss = new ScoreBoard(1, 4).ToResult();
            MatchResult draw = new ScoreBoard(0, 0).ToResult();

            Assert.That(win.Winner, Is.EqualTo(PlayerId.Player1));
            Assert.That(win.Player1Score, Is.EqualTo(5));
            Assert.That(win.Player2Score, Is.EqualTo(2));
            Assert.That(loss.Winner, Is.EqualTo(PlayerId.Player2));
            Assert.That(draw.IsDraw, Is.True);
        }

        [Test]
        public void ObstacleLayout_PacksVariantAndYaw()
        {
            var placements = new ObstaclePlacement[4];
            placements[0] = new ObstaclePlacement { Variant = 0, Yaw = 90f };
            placements[1].Clear();
            placements[2] = new ObstaclePlacement { Variant = 31, Yaw = 359.8f };
            placements[3] = new ObstaclePlacement { Variant = 2, Yaw = -45f };

            ObstacleLayoutState state = RoundTrip(ObstacleLayoutState.FromPlacements(placements, placements.Length));
            var output = new ObstaclePlacement[4];
            int written = state.CopyTo(output);

            Assert.That(written, Is.EqualTo(4));
            Assert.That(output[0].Variant, Is.EqualTo(0));
            Assert.That(output[0].Yaw, Is.EqualTo(90f).Within(0.36f));
            Assert.That(output[1].IsActive, Is.False);
            Assert.That(output[2].Variant, Is.EqualTo(31));
            // 359.8 deg rounds to a full turn, which wraps to 0.
            Assert.That(UnityEngine.Mathf.DeltaAngle(output[2].Yaw, 359.8f), Is.EqualTo(0f).Within(0.36f));
            Assert.That(output[3].Variant, Is.EqualTo(2));
            Assert.That(output[3].Yaw, Is.EqualTo(315f).Within(0.36f));
        }

        [Test]
        public void ObstacleLayout_EqualityAndCapacity()
        {
            var placements = new ObstaclePlacement[40];
            for (int i = 0; i < placements.Length; i++)
            {
                placements[i] = new ObstaclePlacement { Variant = i % 3, Yaw = i * 10f };
            }

            ObstacleLayoutState full = ObstacleLayoutState.FromPlacements(placements, placements.Length);
            ObstacleLayoutState same = ObstacleLayoutState.FromPlacements(placements, placements.Length);
            placements[5].Yaw += 90f;
            ObstacleLayoutState changed = ObstacleLayoutState.FromPlacements(placements, placements.Length);

            Assert.That(full.Count, Is.EqualTo(ObstacleLayoutState.Capacity));
            Assert.That(full.Equals(same), Is.True);
            Assert.That(full.Equals(changed), Is.False);
            Assert.That(full.CopyTo(new ObstaclePlacement[3]), Is.EqualTo(0));
            Assert.That(RoundTrip(full).Equals(full), Is.True);
        }

        [Test]
        public void PenEntry_RoundTripsAndClamps()
        {
            var entry = new PenEntry(2, 17);

            PenEntry result = RoundTrip(entry);

            Assert.That(result.AnimalTypeIndex, Is.EqualTo(2));
            Assert.That(result.Count, Is.EqualTo(17));
            Assert.That(result, Is.EqualTo(entry));
            Assert.That(new PenEntry(1, -3).Count, Is.EqualTo(0));
            Assert.That(new PenEntry(1, 3).Equals(new PenEntry(1, 4)), Is.False);
        }

        private static T RoundTrip<T>(T value) where T : INetworkSerializable, new()
        {
            using (var writer = new FastBufferWriter(256, Allocator.Temp))
            {
                writer.WriteNetworkSerializable(value);
                byte[] bytes = writer.ToArray();
                using (var reader = new FastBufferReader(bytes, Allocator.Temp))
                {
                    reader.ReadNetworkSerializable(out T result);
                    return result;
                }
            }
        }
    }
}
