using AlienFarms.Networking.Session;
using NUnit.Framework;

namespace AlienFarms.Networking.Tests
{
    /// <summary>Spectator role from the host-named session property, position only as fallback (B2).</summary>
    public sealed class ParticipantRoleRulesTests
    {
        private const int Seats = 2;

        private static bool[] Classify(string[] ids, bool[] isHost, string named)
        {
            var result = new bool[ids.Length];
            ParticipantRoleRules.Classify(ids, isHost, named, Seats, result);
            return result;
        }

        [Test]
        public void HostClientSpectator_Named_SpectatorIsThird()
        {
            bool[] result = Classify(new[] { "H", "C", "S" }, new[] { true, false, false }, "S");

            Assert.That(result, Is.EqualTo(new[] { false, false, true }));
        }

        [Test]
        public void ClientLeft_NamedSpectatorStaysSpectator()
        {
            bool[] result = Classify(new[] { "H", "S" }, new[] { true, false }, "S");

            Assert.That(result, Is.EqualTo(new[] { false, true }));
        }

        [Test]
        public void ClientLeft_NoName_FallsBackToPosition()
        {
            // The old behaviour, kept only until the host wrote the property.
            bool[] result = Classify(new[] { "H", "S" }, new[] { true, false }, null);

            Assert.That(result, Is.EqualTo(new[] { false, false }));
        }

        [Test]
        public void NewPlayerAfterSpectator_TakesTheFreeSeat()
        {
            bool[] result = Classify(new[] { "H", "S", "N" }, new[] { true, false, false }, "S");

            Assert.That(result, Is.EqualTo(new[] { false, true, false }));
        }

        [Test]
        public void NoName_ThirdMemberIsSpectator()
        {
            bool[] result = Classify(new[] { "H", "C", "S" }, new[] { true, false, false }, "");

            Assert.That(result, Is.EqualTo(new[] { false, false, true }));
        }

        [Test]
        public void ClientLeft_EmptyName_IsNobody_FallsBackToPosition()
        {
            // The host writes NoSpectator ("") instead of deleting the key.
            bool[] result = Classify(new[] { "H", "C", "S" }, new[] { true, false, false }, ParticipantRoleRules.NoSpectator);

            Assert.That(result, Is.EqualTo(new[] { false, false, true }));
        }

        [TestCase(null)]
        [TestCase("")]
        public void ReadSpectatorId_NullOrEmpty_IsNobody(string value)
        {
            Assert.That(ParticipantRoleRules.ReadSpectatorId(value), Is.Null);
        }

        [Test]
        public void ReadSpectatorId_Id_IsKept()
        {
            Assert.That(ParticipantRoleRules.ReadSpectatorId("S"), Is.EqualTo("S"));
        }

        [Test]
        public void ShouldWriteSpectator_UnchangedAndSaved_DoesNotWrite()
        {
            Assert.That(ParticipantRoleRules.ShouldWriteSpectator("S", "S", false), Is.False);
            Assert.That(ParticipantRoleRules.ShouldWriteSpectator(null, null, false), Is.False);
            Assert.That(ParticipantRoleRules.ShouldWriteSpectator(null, "", false), Is.False, "null and empty both mean nobody");
        }

        [Test]
        public void ShouldWriteSpectator_Changed_Writes()
        {
            Assert.That(ParticipantRoleRules.ShouldWriteSpectator("S", null, false), Is.True);
            Assert.That(ParticipantRoleRules.ShouldWriteSpectator(null, "S", false), Is.True);
            Assert.That(ParticipantRoleRules.ShouldWriteSpectator("T", "S", false), Is.True);
        }

        [Test]
        public void ShouldWriteSpectator_FailedSavePending_WritesEvenIfTheValueLooksTheSame()
        {
            // The SDK's local copy already shows the unsaved value; only a successful save clears the pending flag.
            Assert.That(ParticipantRoleRules.ShouldWriteSpectator("S", "S", true), Is.True);
            Assert.That(ParticipantRoleRules.ShouldWriteSpectator(null, null, true), Is.True);
        }

        [Test]
        public void StaleNameNotInSession_RealPlayersKeepTheirSeats()
        {
            bool[] result = Classify(new[] { "H", "C" }, new[] { true, false }, "gone");

            Assert.That(result, Is.EqualTo(new[] { false, false }));
        }

        [Test]
        public void HostIsNeverSpectator_EvenIfNamed()
        {
            bool[] result = Classify(new[] { "H", "C" }, new[] { true, false }, "H");

            Assert.That(result, Is.EqualTo(new[] { false, false }));
        }
    }
}
