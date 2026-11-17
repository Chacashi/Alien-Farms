using UnityEngine;

namespace AlienFarms.Networking.Persistence
{
    /// <summary>
    /// <see cref="IPendingStatsStore"/> on PlayerPrefs, key <c>stats.pending.{ugsId}</c> (one per account, so MPPM clones
    /// sharing PlayerPrefs never collide). Every write and delete is flushed with <see cref="PlayerPrefs.Save"/>, so a
    /// round recorded right before closing the game survives. Invalid or newer entries are logged and deleted.
    /// </summary>
    public sealed class PlayerPrefsPendingStatsStore : IPendingStatsStore
    {
        public const string KeyPrefix = "stats.pending.";

        /// <summary>PlayerPrefs key of <paramref name="ownerId"/>'s pending stats.</summary>
        public static string KeyFor(string ownerId) => KeyPrefix + ownerId;

        public PendingStats Load(string ownerId)
        {
            if (string.IsNullOrEmpty(ownerId))
            {
                return default;
            }

            string key = KeyFor(ownerId);
            string json = PlayerPrefs.GetString(key, null);
            StatsReadResult read = PendingStatsCodec.Read(json, out PendingStats pending);
            switch (read)
            {
                case StatsReadResult.Ok:
                    return pending;
                case StatsReadResult.Missing:
                    return default;
                default:
                    Debug.LogWarning($"[{nameof(PlayerPrefsPendingStatsStore)}] Ignoring pending stats '{key}' ({read}); deleted.");
                    Delete(key);
                    return default;
            }
        }

        public void Save(string ownerId, in PendingStats pending)
        {
            if (string.IsNullOrEmpty(ownerId))
            {
                return;
            }

            string json = PendingStatsCodec.Write(pending);
            if (json == null)
            {
                Clear(ownerId);
                return;
            }

            PlayerPrefs.SetString(KeyFor(ownerId), json);
            PlayerPrefs.Save();
        }

        public void Clear(string ownerId)
        {
            if (!string.IsNullOrEmpty(ownerId))
            {
                Delete(KeyFor(ownerId));
            }
        }

        private static void Delete(string key)
        {
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }
    }
}
