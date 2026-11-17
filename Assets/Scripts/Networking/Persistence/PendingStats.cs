using System;
using AlienFarms.Core;

namespace AlienFarms.Networking.Persistence
{
    /// <summary>
    /// Stats recorded on this machine and not yet confirmed by the cloud (persisted locally through
    /// <see cref="IPendingStatsStore"/>), in two segments so a delta is never lost behind a batch that may already be
    /// applied:
    /// <list type="bullet">
    /// <item><b>Head</b>: what the next (or the in-flight / unanswered) write carries, tagged with <see cref="HeadBatchId"/>.
    /// Once sent (<see cref="HeadSent"/>) it is frozen: a write that may have landed with that batch id must be retried
    /// with exactly the same content.</item>
    /// <item><b>Tail</b>: rounds enqueued after the head was sent. It becomes the head once the head is confirmed (or
    /// found applied), and is merged back into the head when the head is known not to have landed.</item>
    /// </list>
    /// Invariants (enforced here): no tail without a head, and no tail while the head is unsent (it is merged). Default =
    /// nothing pending.
    /// </summary>
    public readonly struct PendingStats : IEquatable<PendingStats>
    {
        /// <summary>One unsent head (what a v1 entry holds).</summary>
        public PendingStats(in PlayerStatsSummary delta, string batchId)
            : this(delta, batchId, false, default, null)
        {
        }

        public PendingStats(
            in PlayerStatsSummary headDelta,
            string headBatchId,
            bool headSent,
            in PlayerStatsSummary tailDelta,
            string tailBatchId)
        {
            bool hasHead = !string.IsNullOrEmpty(headBatchId) && !PlayerStatsRules.IsZero(headDelta);
            bool hasTail = !string.IsNullOrEmpty(tailBatchId) && !PlayerStatsRules.IsZero(tailDelta);

            if (!hasHead && hasTail)
            {
                // The tail moves up as an unsent head.
                HeadDelta = tailDelta;
                HeadBatchId = tailBatchId;
                HeadSent = false;
                TailDelta = default;
                TailBatchId = null;
            }
            else if (hasHead && hasTail && !headSent)
            {
                // An unsent head takes everything (its batch id never reached the cloud).
                HeadDelta = PlayerStatsRules.Add(headDelta, tailDelta);
                HeadBatchId = headBatchId;
                HeadSent = false;
                TailDelta = default;
                TailBatchId = null;
            }
            else if (hasHead)
            {
                HeadDelta = headDelta;
                HeadBatchId = headBatchId;
                HeadSent = headSent;
                TailDelta = hasTail ? tailDelta : default;
                TailBatchId = hasTail ? tailBatchId : null;
            }
            else
            {
                HeadDelta = default;
                HeadBatchId = null;
                HeadSent = false;
                TailDelta = default;
                TailBatchId = null;
            }
        }

        /// <summary>Rounds carried by the next / in-flight write.</summary>
        public PlayerStatsSummary HeadDelta { get; }

        /// <summary>Batch id of the head; null when nothing is pending.</summary>
        public string HeadBatchId { get; }

        /// <summary>The head was sent at least once (it may have landed): frozen until resolved.</summary>
        public bool HeadSent { get; }

        /// <summary>Rounds enqueued after the head was sent.</summary>
        public PlayerStatsSummary TailDelta { get; }

        /// <summary>Batch id of the tail; null when there is no tail.</summary>
        public string TailBatchId { get; }

        public bool HasHead => HeadBatchId != null;

        public bool HasTail => TailBatchId != null;

        public bool IsEmpty => !HasHead;

        /// <summary>Every unconfirmed round (head + tail).</summary>
        public PlayerStatsSummary Delta => HasTail ? PlayerStatsRules.Add(HeadDelta, TailDelta) : HeadDelta;

        /// <summary>Batch id of the head (what the next write is tagged with).</summary>
        public string BatchId => HeadBatchId;

        /// <summary>Adds <paramref name="delta"/>: to the head while unsent, otherwise to the tail (first id names it).</summary>
        public PendingStats Enqueue(in PlayerStatsSummary delta, string deltaId)
        {
            if (!HasHead)
            {
                return new PendingStats(delta, deltaId);
            }

            if (!HeadSent)
            {
                return new PendingStats(PlayerStatsRules.Add(HeadDelta, delta), HeadBatchId, false, default, null);
            }

            return HasTail
                ? new PendingStats(HeadDelta, HeadBatchId, true, PlayerStatsRules.Add(TailDelta, delta), TailBatchId)
                : new PendingStats(HeadDelta, HeadBatchId, true, delta, deltaId);
        }

        /// <summary>The head is being sent: frozen from now on.</summary>
        public PendingStats MarkHeadSent()
        {
            return HasHead ? new PendingStats(HeadDelta, HeadBatchId, true, TailDelta, TailBatchId) : this;
        }

        /// <summary>The head is in the cloud: the tail (if any) becomes the new, unsent head.</summary>
        public PendingStats DropHead()
        {
            return new PendingStats(TailDelta, TailBatchId);
        }

        /// <summary>
        /// Resolves against the batch id of the cloud record: the head's → drop the head; the tail's → drop both;
        /// anything else → neither landed, so the head is unsent again and takes the tail.
        /// </summary>
        public PendingStats ResolveAgainst(string remoteBatchId, out bool headApplied)
        {
            headApplied = false;
            if (!HasHead)
            {
                return this;
            }

            if (remoteBatchId != null && string.Equals(remoteBatchId, HeadBatchId, StringComparison.Ordinal))
            {
                headApplied = true;
                return DropHead();
            }

            if (remoteBatchId != null && HasTail && string.Equals(remoteBatchId, TailBatchId, StringComparison.Ordinal))
            {
                headApplied = true;
                return default;
            }

            return new PendingStats(HeadDelta, HeadBatchId, false, TailDelta, TailBatchId);
        }

        public bool Equals(PendingStats other)
        {
            return HeadDelta.Equals(other.HeadDelta)
                && string.Equals(HeadBatchId, other.HeadBatchId, StringComparison.Ordinal)
                && HeadSent == other.HeadSent
                && TailDelta.Equals(other.TailDelta)
                && string.Equals(TailBatchId, other.TailBatchId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is PendingStats other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(HeadDelta, HeadBatchId, HeadSent, TailDelta, TailBatchId);
    }
}
