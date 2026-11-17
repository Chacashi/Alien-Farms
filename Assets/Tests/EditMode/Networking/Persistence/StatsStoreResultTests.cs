using AlienFarms.Core;
using AlienFarms.Networking.Persistence;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests.Persistence
{
    public sealed class StatsStoreResultTests
    {
        [Test]
        public void Loaded_CarriesRecordReadAndLock()
        {
            var record = new PlayerStatsRecord(1, new PlayerStatsSummary(3, 2, 0, 1, 9, 0), "b1");
            StatsStoreResult result = StatsStoreResult.Loaded(StatsReadResult.Ok, record, "lock");

            Assert.That(result.IsOk, Is.True);
            Assert.That(result.Read, Is.EqualTo(StatsReadResult.Ok));
            Assert.That(result.Record, Is.EqualTo(record));
            Assert.That(result.WriteLock, Is.EqualTo("lock"));
            Assert.That(result.ErrorKey, Is.Null);
        }

        [Test]
        public void Saved_EmptyLockReadsAsNull()
        {
            StatsStoreResult result = StatsStoreResult.Saved(string.Empty);

            Assert.That(result.IsOk, Is.True);
            Assert.That(result.WriteLock, Is.Null);
        }

        [Test]
        public void Retryable_KeepsPositiveRetryAfterAndDefaultsTheKey()
        {
            StatsStoreResult limited = StatsStoreResult.Retryable(PlayerProgressErrorKeys.RateLimited, 12.5f);
            StatsStoreResult unknown = StatsStoreResult.Retryable(null, -3f);

            Assert.That(limited.Outcome, Is.EqualTo(StatsStoreOutcome.RetryableError));
            Assert.That(limited.RetryAfter, Is.EqualTo(12.5f));
            Assert.That(limited.ErrorKey, Is.EqualTo(PlayerProgressErrorKeys.RateLimited));
            Assert.That(unknown.RetryAfter, Is.EqualTo(0f));
            Assert.That(unknown.ErrorKey, Is.EqualTo(PlayerProgressErrorKeys.Unknown));
        }

        [Test]
        public void ConflictAndFailed_AreNotOk()
        {
            Assert.That(StatsStoreResult.Conflict().Outcome, Is.EqualTo(StatsStoreOutcome.Conflict));
            Assert.That(StatsStoreResult.Conflict().IsOk, Is.False);
            Assert.That(StatsStoreResult.Failed(PlayerProgressErrorKeys.SignedOut).Outcome, Is.EqualTo(StatsStoreOutcome.Error));
            Assert.That(StatsStoreResult.Failed(null).ErrorKey, Is.EqualTo(PlayerProgressErrorKeys.Unknown));
        }

        [Test]
        public void SimulatedFailure_FailsAsOfflineOnlyWhileEnabled()
        {
            var inner = new FakePlayerStatsStore();
            bool fail = true;
            var store = new SimulatedFailureStatsStore(inner, () => fail);

            StatsStoreResult failed = store.SaveOwnAsync(PlayerStatsRecord.Empty, null).Result;
            Assert.That(failed.Outcome, Is.EqualTo(StatsStoreOutcome.RetryableError));
            Assert.That(failed.ErrorKey, Is.EqualTo(PlayerProgressErrorKeys.Offline));
            Assert.That(store.LoadOwnAsync().Result.ErrorKey, Is.EqualTo(PlayerProgressErrorKeys.Offline));
            Assert.That(store.LoadPublicAsync("x").Result.ErrorKey, Is.EqualTo(PlayerProgressErrorKeys.Offline));
            Assert.That(inner.SaveCount + inner.LoadCount + inner.PublicLoadCount, Is.EqualTo(0));

            fail = false;
            Assert.That(store.SaveOwnAsync(PlayerStatsRecord.Empty, null).Result.IsOk, Is.True);
            Assert.That(inner.SaveCount, Is.EqualTo(1));
        }
    }
}
