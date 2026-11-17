using System;
using System.Collections.Generic;
using System.Globalization;
using AlienFarms.Core;

namespace AlienFarms.Networking.Persistence
{
    /// <summary>
    /// Schema of the cloud stats record (key <see cref="Key"/>, a JSON object the Cloud Save SDK serializes):
    /// <c>{ "v": 1, "matches", "wins", "draws", "losses", "animals", "tackles", "batch" }</c>. Pure: no SDK types.
    /// Reading accepts what the SDK's JSON deserialization yields for numbers (<c>long</c>, <c>int</c>, an integral
    /// <c>double</c>) and numeric strings; values are sanitized (see <see cref="PlayerStatsRules.Sanitize"/>) and clipped
    /// to <see cref="int.MaxValue"/>.
    /// </summary>
    public static class PlayerStatsCodec
    {
        public const string Key = "stats";
        public const int CurrentVersion = 1;

        public const string VersionField = "v";
        public const string MatchesField = "matches";
        public const string WinsField = "wins";
        public const string DrawsField = "draws";
        public const string LossesField = "losses";
        public const string AnimalsField = "animals";
        public const string TacklesField = "tackles";
        public const string BatchField = "batch";

        /// <summary>Fills <paramref name="target"/> (cleared first) with <paramref name="record"/> at the current version.</summary>
        public static void Write(in PlayerStatsRecord record, IDictionary<string, object> target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            PlayerStatsSummary stats = PlayerStatsRules.Sanitize(record.Stats);
            target.Clear();
            target[VersionField] = CurrentVersion;
            target[MatchesField] = stats.MatchesPlayed;
            target[WinsField] = stats.Wins;
            target[DrawsField] = stats.Draws;
            target[LossesField] = stats.Losses;
            target[AnimalsField] = stats.AnimalsScored;
            target[TacklesField] = stats.TacklesLanded;
            if (record.LastBatchId != null)
            {
                target[BatchField] = record.LastBatchId;
            }
        }

        /// <summary>
        /// Decodes <paramref name="source"/>. Null = <see cref="StatsReadResult.Missing"/> (record = Empty). Without "v" it is
        /// v1. A higher version is <see cref="StatsReadResult.NewerSchema"/> (record holds what could be read, for display).
        /// Any field of an invalid type is <see cref="StatsReadResult.Corrupt"/> (record = Empty). Missing fields read as 0.
        /// </summary>
        public static StatsReadResult Read(IReadOnlyDictionary<string, object> source, out PlayerStatsRecord record)
        {
            record = PlayerStatsRecord.Empty;
            if (source == null)
            {
                return StatsReadResult.Missing;
            }

            int version = CurrentVersion;
            if (source.TryGetValue(VersionField, out object rawVersion) && rawVersion != null)
            {
                if (!TryReadCount(rawVersion, out version) || version < 1)
                {
                    return StatsReadResult.Corrupt;
                }
            }

            bool valid = TryReadField(source, MatchesField, out int matches)
                & TryReadField(source, WinsField, out int wins)
                & TryReadField(source, DrawsField, out int draws)
                & TryReadField(source, LossesField, out int losses)
                & TryReadField(source, AnimalsField, out int animals)
                & TryReadField(source, TacklesField, out int tackles);

            string batch = null;
            if (source.TryGetValue(BatchField, out object rawBatch) && rawBatch != null)
            {
                batch = rawBatch as string;
                valid &= batch != null;
            }

            if (version > CurrentVersion)
            {
                // Shown as is (best effort), never overwritten.
                if (valid)
                {
                    var newer = new PlayerStatsSummary(matches, wins, draws, losses, animals, tackles);
                    record = new PlayerStatsRecord(version, PlayerStatsRules.Sanitize(newer), batch);
                }

                return StatsReadResult.NewerSchema;
            }

            if (!valid)
            {
                return StatsReadResult.Corrupt;
            }

            var stats = new PlayerStatsSummary(matches, wins, draws, losses, animals, tackles);
            record = new PlayerStatsRecord(version, PlayerStatsRules.Sanitize(stats), batch);
            return StatsReadResult.Ok;
        }

        /// <summary>
        /// A non-negative count from a JSON number (<c>long</c>, <c>int</c> and the other integral types, an integral
        /// <c>double</c> / <c>float</c> / <c>decimal</c>) or a numeric string. Negatives read as 0, values above
        /// <see cref="int.MaxValue"/> are clipped. False for any other type, fractions, NaN or infinities.
        /// </summary>
        public static bool TryReadCount(object value, out int count)
        {
            count = 0;
            switch (value)
            {
                case int i:
                    count = Clip(i);
                    return true;
                case long l:
                    count = Clip(l);
                    return true;
                case short s:
                    count = Clip(s);
                    return true;
                case byte b:
                    count = b;
                    return true;
                case sbyte sb:
                    count = Clip(sb);
                    return true;
                case ushort us:
                    count = us;
                    return true;
                case uint ui:
                    count = Clip((long)ui);
                    return true;
                case ulong ul:
                    count = ul > int.MaxValue ? int.MaxValue : (int)ul;
                    return true;
                case double d:
                    return TryReadIntegral(d, out count);
                case float f:
                    return TryReadIntegral(f, out count);
                case decimal m:
                    if (decimal.Truncate(m) != m)
                    {
                        return false;
                    }

                    count = m <= 0m ? 0 : m >= int.MaxValue ? int.MaxValue : (int)m;
                    return true;
                case string text:
                    return TryReadString(text, out count);
                default:
                    return false;
            }
        }

        private static bool TryReadField(IReadOnlyDictionary<string, object> source, string field, out int count)
        {
            count = 0;
            if (!source.TryGetValue(field, out object raw) || raw == null)
            {
                return true;
            }

            return TryReadCount(raw, out count);
        }

        private static bool TryReadIntegral(double value, out int count)
        {
            count = 0;
            if (double.IsNaN(value) || double.IsInfinity(value) || Math.Floor(value) != value)
            {
                return false;
            }

            count = value <= 0d ? 0 : value >= int.MaxValue ? int.MaxValue : (int)value;
            return true;
        }

        private static bool TryReadString(string text, out int count)
        {
            count = 0;
            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed))
            {
                count = Clip(parsed);
                return true;
            }

            // Beyond the long range: a valid integer still clips (a sign decides the side).
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double big))
            {
                return TryReadIntegral(big, out count);
            }

            return false;
        }

        private static int Clip(long value) => value <= 0 ? 0 : value >= int.MaxValue ? int.MaxValue : (int)value;
    }
}
