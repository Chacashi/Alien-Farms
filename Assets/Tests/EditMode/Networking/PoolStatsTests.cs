using AlienFarms.Data;
using AlienFarms.Networking.Diagnostics;
using AlienFarms.Networking.Pooling;
using NUnit.Framework;
using UnityEngine;

namespace AlienFarms.Networking.Tests
{
    /// <summary>Pool / transport stats view text, its visibility switch and the pool lifecycle log rules (task 2.5).</summary>
    public sealed class PoolStatsTests
    {
        [TearDown]
        public void TearDown()
        {
            PoolStatsToggle.ClearOverride();
        }

        private static string Format(in NetworkStatsSample sample, int bufferSize = NetworkStatsFormatter.BufferSize)
        {
            var buffer = new char[bufferSize];
            int length = NetworkStatsFormatter.Write(buffer, sample);
            return new string(buffer, 0, length);
        }

        [Test]
        public void Formatter_PoolOnly_WritesBothPools()
        {
            var sample = new NetworkStatsSample(new NetworkPoolStats(12, 78, 90), new NetworkPoolStats(1, 2, 3));

            Assert.That(Format(sample), Is.EqualTo("Pool  activos/en pool/creados\nAnimales  12/78/90\nBombas  1/2/3"));
        }

        [Test]
        public void Formatter_WithTransport_AddsRxAndFrameLines()
        {
            var sample = new NetworkStatsSample(
                new NetworkPoolStats(0, 90, 90), new NetworkPoolStats(0, 3, 3), true, 150, 24, 41, 1024, 612);

            Assert.That(Format(sample), Is.EqualTo(
                "Pool  activos/en pool/creados\nAnimales  0/90/90\nBombas  0/3/3\nRx  150 pkt/s  cola 2.4  max 41/1024\nFrame max  612 ms"));
        }

        [Test]
        public void Formatter_ZeroAndNegativeNumbers()
        {
            var sample = new NetworkStatsSample(
                new NetworkPoolStats(0, 0, 0), new NetworkPoolStats(-5, int.MinValue, 7), true, 0, 5, 0, 512, 0);

            string text = Format(sample);
            Assert.That(text, Does.Contain("Animales  0/0/0"));
            Assert.That(text, Does.Contain("Bombas  -5/-2147483648/7"));
            Assert.That(text, Does.Contain("cola 0.5  max 0/512"));
        }

        [Test]
        public void Formatter_LongestTextFitsTheBuffer()
        {
            var sample = new NetworkStatsSample(
                new NetworkPoolStats(int.MinValue, int.MinValue, int.MinValue),
                new NetworkPoolStats(int.MinValue, int.MinValue, int.MinValue),
                true, int.MinValue, int.MinValue + 1, int.MinValue, int.MinValue, int.MinValue);

            Assert.That(Format(sample).Length, Is.LessThan(NetworkStatsFormatter.BufferSize));
        }

        [Test]
        public void Formatter_SmallBuffer_Truncates()
        {
            var sample = new NetworkStatsSample(new NetworkPoolStats(1, 2, 3), new NetworkPoolStats(4, 5, 6));

            Assert.That(Format(sample, 4), Is.EqualTo("Pool"));
            Assert.That(NetworkStatsFormatter.Write(null, sample), Is.EqualTo(0));
        }

        [Test]
        public void Sample_Equality_DetectsAnyChange()
        {
            var a = new NetworkStatsSample(new NetworkPoolStats(1, 2, 3), new NetworkPoolStats(0, 1, 1), true, 10, 5, 7, 512, 16);
            var same = new NetworkStatsSample(new NetworkPoolStats(1, 2, 3), new NetworkPoolStats(0, 1, 1), true, 10, 5, 7, 512, 16);
            var created = new NetworkStatsSample(new NetworkPoolStats(1, 2, 4), new NetworkPoolStats(0, 1, 1), true, 10, 5, 7, 512, 16);
            var frame = new NetworkStatsSample(new NetworkPoolStats(1, 2, 3), new NetworkPoolStats(0, 1, 1), true, 10, 5, 7, 512, 17);

            Assert.That(a == same, Is.True);
            Assert.That(a != created, Is.True);
            Assert.That(a != frame, Is.True);
            Assert.That(new NetworkPoolStats(1, 2, 3), Is.EqualTo(new NetworkPoolStats(1, 2, 3)));
        }

        [Test]
        public void Toggle_FollowsDefaultUntilOverridden()
        {
            int changes = 0;
            void Count() => changes++;
            PoolStatsToggle.Changed += Count;
            try
            {
                Assert.That(PoolStatsToggle.IsVisible(true), Is.True);
                PoolStatsToggle.Toggle(true);
                Assert.That(PoolStatsToggle.IsVisible(true), Is.False);
                PoolStatsToggle.SetVisible(false);
                Assert.That(changes, Is.EqualTo(1));
                PoolStatsToggle.ClearOverride();
                Assert.That(PoolStatsToggle.HasOverride, Is.False);
                Assert.That(changes, Is.EqualTo(2));
            }
            finally
            {
                PoolStatsToggle.Changed -= Count;
            }
        }

        [Test]
        public void Toggle_IsIndependentFromOwnershipLabels()
        {
            PoolStatsToggle.SetVisible(false);
            try
            {
                Assert.That(OwnershipLabelToggle.HasOverride, Is.False);
                Assert.That(OwnershipLabelToggle.IsVisible(true), Is.True);
            }
            finally
            {
                PoolStatsToggle.ClearOverride();
            }
        }

        [Test]
        public void DebugViewSwitch_Reset_DropsOverrideAndListeners()
        {
            var view = new DebugViewSwitch();
            int changes = 0;
            view.Changed += () => changes++;
            view.SetVisible(true);
            view.Reset();

            Assert.That(view.HasOverride, Is.False);
            view.SetVisible(true);
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void LifecycleRules_DescribeUse()
        {
            Assert.That(PoolLifecycleRules.DescribeUse(0), Is.EqualTo("unspawned"));
            Assert.That(PoolLifecycleRules.DescribeUse(1), Is.EqualTo("first use"));
            Assert.That(PoolLifecycleRules.DescribeUse(3), Is.EqualTo("reused (use 3)"));
        }

        [Test]
        public void LifecycleRules_Growth()
        {
            Assert.That(PoolLifecycleRules.Growth(-1, 90), Is.EqualTo(0), "no previous reading");
            Assert.That(PoolLifecycleRules.Growth(90, 90), Is.EqualTo(0));
            Assert.That(PoolLifecycleRules.Growth(90, 92), Is.EqualTo(2));
            Assert.That(PoolLifecycleRules.Growth(92, 90), Is.EqualTo(0), "a pool that shrank did not grow");
        }

        [Test]
        public void Formatter_MeanTenths_HandlesIntMinValue()
        {
            var sample = new NetworkStatsSample(
                new NetworkPoolStats(0, 0, 0), new NetworkPoolStats(0, 0, 0), true, 0, int.MinValue, 0, 512, 0);

            Assert.That(Format(sample), Does.Contain("cola -214748364.8  max 0/512"));
        }

        [Test]
        public void GrowthTracker_PrewarmIsTheBaseline()
        {
            var tracker = new PoolGrowthTracker(90);

            Assert.That(tracker.Observe(12), Is.EqualTo(0), "replicated spawns before the prewarm");
            Assert.That(tracker.Observe(90), Is.EqualTo(0), "prewarm done");
            Assert.That(tracker.Observe(91), Is.EqualTo(1), "first spawn that instantiates is reported");
            Assert.That(tracker.Observe(91), Is.EqualTo(0));
            Assert.That(tracker.Observe(94), Is.EqualTo(3));
            Assert.That(tracker.Reference, Is.EqualTo(94));
        }

        [Test]
        public void GrowthTracker_NegativePrewarm_IsZero()
        {
            var tracker = new PoolGrowthTracker(-3);

            Assert.That(tracker.Reference, Is.EqualTo(0));
            Assert.That(tracker.Observe(2), Is.EqualTo(2));
        }

        [Test]
        public void TransportTracker_NewSessionResetsLongestFrameAndRate()
        {
            var tracker = new TransportStatsTracker();
            Assert.That(tracker.Tick(false, 2f), Is.False);
            Assert.That(tracker.LongestFrame, Is.EqualTo(0f), "frames outside a session are ignored");

            Assert.That(tracker.Tick(true, 0.5f), Is.True);
            tracker.Tick(true, 0.1f);
            Assert.That(tracker.LongestFrame, Is.EqualTo(0.5f));
            tracker.SampleRx(1000, 10f);
            Assert.That(tracker.SampleRx(1150, 11f), Is.EqualTo(150));

            tracker.Tick(false, 0.02f);
            Assert.That(tracker.Tick(true, 0.03f), Is.True, "second session");
            Assert.That(tracker.LongestFrame, Is.EqualTo(0.03f));
            Assert.That(tracker.RxPerSecond, Is.EqualTo(0));
            Assert.That(tracker.SampleRx(20, 30f), Is.EqualTo(0), "new driver counters: baseline only");
            Assert.That(tracker.SampleRx(70, 30.5f), Is.EqualTo(100));
        }

        [Test]
        public void TransportTracker_RebaseSkipsTheHiddenPeriod()
        {
            var tracker = new TransportStatsTracker();
            tracker.Tick(true, 0.016f);
            tracker.SampleRx(0, 0f);
            Assert.That(tracker.SampleRx(100, 1f), Is.EqualTo(100));

            tracker.RequestRebase();
            Assert.That(tracker.SampleRx(5000, 60f), Is.EqualTo(0));
            Assert.That(tracker.SampleRx(5200, 61f), Is.EqualTo(200));
        }

        [Test]
        public void Settings_PoolStatsShownAndLifecycleLogOffByDefault()
        {
            var settings = ScriptableObject.CreateInstance<NetworkSettings>();
            try
            {
                Assert.That(settings.ShowPoolStats, Is.True);
                Assert.That(settings.LogPoolLifecycle, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }
    }
}
