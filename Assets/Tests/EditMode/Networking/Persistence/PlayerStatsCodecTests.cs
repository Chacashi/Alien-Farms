using System.Collections.Generic;
using AlienFarms.Core;
using AlienFarms.Networking.Persistence;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests.Persistence
{
    public sealed class PlayerStatsCodecTests
    {
        [Test]
        public void WriteThenRead_RoundTrips()
        {
            var record = new PlayerStatsRecord(1, new PlayerStatsSummary(12, 7, 1, 4, 83, 2), "9f2c");
            var fields = new Dictionary<string, object>();

            PlayerStatsCodec.Write(record, fields);
            StatsReadResult read = PlayerStatsCodec.Read(fields, out PlayerStatsRecord decoded);

            Assert.That(read, Is.EqualTo(StatsReadResult.Ok));
            Assert.That(decoded, Is.EqualTo(record));
            Assert.That(fields[PlayerStatsCodec.VersionField], Is.EqualTo(PlayerStatsCodec.CurrentVersion));
        }

        [Test]
        public void Write_WithoutBatch_OmitsTheFieldAndClearsTarget()
        {
            var fields = new Dictionary<string, object> { ["stale"] = 1 };

            PlayerStatsCodec.Write(PlayerStatsRecord.Empty, fields);

            Assert.That(fields.ContainsKey(PlayerStatsCodec.BatchField), Is.False);
            Assert.That(fields.ContainsKey("stale"), Is.False);
        }

        [Test]
        public void Read_Null_IsMissingAndEmpty()
        {
            Assert.That(PlayerStatsCodec.Read(null, out PlayerStatsRecord record), Is.EqualTo(StatsReadResult.Missing));
            Assert.That(record, Is.EqualTo(PlayerStatsRecord.Empty));
        }

        [Test]
        public void Read_AcceptsLongDoubleIntAndNumericString()
        {
            var fields = new Dictionary<string, object>
            {
                ["v"] = 1L,
                ["matches"] = 5L,
                ["wins"] = 2d,
                ["draws"] = 1,
                ["losses"] = "2",
                ["animals"] = 40L,
                ["tackles"] = 0d
            };

            Assert.That(PlayerStatsCodec.Read(fields, out PlayerStatsRecord record), Is.EqualTo(StatsReadResult.Ok));
            Assert.That(record.Stats, Is.EqualTo(new PlayerStatsSummary(5, 2, 1, 2, 40, 0)));
            Assert.That(record.LastBatchId, Is.Null);
        }

        [Test]
        public void Read_WithoutVersion_IsV1()
        {
            var fields = new Dictionary<string, object> { ["matches"] = 1L, ["wins"] = 1L };

            Assert.That(PlayerStatsCodec.Read(fields, out PlayerStatsRecord record), Is.EqualTo(StatsReadResult.Ok));
            Assert.That(record.Version, Is.EqualTo(1));
        }

        [Test]
        public void Read_NewerVersion_IsNewerSchemaWithValues()
        {
            var fields = new Dictionary<string, object> { ["v"] = 2L, ["matches"] = 3L, ["wins"] = 3L, ["extra"] = "x" };

            Assert.That(PlayerStatsCodec.Read(fields, out PlayerStatsRecord record), Is.EqualTo(StatsReadResult.NewerSchema));
            Assert.That(record.Stats.Wins, Is.EqualTo(3));
            Assert.That(record.Version, Is.EqualTo(2));
        }

        [TestCase("abc")]
        [TestCase(true)]
        [TestCase(1.5d)]
        public void Read_InvalidType_IsCorrupt(object value)
        {
            var fields = new Dictionary<string, object> { ["v"] = 1L, ["wins"] = value };

            Assert.That(PlayerStatsCodec.Read(fields, out PlayerStatsRecord record), Is.EqualTo(StatsReadResult.Corrupt));
            Assert.That(record, Is.EqualTo(PlayerStatsRecord.Empty));
        }

        [Test]
        public void Read_NonStringBatch_IsCorrupt()
        {
            var fields = new Dictionary<string, object> { ["batch"] = 12L };

            Assert.That(PlayerStatsCodec.Read(fields, out _), Is.EqualTo(StatsReadResult.Corrupt));
        }

        [Test]
        public void Read_InvalidVersion_IsCorrupt()
        {
            Assert.That(PlayerStatsCodec.Read(new Dictionary<string, object> { ["v"] = 0L }, out _), Is.EqualTo(StatsReadResult.Corrupt));
            Assert.That(PlayerStatsCodec.Read(new Dictionary<string, object> { ["v"] = "one" }, out _), Is.EqualTo(StatsReadResult.Corrupt));
        }

        [Test]
        public void Read_SanitizesNegativesHugeValuesAndMatches()
        {
            var fields = new Dictionary<string, object>
            {
                ["matches"] = 1L,
                ["wins"] = 3L,
                ["losses"] = -2L,
                ["animals"] = (long)int.MaxValue + 10L,
                ["tackles"] = 1e12d
            };

            Assert.That(PlayerStatsCodec.Read(fields, out PlayerStatsRecord record), Is.EqualTo(StatsReadResult.Ok));
            Assert.That(record.Stats, Is.EqualTo(new PlayerStatsSummary(3, 3, 0, 0, int.MaxValue, int.MaxValue)));
        }

        [Test]
        public void Read_KeepsBatch()
        {
            var fields = new Dictionary<string, object> { ["batch"] = "abc" };

            PlayerStatsCodec.Read(fields, out PlayerStatsRecord record);

            Assert.That(record.LastBatchId, Is.EqualTo("abc"));
        }
    }
}
