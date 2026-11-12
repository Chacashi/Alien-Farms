using AlienFarms.Core;
using AlienFarms.Networking.Diagnostics;
using AlienFarms.Networking.Player;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests
{
    /// <summary>Ownership debug label text (cached) and its machine-local visibility switch (task 2.4).</summary>
    public sealed class OwnershipLabelTests
    {
        [TearDown]
        public void TearDown()
        {
            OwnershipLabelToggle.ClearOverride();
        }

        [Test]
        public void Cache_FormatsOwnerAndMarks()
        {
            var cache = new OwnershipLabelCache("✓", "✗");

            Assert.That(cache.Get(CarriableOwner.ForSeat(PlayerId.Player2), false, true),
                Is.EqualTo("Owner: P2 · IsOwner ✗ · HasAuthority ✓"));
            Assert.That(cache.Get(CarriableOwner.ForSeat(PlayerId.Player1), true, true),
                Is.EqualTo("Owner: P1 · IsOwner ✓ · HasAuthority ✓"));
            Assert.That(cache.Get(CarriableOwner.Server, false, false),
                Is.EqualTo("Owner: Server · IsOwner ✗ · HasAuthority ✗"));
            Assert.That(cache.Get(CarriableOwner.Unresolved, false, false),
                Is.EqualTo("Owner: ? · IsOwner ✗ · HasAuthority ✗"));
        }

        [Test]
        public void Cache_ReturnsTheSameInstance()
        {
            var cache = new OwnershipLabelCache("Y", "N");
            string first = cache.Get(CarriableOwner.ForSeat(PlayerId.Player2), true, false);

            Assert.That(cache.Get(CarriableOwner.ForSeat(PlayerId.Player2), true, false), Is.SameAs(first));
            Assert.That(first, Is.EqualTo("Owner: P2 · IsOwner Y · HasAuthority N"));
        }

        [Test]
        public void Cache_EveryVariantIsDistinct()
        {
            var cache = new OwnershipLabelCache("Y", "N");
            var owners = new[] { CarriableOwner.Server, CarriableOwner.ForSeat(PlayerId.Player1), CarriableOwner.ForSeat(PlayerId.Player2), CarriableOwner.Unresolved };
            var texts = new System.Collections.Generic.List<string>();
            foreach (CarriableOwner owner in owners)
            {
                texts.Add(cache.Get(owner, false, false));
                texts.Add(cache.Get(owner, false, true));
                texts.Add(cache.Get(owner, true, false));
                texts.Add(cache.Get(owner, true, true));
            }

            Assert.That(texts, Is.Unique);
        }

        [Test]
        public void Cache_NullMarks_AreEmpty()
        {
            var cache = new OwnershipLabelCache(null, null);
            Assert.That(cache.Get(CarriableOwner.Server, true, false), Is.EqualTo("Owner: Server · IsOwner  · HasAuthority "));
        }

        [Test]
        public void Toggle_FollowsTheDefaultUntilOverridden()
        {
            Assert.That(OwnershipLabelToggle.HasOverride, Is.False);
            Assert.That(OwnershipLabelToggle.IsVisible(true), Is.True);
            Assert.That(OwnershipLabelToggle.IsVisible(false), Is.False);

            OwnershipLabelToggle.SetVisible(false);
            Assert.That(OwnershipLabelToggle.IsVisible(true), Is.False);

            OwnershipLabelToggle.ClearOverride();
            Assert.That(OwnershipLabelToggle.IsVisible(true), Is.True);
        }

        [Test]
        public void Toggle_FlipsTheEffectiveValue_AndRaisesChangedOnlyOnChange()
        {
            int raised = 0;
            void Count() => raised++;
            OwnershipLabelToggle.Changed += Count;
            try
            {
                OwnershipLabelToggle.Toggle(true);
                Assert.That(OwnershipLabelToggle.IsVisible(true), Is.False);
                Assert.That(raised, Is.EqualTo(1));

                OwnershipLabelToggle.SetVisible(false);
                Assert.That(raised, Is.EqualTo(1));

                OwnershipLabelToggle.Toggle(true);
                Assert.That(OwnershipLabelToggle.IsVisible(true), Is.True);
                Assert.That(raised, Is.EqualTo(2));

                OwnershipLabelToggle.ClearOverride();
                Assert.That(raised, Is.EqualTo(3));
                OwnershipLabelToggle.ClearOverride();
                Assert.That(raised, Is.EqualTo(3));
            }
            finally
            {
                OwnershipLabelToggle.Changed -= Count;
            }
        }
    }
}
