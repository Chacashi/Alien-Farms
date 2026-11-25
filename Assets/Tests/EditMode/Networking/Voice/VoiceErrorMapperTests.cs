using System;
using AlienFarms.Networking.Voice;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests.Voice
{
    public sealed class VoiceErrorMapperTests
    {
        [TestCase("Vivox credentials are missing!")]
        [TestCase("Invalid Auth Token")]
        [TestCase("Access token issuer mismatch")]
        public void MapsNestedCredentialFailures(string message)
        {
            var exception = new InvalidOperationException("Vivox login failed", new Exception(message));
            Assert.That(VoiceErrorMapper.FromException(exception, VoiceErrorMapper.Login),
                Is.EqualTo(VoiceErrorMapper.Credentials));
        }

        [TestCase(VoiceErrorMapper.Login)]
        [TestCase(VoiceErrorMapper.Join)]
        [TestCase(VoiceErrorMapper.RecoverFailed)]
        public void PreservesOperationFallback(string fallback)
        {
            Assert.That(VoiceErrorMapper.FromException(new TimeoutException(), fallback), Is.EqualTo(fallback));
            Assert.That(VoiceErrorMapper.FromException(null, fallback), Is.EqualTo(fallback));
        }
    }
}
