using AlienFarms.Data;
using NUnit.Framework;
using UnityEngine;

namespace AlienFarms.Networking.Tests
{
    /// <summary>Defaults the online layer relies on (plan section 5.4).</summary>
    public sealed class NetworkSettingsTests
    {
        private NetworkSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<NetworkSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_settings);
        }

        [Test]
        public void Defaults_MatchThePlan()
        {
            Assert.That(_settings.EnvironmentName, Is.EqualTo("dev"));
            Assert.That(_settings.MaxPlayers, Is.EqualTo(2));
            Assert.That(_settings.ReconnectGraceSeconds, Is.EqualTo(20f));
            Assert.That(_settings.InputRedundancy, Is.EqualTo(3));
            Assert.That(_settings.InputBufferTarget, Is.EqualTo(2));
            Assert.That(_settings.InterpolationDelay, Is.EqualTo(0.1f));
            Assert.That(_settings.ReconcileThreshold, Is.EqualTo(0.05f));
            Assert.That(_settings.MaxRewindSeconds, Is.EqualTo(0.3f));
            Assert.That(_settings.TackleRewindTolerance, Is.EqualTo(0.35f));
        }

        [Test]
        public void Defaults_CloudSaveMatchesTheDesign()
        {
            Assert.That(_settings.ReadRivalStats, Is.True);
            Assert.That(_settings.StatsSaveAttempts, Is.EqualTo(3));
            Assert.That(_settings.StatsRetryDelaySeconds, Is.EqualTo(4f));
            Assert.That(_settings.LogPersistence, Is.True);
            Assert.That(_settings.SimulateStatsFailure, Is.False);
        }

        [Test]
        public void Defaults_ShowOwnershipLabelsAndLogOwnership()
        {
            Assert.That(_settings.ShowOwnershipLabels, Is.True);
            Assert.That(_settings.LogOwnership, Is.True);
        }
    }
}
