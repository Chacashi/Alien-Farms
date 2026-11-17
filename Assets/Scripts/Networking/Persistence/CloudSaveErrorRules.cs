using AlienFarms.Core;
using Unity.Services.CloudSave;

namespace AlienFarms.Networking.Persistence
{
    /// <summary>Cloud Save failure reason → localization key (<see cref="PlayerProgressErrorKeys"/>) and whether to retry.</summary>
    public static class CloudSaveErrorRules
    {
        public static string ToKey(CloudSaveExceptionReason reason)
        {
            switch (reason)
            {
                case CloudSaveExceptionReason.NoInternetConnection:
                    return PlayerProgressErrorKeys.Offline;
                case CloudSaveExceptionReason.TooManyRequests:
                    return PlayerProgressErrorKeys.RateLimited;
                case CloudSaveExceptionReason.ServiceUnavailable:
                case CloudSaveExceptionReason.ProjectIdMissing:
                case CloudSaveExceptionReason.NotFound:
                    return PlayerProgressErrorKeys.Unavailable;
                case CloudSaveExceptionReason.PlayerIdMissing:
                case CloudSaveExceptionReason.AccessTokenMissing:
                case CloudSaveExceptionReason.Unauthorized:
                    return PlayerProgressErrorKeys.SignedOut;
                default:
                    return PlayerProgressErrorKeys.Unknown;
            }
        }

        /// <summary>Transient failures worth retrying with backoff: no connection, rate limit, service unavailable.</summary>
        public static bool IsRetryable(CloudSaveExceptionReason reason)
        {
            return reason == CloudSaveExceptionReason.NoInternetConnection
                || reason == CloudSaveExceptionReason.TooManyRequests
                || reason == CloudSaveExceptionReason.ServiceUnavailable;
        }
    }
}
