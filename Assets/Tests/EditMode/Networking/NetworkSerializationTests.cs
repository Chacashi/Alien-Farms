using System;
using AlienFarms.Animals;
using AlienFarms.Core;
using AlienFarms.Items;
using AlienFarms.Networking.Animals;
using AlienFarms.Networking.Items;
using AlienFarms.Networking.Match;
using AlienFarms.Networking.Player;
using AlienFarms.Networking.Session;
using AlienFarms.Zones;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Tests
{
    /// <summary>
    /// Wire sizes and limit values of every network-serialized struct: round-trips through FastBufferWriter/Reader with
    /// extreme fields, combined bits, raw out-of-range enum bytes and hostile counts.
    /// </summary>
    public sealed class NetworkSerializationTests
    {
        private const int Protocol = 1;

        // ---------------------------------------------------------------- PlayerSnapshot

        [Test]
        public void PlayerSnapshot_Is50Bytes()
        {
            // uint + double + 2 x Vector3 + 2 x float + uint + byte + byte.
            Assert.That(Serialize(new PlayerSnapshot()).Length, Is.EqualTo(50));
        }

        [Test]
        public void PlayerSnapshot_RoundTripsLimitValues()
        {
            var snapshot = new PlayerSnapshot
            {
                Tick = uint.MaxValue,
                ServerTime = 86400.123456789d,
                Position = new Vector3(float.MaxValue, float.MinValue, -0.0001f),
                Velocity = new Vector3(-1e6f, 1e-7f, 0f),
                FacingYaw = 359.999f,
                StunRemaining = float.Epsilon,
                AckSeq = uint.MaxValue,
                TeleportEpoch = byte.MaxValue,
                Flags = (PlayerSnapshotFlags)0xFF
            };

            PlayerSnapshot result = RoundTrip(snapshot);

            Assert.That(result.Tick, Is.EqualTo(uint.MaxValue));
            Assert.That(result.ServerTime, Is.EqualTo(snapshot.ServerTime));
            Assert.That(result.Position, Is.EqualTo(snapshot.Position));
            Assert.That(result.Velocity, Is.EqualTo(snapshot.Velocity));
            Assert.That(result.FacingYaw, Is.EqualTo(snapshot.FacingYaw));
            Assert.That(result.StunRemaining, Is.EqualTo(float.Epsilon));
            Assert.That(result.AckSeq, Is.EqualTo(uint.MaxValue));
            Assert.That(result.TeleportEpoch, Is.EqualTo(byte.MaxValue));
            // Unknown bits survive as the raw byte.
            Assert.That((byte)result.Flags, Is.EqualTo(0xFF));
        }

        [TestCase(PlayerSnapshotFlags.None)]
        [TestCase(PlayerSnapshotFlags.Sprinting)]
        [TestCase(PlayerSnapshotFlags.Aiming)]
        [TestCase(PlayerSnapshotFlags.Sprinting | PlayerSnapshotFlags.Aiming)]
        public void PlayerSnapshot_FlagsRoundTrip(PlayerSnapshotFlags flags)
        {
            Assert.That(RoundTrip(new PlayerSnapshot { Flags = flags }).Flags, Is.EqualTo(flags));
        }

        // ---------------------------------------------------------------- PlayerInputCommand

        [Test]
        public void InputCommand_Is11Bytes()
        {
            Assert.That(Serialize(PlayerInputCommand.Create(1, Vector2.one, 10f, 10f, true, true, true, true)).Length, Is.EqualTo(11));
        }

        [Test]
        public void InputCommand_RoundTripsRawLimitValues()
        {
            var command = new PlayerInputCommand
            {
                Seq = uint.MaxValue,
                MoveX = sbyte.MinValue,
                MoveY = sbyte.MaxValue,
                YawQuantized = ushort.MaxValue,
                PitchCentidegrees = short.MinValue,
                Buttons = byte.MaxValue
            };

            PlayerInputCommand result = RoundTrip(command);

            Assert.That(result.Seq, Is.EqualTo(uint.MaxValue));
            Assert.That(result.MoveX, Is.EqualTo(sbyte.MinValue));
            Assert.That(result.MoveY, Is.EqualTo(sbyte.MaxValue));
            Assert.That(result.YawQuantized, Is.EqualTo(ushort.MaxValue));
            Assert.That(result.PitchCentidegrees, Is.EqualTo(short.MinValue));
            Assert.That(result.Buttons, Is.EqualTo(byte.MaxValue));
        }

        [Test]
        public void InputCommand_CreatedExtremesRoundTripWithEveryBitSet()
        {
            PlayerInputCommand command = PlayerInputCommand.Create(uint.MaxValue, new Vector2(-1f, 0f), 0f, -500f, true, true, true, true);

            PlayerInputCommand result = RoundTrip(command);

            Assert.That(result.MoveX, Is.EqualTo((sbyte)-127));
            Assert.That(result.Move.x, Is.EqualTo(-1f));
            Assert.That(result.Pitch, Is.EqualTo(-90f));
            Assert.That(result.Buttons, Is.EqualTo(PlayerInputCommand.SprintBit | PlayerInputCommand.GrabBit | PlayerInputCommand.AimBit | PlayerInputCommand.PausedBit));
            Assert.That(result.IsSprintHeld && result.IsGrabHeld && result.IsAimHeld && result.IsPaused, Is.True);
        }

        [TestCase(360f)]
        [TestCase(720f)]
        [TestCase(359.999f)]
        public void InputCommand_FullTurnYawWrapsToZero(float yaw)
        {
            PlayerInputCommand command = PlayerInputCommand.Create(1, Vector2.zero, yaw, 0f, false, false, false);

            Assert.That(command.YawQuantized, Is.EqualTo((ushort)0));
            Assert.That(command.Yaw, Is.EqualTo(0f));
        }

        // ---------------------------------------------------------------- InputCommandBatch

        [TestCase(0, 1)]
        [TestCase(1, 12)]
        [TestCase(2, 23)]
        [TestCase(3, 34)]
        [TestCase(5, 34)]
        public void Batch_SizeIsCountPlusElevenBytesPerCommand(int pushed, int expectedBytes)
        {
            var batch = new InputCommandBatch();
            for (int i = 1; i <= pushed; i++)
            {
                batch.Push(PlayerInputCommand.Create((uint)i, Vector2.zero, 0f, 0f, false, false, false));
            }

            Assert.That(Serialize(batch).Length, Is.EqualTo(expectedBytes));
        }

        [Test]
        public void Batch_EmptyRoundTripsAndOutOfRangeGetsAreDefault()
        {
            InputCommandBatch result = RoundTrip(new InputCommandBatch());

            Assert.That(result.Count, Is.EqualTo(0));
            Assert.That(result.Get(0).Seq, Is.EqualTo(0u));
            Assert.That(result.Get(-1).Seq, Is.EqualTo(0u));
        }

        [Test]
        public void Batch_ClearDropsEverything()
        {
            var batch = new InputCommandBatch();
            batch.Push(PlayerInputCommand.Create(7, Vector2.one, 0f, 0f, true, false, false));

            batch.Clear();

            Assert.That(batch.Count, Is.EqualTo(0));
            Assert.That(batch.Get(0).Seq, Is.EqualTo(0u));
        }

        [Test]
        public void Batch_HostileCountIsClampedToMaxCommands()
        {
            byte[] bytes;
            using (var writer = new FastBufferWriter(64, Allocator.Temp))
            {
                writer.WriteValueSafe((byte)200);
                for (uint seq = 3; seq >= 1; seq--)
                {
                    writer.WriteNetworkSerializable(PlayerInputCommand.Create(seq, Vector2.zero, 0f, 0f, false, false, false));
                }

                bytes = writer.ToArray();
            }

            InputCommandBatch result;
            using (var reader = new FastBufferReader(bytes, Allocator.Temp))
            {
                reader.ReadNetworkSerializable(out result);
                Assert.That(reader.Position, Is.EqualTo(bytes.Length));
            }

            Assert.That(result.Count, Is.EqualTo(InputCommandBatch.MaxCommands));
            Assert.That(result.Get(0).Seq, Is.EqualTo(3u));
            Assert.That(result.Get(2).Seq, Is.EqualTo(1u));
        }

        // ---------------------------------------------------------------- AnimalNetState / BombNetState

        [Test]
        public void AnimalNetState_Is4BytesAndEveryEnumCombinationRoundTrips()
        {
            Assert.That(Serialize(new AnimalNetState()).Length, Is.EqualTo(4));

            foreach (AnimalState state in (AnimalState[])Enum.GetValues(typeof(AnimalState)))
            {
                foreach (PlayerId player in (PlayerId[])Enum.GetValues(typeof(PlayerId)))
                {
                    foreach (DropReason drop in (DropReason[])Enum.GetValues(typeof(DropReason)))
                    {
                        var value = new AnimalNetState(state, player, player, drop);
                        Assert.That(RoundTrip(value), Is.EqualTo(value), $"{state}/{player}/{drop}");
                    }
                }
            }
        }

        [Test]
        public void AnimalNetState_RawOutOfRangeBytesSurvive()
        {
            var value = new AnimalNetState((AnimalState)255, (PlayerId)255, (PlayerId)254, (DropReason)253);

            AnimalNetState result = RoundTrip(value);

            Assert.That((int)result.State, Is.EqualTo(255));
            Assert.That((int)result.Holder, Is.EqualTo(255));
            Assert.That((int)result.ZoneOwner, Is.EqualTo(254));
            Assert.That((int)result.LastDrop, Is.EqualTo(253));
        }

        [Test]
        public void BombNetState_Is3BytesAndEveryEnumCombinationRoundTrips()
        {
            Assert.That(Serialize(new BombNetState()).Length, Is.EqualTo(3));

            foreach (BombState state in (BombState[])Enum.GetValues(typeof(BombState)))
            {
                foreach (PlayerId holder in (PlayerId[])Enum.GetValues(typeof(PlayerId)))
                {
                    foreach (PlayerId thrower in (PlayerId[])Enum.GetValues(typeof(PlayerId)))
                    {
                        var value = new BombNetState(state, holder, thrower);
                        Assert.That(RoundTrip(value), Is.EqualTo(value), $"{state}/{holder}/{thrower}");
                    }
                }
            }

            Assert.That(default(BombNetState).State, Is.EqualTo(BombState.Inactive));
        }

        // ---------------------------------------------------------------- ScoreBoard / PenEntry

        [Test]
        public void ScoreBoard_Is8BytesAndRoundTripsIntLimits()
        {
            var board = new ScoreBoard(int.MinValue, int.MaxValue);

            Assert.That(Serialize(board).Length, Is.EqualTo(8));
            ScoreBoard result = RoundTrip(board);
            Assert.That(result.Player1, Is.EqualTo(int.MinValue));
            Assert.That(result.Player2, Is.EqualTo(int.MaxValue));
            Assert.That(result.ToResult().Winner, Is.EqualTo(PlayerId.Player2));
        }

        [Test]
        public void PenEntry_Is3BytesAndClampsToItsFieldWidths()
        {
            var high = new PenEntry(300, 70000);
            var low = new PenEntry(-4, 0);

            Assert.That(Serialize(high).Length, Is.EqualTo(3));
            Assert.That(high.AnimalTypeIndex, Is.EqualTo(byte.MaxValue));
            Assert.That(high.Count, Is.EqualTo(ushort.MaxValue));
            Assert.That(RoundTrip(high), Is.EqualTo(high));
            Assert.That(low.AnimalTypeIndex, Is.EqualTo(0));
            Assert.That(RoundTrip(low), Is.EqualTo(low));
        }

        // ---------------------------------------------------------------- ObstacleLayoutState

        [TestCase(0, 1)]
        [TestCase(1, 3)]
        [TestCase(31, 63)]
        [TestCase(40, 63)]
        public void ObstacleLayout_SizeIsCountPlusTwoBytesPerSlot(int placements, int expectedBytes)
        {
            var source = new ObstaclePlacement[placements];
            for (int i = 0; i < placements; i++)
            {
                source[i] = new ObstaclePlacement { Variant = 1, Yaw = i };
            }

            Assert.That(Serialize(ObstacleLayoutState.FromPlacements(source, placements)).Length, Is.EqualTo(expectedBytes));
        }

        [Test]
        public void ObstacleLayout_EmptyAndNullRoundTrip()
        {
            ObstacleLayoutState empty = RoundTrip(ObstacleLayoutState.FromPlacements(null, 5));

            Assert.That(empty.Count, Is.EqualTo(0));
            Assert.That(empty.CopyTo(new ObstaclePlacement[0]), Is.EqualTo(0));
            Assert.That(empty.Equals(new ObstacleLayoutState()), Is.True);
        }

        [Test]
        public void ObstacleLayout_CountIsLimitedByTheArrayLength()
        {
            var source = new ObstaclePlacement[2];

            Assert.That(ObstacleLayoutState.FromPlacements(source, 10).Count, Is.EqualTo(2));
        }

        [Test]
        public void ObstacleLayout_HighestVariantFitsAndTheNextOneIsDropped()
        {
            ushort highest = ObstacleLayoutState.Pack(new ObstaclePlacement { Variant = 62, Yaw = 359f });
            ushort overflow = ObstacleLayoutState.Pack(new ObstaclePlacement { Variant = 63, Yaw = 10f });

            ObstaclePlacement unpacked = ObstacleLayoutState.Unpack(highest);

            Assert.That(unpacked.IsActive, Is.True);
            Assert.That(unpacked.Variant, Is.EqualTo(62));
            Assert.That(unpacked.Yaw, Is.EqualTo(359f).Within(0.36f));
            Assert.That(overflow, Is.EqualTo((ushort)0));
            Assert.That(ObstacleLayoutState.Unpack(overflow).IsActive, Is.False);
            Assert.That(ObstacleLayoutState.Unpack(ushort.MaxValue).Variant, Is.EqualTo(62));
        }

        [Test]
        public void ObstacleLayout_HostileCountKeepsCapacityAndConsumesEveryByte()
        {
            const int sent = 40;
            byte[] bytes;
            using (var writer = new FastBufferWriter(128, Allocator.Temp))
            {
                writer.WriteValueSafe((byte)sent);
                for (int i = 0; i < sent; i++)
                {
                    writer.WriteValueSafe((ushort)((1 << 10) | i));
                }

                bytes = writer.ToArray();
            }

            ObstacleLayoutState result;
            using (var reader = new FastBufferReader(bytes, Allocator.Temp))
            {
                reader.ReadNetworkSerializable(out result);
                Assert.That(reader.Position, Is.EqualTo(bytes.Length));
            }

            var output = new ObstaclePlacement[ObstacleLayoutState.Capacity];
            Assert.That(result.Count, Is.EqualTo(ObstacleLayoutState.Capacity));
            Assert.That(result.CopyTo(output), Is.EqualTo(ObstacleLayoutState.Capacity));
            Assert.That(output[30].Variant, Is.EqualTo(0));
            Assert.That(output[30].Yaw, Is.EqualTo(30f * 360f / 1024f).Within(1e-4f));
        }

        // ---------------------------------------------------------------- ConnectionPayload

        [Test]
        public void Payload_SizeIsVersionPlusLengthPrefixedStrings()
        {
            ConnectionPayload payload = ConnectionPayload.Create(Protocol, "Ana", "abcde");

            // int + (uint + 3) + (uint + 5): no character byte since protocol 2.
            Assert.That(payload.ToBytes().Length, Is.EqualTo(4 + 4 + 3 + 4 + 5));
        }

        [Test]
        public void Payload_MaxLengthStringsRoundTrip()
        {
            string name = new string('n', FixedString32Bytes.UTF8MaxLengthInBytes);
            string ugsId = new string('u', FixedString64Bytes.UTF8MaxLengthInBytes);
            ConnectionPayload payload = ConnectionPayload.Create(int.MaxValue, name, ugsId);

            byte[] bytes = payload.ToBytes();
            bool parsed = ConnectionPayload.TryParse(bytes, out ConnectionPayload result);

            Assert.That(bytes.Length, Is.EqualTo(4 + 4 + 29 + 4 + 61));
            Assert.That(parsed, Is.True);
            Assert.That(result.ProtocolVersion, Is.EqualTo(int.MaxValue));
            Assert.That(result.DisplayName.ToString(), Is.EqualTo(name));
            Assert.That(result.UgsPlayerId.ToString(), Is.EqualTo(ugsId));
            Assert.That(result.HasUgsPlayerId, Is.True);
        }

        [Test]
        public void Payload_TooLongUgsIdIsTruncatedToCapacity()
        {
            ConnectionPayload payload = ConnectionPayload.Create(Protocol, "x", new string('u', 100));

            Assert.That(payload.UgsPlayerId.Length, Is.EqualTo(FixedString64Bytes.UTF8MaxLengthInBytes));
            Assert.That(ConnectionPayload.TryParse(payload.ToBytes(), out _), Is.True);
        }

        [Test]
        public void Payload_EmptyFieldsRoundTrip()
        {
            ConnectionPayload payload = ConnectionPayload.Create(int.MinValue, null);

            bool parsed = ConnectionPayload.TryParse(payload.ToBytes(), out ConnectionPayload result);

            Assert.That(parsed, Is.True);
            Assert.That(result.ProtocolVersion, Is.EqualTo(int.MinValue));
            Assert.That(result.DisplayName.Length, Is.EqualTo(0));
            Assert.That(result.UgsPlayerId.Length, Is.EqualTo(0));
            Assert.That(result.HasUgsPlayerId, Is.False);
        }

        [Test]
        public void Payload_MultiByteNameRoundTripsAndTruncatesOnACodePoint()
        {
            const string accented = "Ñandú \U0001F6F8";
            ConnectionPayload exact = ConnectionPayload.Create(Protocol, accented);
            ConnectionPayload.TryParse(exact.ToBytes(), out ConnectionPayload exactResult);

            // 20 two-byte characters = 40 bytes, more than the 29 the name holds.
            string longName = new string('ñ', 20);
            ConnectionPayload truncated = ConnectionPayload.Create(Protocol, longName);
            string truncatedText = truncated.DisplayName.ToString();

            Assert.That(exactResult.DisplayName.ToString(), Is.EqualTo(accented));
            Assert.That(truncated.DisplayName.Length, Is.LessThanOrEqualTo(FixedString32Bytes.UTF8MaxLengthInBytes));
            Assert.That(truncated.DisplayName.Length % 2, Is.EqualTo(0), "Truncation split a code point.");
            Assert.That(longName.StartsWith(truncatedText, StringComparison.Ordinal), Is.True);
            Assert.That(truncatedText.Length, Is.EqualTo(14));
        }

        [Test]
        public void Payload_UgsIdLengthPrefixOverCapacityIsRejected()
        {
            byte[] bytes = ConnectionPayload.Create(Protocol, "Ana", "id").ToBytes();

            // int + (uint + 3): the UGS id length prefix starts at byte 11.
            const int prefix = 4 + 4 + 3;
            byte[] overCapacity = (byte[])bytes.Clone();
            overCapacity[prefix] = (byte)(FixedString64Bytes.UTF8MaxLengthInBytes + 1);

            Assert.That(bytes[prefix], Is.EqualTo(2));
            Assert.That(ConnectionPayload.TryParse(bytes, out _), Is.True);
            Assert.That(ConnectionPayload.TryParse(overCapacity, out _), Is.False);
        }

        [Test]
        public void Payload_MaxSerializedSizeIsTheLargestValidPayload()
        {
            string name = new string('n', FixedString32Bytes.UTF8MaxLengthInBytes);
            string ugsId = new string('u', FixedString64Bytes.UTF8MaxLengthInBytes);

            Assert.That(ConnectionPayload.MaxSerializedSize, Is.EqualTo(102));
            Assert.That(ConnectionPayload.Create(Protocol, name, ugsId).ToBytes().Length, Is.EqualTo(ConnectionPayload.MaxSerializedSize));
        }

        [Test]
        public void Payload_NameLengthPrefixOverCapacityIsRejected()
        {
            byte[] bytes = ConnectionPayload.Create(Protocol, "Ana", "id").ToBytes();

            // The name length prefix starts right after the int version.
            byte[] overCapacity = (byte[])bytes.Clone();
            overCapacity[sizeof(int)] = (byte)(FixedString32Bytes.UTF8MaxLengthInBytes + 1);

            Assert.That(bytes[sizeof(int)], Is.EqualTo(3));
            Assert.That(ConnectionPayload.TryParse(overCapacity, out ConnectionPayload result), Is.False);
            Assert.That(result.ProtocolVersion, Is.EqualTo(Protocol), "The version is still answered as such.");
            Assert.That(result.DisplayName.Length, Is.EqualTo(0));
        }

        [TestCase(ConnectionPayload.MaxSerializedSize + 1)]
        [TestCase(64 * 1024)]
        public void Payload_OversizedDataIsRejectedKeepingOnlyTheVersion(int size)
        {
            byte[] valid = ConnectionPayload.Create(Protocol, "Ana", "id").ToBytes();
            byte[] huge = new byte[size];
            Array.Copy(valid, huge, valid.Length);

            Assert.That(ConnectionPayload.TryParse(huge, out ConnectionPayload result), Is.False);
            Assert.That(result.ProtocolVersion, Is.EqualTo(Protocol));
            Assert.That(result.DisplayName.Length, Is.EqualTo(0));
            Assert.That(result.UgsPlayerId.Length, Is.EqualTo(0));
        }

        [TestCase("  <b>Zorg</b>\n ", "bZorg/b")]
        [TestCase("\u0007\u200B", "")]
        [TestCase("Ana", "Ana")]
        public void Payload_WithSanitizedName_CleansOnlyTheName(string name, string expected)
        {
            ConnectionPayload payload = ConnectionPayload.Create(Protocol, name, "u1");

            ConnectionPayload clean = payload.WithSanitizedName();

            Assert.That(clean.DisplayName.ToString(), Is.EqualTo(expected));
            Assert.That(clean.UgsPlayerId.ToString(), Is.EqualTo("u1"));
            Assert.That(clean.ProtocolVersion, Is.EqualTo(Protocol));
        }

        [Test]
        public void Payload_TrailingBytesAreRejected()
        {
            byte[] bytes = ConnectionPayload.Create(Protocol, "Ana", "id").ToBytes();
            byte[] longer = new byte[bytes.Length + 1];
            Array.Copy(bytes, longer, bytes.Length);

            Assert.That(ConnectionPayload.TryParse(longer, out _), Is.False);
        }

        [Test]
        public void Payload_Protocol1LayoutFailsButKeepsItsVersion()
        {
            // Protocol 1 wrote int + name + CharacterIndex byte + UGS id.
            byte[] old = Protocol1Bytes(1, "Ana", 2, "player-1");

            bool parsed = ConnectionPayload.TryParse(old, out ConnectionPayload result);

            Assert.That(parsed, Is.False);
            Assert.That(result.ProtocolVersion, Is.EqualTo(1));
            Assert.That(result.DisplayName.Length, Is.EqualTo(0));
            Assert.That(result.UgsPlayerId.Length, Is.EqualTo(0));
        }

        [Test]
        public void Payload_ReadsBackThroughTheGenericNetworkSerializablePath()
        {
            ConnectionPayload payload = ConnectionPayload.Create(Protocol, "Zorg", "player-123");

            ConnectionPayload result = RoundTrip(payload);

            Assert.That(result.ProtocolVersion, Is.EqualTo(Protocol));
            Assert.That(result.DisplayName.ToString(), Is.EqualTo("Zorg"));
            Assert.That(result.UgsPlayerId.ToString(), Is.EqualTo("player-123"));
        }

        private static byte[] Protocol1Bytes(int version, string name, byte character, string ugsId)
        {
            using (var writer = new FastBufferWriter(256, Allocator.Temp))
            {
                var fixedName = new FixedString32Bytes(name);
                var fixedId = new FixedString64Bytes(ugsId);
                writer.WriteValueSafe(version);
                writer.WriteValueSafe(fixedName);
                writer.WriteValueSafe(character);
                writer.WriteValueSafe(fixedId);
                return writer.ToArray();
            }
        }

        // ---------------------------------------------------------------- Helpers

        private static byte[] Serialize<T>(T value) where T : INetworkSerializable
        {
            using (var writer = new FastBufferWriter(256, Allocator.Temp))
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
