using AlienFarms.Networking.Session;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests
{
    /// <summary>Payload name: typed, else UGS, else the fallback tagged per MPPM instance.</summary>
    public sealed class DisplayNameResolverTests
    {
        [Test]
        public void TypedName_Wins()
        {
            Assert.That(DisplayNameResolver.Resolve("Lindy", "Brave#1234", "Player", "player2"), Is.EqualTo("Lindy"));
        }

        [Test]
        public void TypedName_IsSanitized()
        {
            Assert.That(DisplayNameResolver.Resolve("  <b>Lindy</b> ", null, "Player", null), Is.EqualTo("bLindy/b"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void NoTypedName_UsesUgsName(string typed)
        {
            Assert.That(DisplayNameResolver.Resolve(typed, "Brave#1234", "Player", "player2"), Is.EqualTo("Brave#1234"));
        }

        [Test]
        public void NoTypedNorUgsName_MainEditor_UsesFallback()
        {
            Assert.That(DisplayNameResolver.Resolve(null, null, "Player", null), Is.EqualTo("Player"));
        }

        [Test]
        public void NoTypedNorUgsName_Clones_GetDistinctNames()
        {
            string clone2 = DisplayNameResolver.Resolve(null, null, "Player", "player2");
            string clone3 = DisplayNameResolver.Resolve(null, null, "Player", "player3");
            string main = DisplayNameResolver.Resolve(null, null, "Player", null);

            Assert.That(clone2, Is.EqualTo("Player player2"));
            Assert.That(clone2, Is.Not.EqualTo(clone3));
            Assert.That(clone2, Is.Not.EqualTo(main));
        }

        [Test]
        public void EmptyFallback_UsesDefaultName()
        {
            Assert.That(DisplayNameResolver.Resolve(null, null, "  ", null), Is.EqualTo(DisplayNameResolver.DefaultName));
        }
    }
}
