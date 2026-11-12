using AlienFarms.Core;
using AlienFarms.Networking.Player;
using NUnit.Framework;
using UnityEngine;

namespace AlienFarms.Networking.Tests
{
    /// <summary>Online name tag rules: visibility, name fallback, color fallback and the "client N" debug line (task 2.7).</summary>
    public sealed class NameTagRulesTests
    {
        [TestCase(true, true, false, PlayerId.Player2, true)]
        [TestCase(true, true, false, PlayerId.Player1, true)]
        [TestCase(true, true, true, PlayerId.Player2, false)]
        [TestCase(true, false, false, PlayerId.Player2, false)]
        [TestCase(false, true, false, PlayerId.Player2, false)]
        [TestCase(true, true, false, PlayerId.None, false)]
        public void IsVisible_OnlyOverOtherSeatedSpawnedAvatars(bool spawned, bool showTags, bool isLocalOwner, PlayerId seat, bool expected)
        {
            Assert.That(NameTagRules.IsVisible(spawned, showTags, isLocalOwner, seat), Is.EqualTo(expected));
        }

        [Test]
        public void ResolveName_UsesDisplayName()
        {
            Assert.That(NameTagRules.ResolveName("Ana#1234", PlayerId.Player2), Is.EqualTo("Ana#1234"));
        }

        [TestCase(null, PlayerId.Player1, CarriableOwnerRules.Player1Tag)]
        [TestCase("", PlayerId.Player2, CarriableOwnerRules.Player2Tag)]
        [TestCase("", PlayerId.None, CarriableOwnerRules.UnresolvedTag)]
        public void ResolveName_WithoutName_FallsBackToSeatTag(string displayName, PlayerId seat, string expected)
        {
            Assert.That(NameTagRules.ResolveName(displayName, seat), Is.EqualTo(expected));
        }

        [Test]
        public void ResolveColor_WithoutCharacter_IsFallback()
        {
            Assert.That(NameTagRules.ResolveColor(null, Color.cyan), Is.EqualTo(Color.cyan));
        }

        [Test]
        public void ClientDetail_On_ShowsClientId()
        {
            Assert.That(NameTagRules.ClientDetail(true, 3), Is.EqualTo("client 3"));
        }

        [Test]
        public void ClientDetail_Off_IsNull()
        {
            Assert.That(NameTagRules.ClientDetail(false, 3), Is.Null);
        }
    }
}
