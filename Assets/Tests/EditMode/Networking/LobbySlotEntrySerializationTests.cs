using AlienFarms.Core;
using AlienFarms.Networking.Session;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode;

namespace AlienFarms.Networking.Tests
{
    /// <summary>Wire format and equality of <see cref="LobbySlotEntry"/>, the NetworkList element of the online room.</summary>
    public sealed class LobbySlotEntrySerializationTests
    {
        [Test]
        public void EmptyName_Is14Bytes()
        {
            // byte id + ulong client + byte character + uint name length.
            Assert.That(Serialize(new LobbySlotEntry()).Length, Is.EqualTo(14));
        }

        [Test]
        public void Name_AddsItsUtf8Bytes()
        {
            var entry = new LobbySlotEntry(PlayerId.Player1, 0, 0, new FixedString32Bytes("Ana"));

            Assert.That(Serialize(entry).Length, Is.EqualTo(14 + 3));
        }

        [Test]
        public void RoundTrip_LimitValues()
        {
            var name = new FixedString32Bytes();
            name.CopyFromTruncated("ÑandúÑandúÑandúÑandúÑandú-long-name");
            var entry = new LobbySlotEntry(PlayerId.Player2, ulong.MaxValue, 254, name);

            LobbySlotEntry result = RoundTrip(entry);

            Assert.That(result.Id, Is.EqualTo(PlayerId.Player2));
            Assert.That(result.ClientId, Is.EqualTo(ulong.MaxValue));
            Assert.That(result.CharacterIndex, Is.EqualTo(254));
            Assert.That(result.DisplayName, Is.EqualTo(name));
            Assert.That(result, Is.EqualTo(entry));
        }

        [Test]
        public void RoundTrip_NoCharacter()
        {
            var entry = new LobbySlotEntry(PlayerId.Player1, 3, ConnectionPayload.NoCharacter, new FixedString32Bytes("Host"));

            LobbySlotEntry result = RoundTrip(entry);

            Assert.That(result.HasCharacter, Is.False);
            Assert.That(result.CharacterIndex, Is.EqualTo(ConnectionPayload.NoCharacter));
        }

        [Test]
        public void RoundTrip_UnknownIdByteSurvives()
        {
            var entry = new LobbySlotEntry((PlayerId)200, 1, 0, default);

            Assert.That((int)RoundTrip(entry).Id, Is.EqualTo(200));
        }

        [Test]
        public void Equality_ComparesEveryField()
        {
            var name = new FixedString32Bytes("Ana");
            var entry = new LobbySlotEntry(PlayerId.Player1, 5, 1, name);

            Assert.That(entry.Equals(new LobbySlotEntry(PlayerId.Player1, 5, 1, name)), Is.True);
            Assert.That(entry.GetHashCode(), Is.EqualTo(new LobbySlotEntry(PlayerId.Player1, 5, 1, name).GetHashCode()));
            Assert.That(entry.Equals(new LobbySlotEntry(PlayerId.Player2, 5, 1, name)), Is.False);
            Assert.That(entry.Equals(new LobbySlotEntry(PlayerId.Player1, 6, 1, name)), Is.False);
            Assert.That(entry.Equals(new LobbySlotEntry(PlayerId.Player1, 5, 2, name)), Is.False);
            Assert.That(entry.Equals(new LobbySlotEntry(PlayerId.Player1, 5, 1, new FixedString32Bytes("Bob"))), Is.False);
            Assert.That(entry.Equals((object)entry), Is.True);
            Assert.That(entry.Equals(null), Is.False);
        }

        [Test]
        public void WithCharacter_ChangesOnlyThePick()
        {
            var entry = new LobbySlotEntry(PlayerId.Player2, 5, ConnectionPayload.NoCharacter, new FixedString32Bytes("Ana"));

            LobbySlotEntry picked = entry.WithCharacter(3);

            Assert.That(picked.CharacterIndex, Is.EqualTo(3));
            Assert.That(picked.HasCharacter, Is.True);
            Assert.That(picked.Id, Is.EqualTo(entry.Id));
            Assert.That(picked.ClientId, Is.EqualTo(entry.ClientId));
            Assert.That(picked.DisplayName, Is.EqualTo(entry.DisplayName));
        }

        private static byte[] Serialize<T>(T value) where T : INetworkSerializable
        {
            using (var writer = new FastBufferWriter(64, Allocator.Temp))
            {
                writer.WriteNetworkSerializable(value);
                return writer.ToArray();
            }
        }

        private static T RoundTrip<T>(T value) where T : INetworkSerializable, new()
        {
            byte[] bytes = Serialize(value);
            using (var reader = new FastBufferReader(bytes, Allocator.Temp))
            {
                reader.ReadNetworkSerializable(out T result);
                Assert.That(reader.Position, Is.EqualTo(bytes.Length), $"{typeof(T).Name} left unread bytes.");
                return result;
            }
        }
    }
}
