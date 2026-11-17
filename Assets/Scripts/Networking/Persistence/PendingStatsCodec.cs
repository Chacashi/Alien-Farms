using System;
using AlienFarms.Core;
using UnityEngine;

namespace AlienFarms.Networking.Persistence
{
    /// <summary>
    /// JSON of the local pending stats (v2): <c>{ "v":2, "batch":"…", "matches", "wins", "draws", "losses", "animals",
    /// "tackles", "sent":bool, "tail":{ "batch", "matches", … } }</c>. The top level is the head (a v1 entry, without
    /// "sent" or "tail", reads as an unsent head). Pure (JsonUtility only), so it is tested in EditMode. Called on enqueue
    /// and load only, never per frame.
    /// </summary>
    public static class PendingStatsCodec
    {
        public const int CurrentVersion = 2;

        /// <summary>JSON of <paramref name="pending"/>; null when it is empty (nothing to store).</summary>
        public static string Write(in PendingStats pending)
        {
            if (pending.IsEmpty)
            {
                return null;
            }

            PlayerStatsSummary head = PlayerStatsRules.Sanitize(pending.HeadDelta);
            PlayerStatsSummary tail = PlayerStatsRules.Sanitize(pending.TailDelta);
            var json = new PendingStatsJson
            {
                Version = CurrentVersion,
                Batch = pending.HeadBatchId,
                Matches = head.MatchesPlayed,
                Wins = head.Wins,
                Draws = head.Draws,
                Losses = head.Losses,
                Animals = head.AnimalsScored,
                Tackles = head.TacklesLanded,
                Sent = pending.HeadSent,
                Tail = new PendingSegmentJson
                {
                    Batch = pending.TailBatchId ?? string.Empty,
                    Matches = tail.MatchesPlayed,
                    Wins = tail.Wins,
                    Draws = tail.Draws,
                    Losses = tail.Losses,
                    Animals = tail.AnimalsScored,
                    Tackles = tail.TacklesLanded
                }
            };
            return JsonUtility.ToJson(json);
        }

        /// <summary>
        /// Decodes <paramref name="json"/>. Null / blank or an entry with nothing pending = <see cref="StatsReadResult.Missing"/>;
        /// invalid JSON or a version below 1 = <see cref="StatsReadResult.Corrupt"/>; a higher version =
        /// <see cref="StatsReadResult.NewerSchema"/>. <paramref name="pending"/> is default unless <see cref="StatsReadResult.Ok"/>.
        /// A missing "v" (0) reads as v1; v1 has no "sent" / "tail" (an unsent head).
        /// </summary>
        public static StatsReadResult Read(string json, out PendingStats pending)
        {
            pending = default;
            if (string.IsNullOrWhiteSpace(json))
            {
                return StatsReadResult.Missing;
            }

            PendingStatsJson parsed;
            try
            {
                parsed = JsonUtility.FromJson<PendingStatsJson>(json);
            }
            catch (ArgumentException)
            {
                return StatsReadResult.Corrupt;
            }

            if (parsed == null)
            {
                return StatsReadResult.Corrupt;
            }

            int version = parsed.Version == 0 ? 1 : parsed.Version;
            if (version < 0)
            {
                return StatsReadResult.Corrupt;
            }

            if (version > CurrentVersion)
            {
                return StatsReadResult.NewerSchema;
            }

            var head = new PlayerStatsSummary(parsed.Matches, parsed.Wins, parsed.Draws, parsed.Losses, parsed.Animals, parsed.Tackles);
            PendingStats result;
            PendingSegmentJson tail = parsed.Tail;
            if (version >= 2 && tail != null)
            {
                var tailDelta = new PlayerStatsSummary(tail.Matches, tail.Wins, tail.Draws, tail.Losses, tail.Animals, tail.Tackles);
                result = new PendingStats(PlayerStatsRules.Sanitize(head), parsed.Batch, parsed.Sent,
                    PlayerStatsRules.Sanitize(tailDelta), tail.Batch);
            }
            else
            {
                result = new PendingStats(PlayerStatsRules.Sanitize(head), parsed.Batch);
            }

            if (result.IsEmpty)
            {
                return StatsReadResult.Missing;
            }

            pending = result;
            return StatsReadResult.Ok;
        }
    }
}
