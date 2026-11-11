using AlienFarms.Networking.Session;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests
{
    /// <summary>Authentication profile per process: one per MPPM virtual player, explicit override, sanitization.</summary>
    public sealed class UgsProfileResolverTests
    {
        [Test]
        public void Resolve_MainEditor_UsesDefaultProfile()
        {
            string[] args = { "Unity.exe", "-name", "Player1" };

            Assert.That(UgsProfileResolver.Resolve(args, true), Is.Null);
        }

        [Test]
        public void Resolve_VirtualPlayer_UsesItsMppmName()
        {
            string[] args = { "Unity.exe", "-projectPath", "X", "-name", "Player2" };

            Assert.That(UgsProfileResolver.Resolve(args, false), Is.EqualTo("player2"));
        }

        [Test]
        public void Resolve_TwoVirtualPlayers_GetDifferentProfiles()
        {
            string second = UgsProfileResolver.Resolve(new[] { "-name", "Player2" }, false);
            string third = UgsProfileResolver.Resolve(new[] { "-name", "Player3" }, false);

            Assert.That(second, Is.Not.EqualTo(third));
        }

        [Test]
        public void Resolve_ExplicitProfile_WinsEvenOnMainEditor()
        {
            string[] args = { "-name", "Player2", "-ugs-profile", "PcB" };

            Assert.That(UgsProfileResolver.Resolve(args, true), Is.EqualTo("pcb"));
            Assert.That(UgsProfileResolver.Resolve(args, false), Is.EqualTo("pcb"));
        }

        [Test]
        public void Resolve_NoArguments_UsesDefaultProfile()
        {
            Assert.That(UgsProfileResolver.Resolve(new[] { "Game.exe" }, false), Is.Null);
            Assert.That(UgsProfileResolver.Resolve(null, false), Is.Null);
        }

        [Test]
        public void Resolve_ArgumentWithoutValue_IsIgnored()
        {
            Assert.That(UgsProfileResolver.Resolve(new[] { "-name" }, false), Is.Null);
            Assert.That(UgsProfileResolver.Resolve(new[] { "-name", "-batchmode" }, false), Is.Null);
        }

        [Test]
        public void Sanitize_DropsInvalidCharactersAndLowerCases()
        {
            Assert.That(UgsProfileResolver.Sanitize("Player 2!"), Is.EqualTo("player2"));
            Assert.That(UgsProfileResolver.Sanitize("a_b-C"), Is.EqualTo("a_b-c"));
        }

        [Test]
        public void Sanitize_NothingValid_ReturnsNull()
        {
            Assert.That(UgsProfileResolver.Sanitize("   "), Is.Null);
            Assert.That(UgsProfileResolver.Sanitize("!!!"), Is.Null);
            Assert.That(UgsProfileResolver.Sanitize(null), Is.Null);
        }

        [Test]
        public void Sanitize_LongName_IsTruncatedToTheAuthenticationLimit()
        {
            string result = UgsProfileResolver.Sanitize(new string('a', 50));

            Assert.That(result.Length, Is.EqualTo(UgsProfileResolver.MaxProfileLength));
        }

        [Test]
        public void Sanitize_Result_MatchesTheAuthenticationProfileRule()
        {
            string result = UgsProfileResolver.Sanitize("Ünïcode Player#4");

            Assert.That(result, Does.Match("^[a-zA-Z0-9_-]{1,30}$"));
        }
    }
}
