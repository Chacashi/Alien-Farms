using AlienFarms.Core;
using AlienFarms.Networking.Persistence;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests.Persistence
{
    public sealed class PendingStatsCodecTests
    {
        [Test]
        public void WriteThenRead_RoundTrips()
        {
            var pending = new PendingStats(new PlayerStatsSummary(2, 1, 0, 1, 9, 1), "batch-1");

            string json = PendingStatsCodec.Write(pending);
            StatsReadResult read = PendingStatsCodec.Read(json, out PendingStats decoded);

            Assert.That(read, Is.EqualTo(StatsReadResult.Ok));
            Assert.That(decoded, Is.EqualTo(pending));
            Assert.That(json, Does.Contain("\"v\":2"));
            Assert.That(json, Does.Contain("\"batch\":\"batch-1\""));
        }

        [Test]
        public void WriteThenRead_KeepsSentHeadAndTail()
        {
            var pending = new PendingStats(new PlayerStatsSummary(1, 1, 0, 0, 4, 0), "head", true,
                new PlayerStatsSummary(1, 0, 0, 1, 2, 0), "tail");

            StatsReadResult read = PendingStatsCodec.Read(PendingStatsCodec.Write(pending), out PendingStats decoded);

            Assert.That(read, Is.EqualTo(StatsReadResult.Ok));
            Assert.That(decoded, Is.EqualTo(pending));
            Assert.That(decoded.HeadSent, Is.True);
            Assert.That(decoded.TailBatchId, Is.EqualTo("tail"));
        }

        [Test]
        public void Read_V1_IsAnUnsentHeadWithoutTail()
        {
            const string json = "{\"v\":1,\"batch\":\"b\",\"matches\":1,\"wins\":1,\"sent\":true,\"tail\":{\"batch\":\"t\",\"matches\":1}}";

            Assert.That(PendingStatsCodec.Read(json, out PendingStats pending), Is.EqualTo(StatsReadResult.Ok));
            Assert.That(pending.HeadSent, Is.False);
            Assert.That(pending.HasTail, Is.False);
            Assert.That(pending.Delta, Is.EqualTo(new PlayerStatsSummary(1, 1, 0, 0, 0, 0)));
        }

        [Test]
        public void Write_Empty_IsNull()
        {
            Assert.That(PendingStatsCodec.Write(default), Is.Null);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Read_Blank_IsMissing(string json)
        {
            Assert.That(PendingStatsCodec.Read(json, out PendingStats pending), Is.EqualTo(StatsReadResult.Missing));
            Assert.That(pending.IsEmpty, Is.True);
        }

        [Test]
        public void Read_InvalidJson_IsCorrupt()
        {
            Assert.That(PendingStatsCodec.Read("{not json", out PendingStats pending), Is.EqualTo(StatsReadResult.Corrupt));
            Assert.That(pending.IsEmpty, Is.True);
        }

        [Test]
        public void Read_NewerVersion_IsNewerSchema()
        {
            const string json = "{\"v\":3,\"batch\":\"b\",\"matches\":1,\"wins\":1}";

            Assert.That(PendingStatsCodec.Read(json, out PendingStats pending), Is.EqualTo(StatsReadResult.NewerSchema));
            Assert.That(pending.IsEmpty, Is.True);
        }

        [Test]
        public void Read_WithoutVersion_IsV1()
        {
            const string json = "{\"batch\":\"b\",\"matches\":1,\"losses\":1}";

            Assert.That(PendingStatsCodec.Read(json, out PendingStats pending), Is.EqualTo(StatsReadResult.Ok));
            Assert.That(pending.Delta, Is.EqualTo(new PlayerStatsSummary(1, 0, 0, 1, 0, 0)));
        }

        [Test]
        public void Read_WithoutBatch_IsMissing()
        {
            Assert.That(PendingStatsCodec.Read("{\"v\":1,\"matches\":1}", out _), Is.EqualTo(StatsReadResult.Missing));
        }

        [Test]
        public void Read_Sanitizes()
        {
            const string json = "{\"v\":1,\"batch\":\"b\",\"matches\":0,\"wins\":2,\"animals\":-5}";

            PendingStatsCodec.Read(json, out PendingStats pending);

            Assert.That(pending.Delta, Is.EqualTo(new PlayerStatsSummary(2, 2, 0, 0, 0, 0)));
        }
    }
}
