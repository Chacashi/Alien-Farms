using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AlienFarms.Core;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.CloudSave.Models.Data.Player;
using UnityEngine;
// Unity.Services.CloudSave.SaveOptions is the Files one: Player Data needs this one.
using PlayerSaveOptions = Unity.Services.CloudSave.Models.Data.Player.SaveOptions;

namespace AlienFarms.Networking.Persistence
{
    /// <summary>
    /// The only class that talks to Cloud Save (3.4.x): the stats record lives in Player Data under
    /// <see cref="PlayerStatsCodec.Key"/> with the Public access class (only its owner writes it; anyone reads it by
    /// player id). Exceptions become <see cref="StatsStoreResult"/>s: conflict → <see cref="StatsStoreOutcome.Conflict"/>,
    /// rate limit → retryable with Retry-After, other Cloud Save failures through <see cref="CloudSaveErrorRules"/>,
    /// anything else → <see cref="PlayerProgressErrorKeys.Unknown"/>. Requires Unity Services initialized and signed in.
    /// The SDK calls take no cancellation token. Allocates per call (one per round or load), never per frame.
    /// </summary>
    public sealed class CloudSavePlayerStatsStore : IPlayerStatsStore
    {
        private static readonly HashSet<string> Keys = new HashSet<string> { PlayerStatsCodec.Key };

        public async Task<StatsStoreResult> LoadOwnAsync()
        {
            try
            {
                Dictionary<string, Item> items = await CloudSaveService.Instance.Data.Player.LoadAsync(
                    Keys, new LoadOptions(new PublicReadAccessClassOptions()));
                return FromItems(items);
            }
            catch (Exception exception)
            {
                return FromException(exception);
            }
        }

        public async Task<StatsStoreResult> LoadPublicAsync(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                return StatsStoreResult.Failed(PlayerProgressErrorKeys.Unknown);
            }

            try
            {
                Dictionary<string, Item> items = await CloudSaveService.Instance.Data.Player.LoadAsync(
                    Keys, new LoadOptions(new PublicReadAccessClassOptions(playerId)));
                return FromItems(items);
            }
            catch (Exception exception)
            {
                return FromException(exception);
            }
        }

        public async Task<StatsStoreResult> SaveOwnAsync(PlayerStatsRecord record, string writeLock)
        {
            try
            {
                var fields = new Dictionary<string, object>();
                PlayerStatsCodec.Write(record, fields);
                var data = new Dictionary<string, SaveItem>
                {
                    [PlayerStatsCodec.Key] = new SaveItem(fields, string.IsNullOrEmpty(writeLock) ? null : writeLock)
                };

                Dictionary<string, string> locks = await CloudSaveService.Instance.Data.Player.SaveAsync(
                    data, new PlayerSaveOptions(new PublicWriteAccessClassOptions()));
                string newLock = null;
                locks?.TryGetValue(PlayerStatsCodec.Key, out newLock);
                return StatsStoreResult.Saved(newLock);
            }
            catch (Exception exception)
            {
                return FromException(exception);
            }
        }

        private static StatsStoreResult FromItems(Dictionary<string, Item> items)
        {
            if (items == null || !items.TryGetValue(PlayerStatsCodec.Key, out Item item) || item == null || item.Value == null)
            {
                return StatsStoreResult.Loaded(StatsReadResult.Missing, PlayerStatsRecord.Empty, null);
            }

            Dictionary<string, object> fields;
            try
            {
                fields = item.Value.GetAs<Dictionary<string, object>>();
            }
            catch (Exception exception)
            {
                // Not a JSON object: never overwritten (the ledger goes read only).
                Debug.LogWarning($"[{nameof(CloudSavePlayerStatsStore)}] Stats record is not an object: {exception.Message}");
                return StatsStoreResult.Loaded(StatsReadResult.Corrupt, PlayerStatsRecord.Empty, item.WriteLock);
            }

            StatsReadResult read = PlayerStatsCodec.Read(fields, out PlayerStatsRecord record);
            return StatsStoreResult.Loaded(read, record, item.WriteLock);
        }

        private static StatsStoreResult FromException(Exception exception)
        {
            switch (exception)
            {
                case CloudSaveConflictException _:
                    return StatsStoreResult.Conflict();
                case CloudSaveRateLimitedException rateLimited:
                    return StatsStoreResult.Retryable(PlayerProgressErrorKeys.RateLimited, rateLimited.RetryAfter);
                case CloudSaveException cloudSave:
                    string key = CloudSaveErrorRules.ToKey(cloudSave.Reason);
                    Debug.LogWarning($"[{nameof(CloudSavePlayerStatsStore)}] Cloud Save failed ({cloudSave.Reason}): {cloudSave.Message}");
                    return CloudSaveErrorRules.IsRetryable(cloudSave.Reason)
                        ? StatsStoreResult.Retryable(key)
                        : StatsStoreResult.Failed(key);
                default:
                    Debug.LogException(exception);
                    return StatsStoreResult.Failed(PlayerProgressErrorKeys.Unknown);
            }
        }
    }
}
