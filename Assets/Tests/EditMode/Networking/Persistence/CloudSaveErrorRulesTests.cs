using AlienFarms.Core;
using AlienFarms.Networking.Persistence;
using NUnit.Framework;
using Unity.Services.CloudSave;

namespace AlienFarms.Networking.Tests.Persistence
{
    public sealed class CloudSaveErrorRulesTests
    {
        [TestCase(CloudSaveExceptionReason.NoInternetConnection, PlayerProgressErrorKeys.Offline)]
        [TestCase(CloudSaveExceptionReason.TooManyRequests, PlayerProgressErrorKeys.RateLimited)]
        [TestCase(CloudSaveExceptionReason.ServiceUnavailable, PlayerProgressErrorKeys.Unavailable)]
        [TestCase(CloudSaveExceptionReason.NotFound, PlayerProgressErrorKeys.Unavailable)]
        [TestCase(CloudSaveExceptionReason.ProjectIdMissing, PlayerProgressErrorKeys.Unavailable)]
        [TestCase(CloudSaveExceptionReason.Unauthorized, PlayerProgressErrorKeys.SignedOut)]
        [TestCase(CloudSaveExceptionReason.PlayerIdMissing, PlayerProgressErrorKeys.SignedOut)]
        [TestCase(CloudSaveExceptionReason.AccessTokenMissing, PlayerProgressErrorKeys.SignedOut)]
        [TestCase(CloudSaveExceptionReason.InvalidArgument, PlayerProgressErrorKeys.Unknown)]
        [TestCase(CloudSaveExceptionReason.Unknown, PlayerProgressErrorKeys.Unknown)]
        public void ToKey_MapsReason(CloudSaveExceptionReason reason, string expected)
        {
            Assert.That(CloudSaveErrorRules.ToKey(reason), Is.EqualTo(expected));
        }

        [TestCase(CloudSaveExceptionReason.NoInternetConnection, true)]
        [TestCase(CloudSaveExceptionReason.TooManyRequests, true)]
        [TestCase(CloudSaveExceptionReason.ServiceUnavailable, true)]
        [TestCase(CloudSaveExceptionReason.Unauthorized, false)]
        [TestCase(CloudSaveExceptionReason.Conflict, false)]
        [TestCase(CloudSaveExceptionReason.InvalidArgument, false)]
        [TestCase(CloudSaveExceptionReason.Unknown, false)]
        public void IsRetryable_OnlyTransientFailures(CloudSaveExceptionReason reason, bool expected)
        {
            Assert.That(CloudSaveErrorRules.IsRetryable(reason), Is.EqualTo(expected));
        }
    }
}
