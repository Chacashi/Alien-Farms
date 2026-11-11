using AlienFarms.Networking.Player;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Tests
{
    /// <summary>Input command quantization and serialization, the command batch and the server source's edges.</summary>
    public sealed class InputCommandTests
    {
        [Test]
        public void Command_QuantizesWithinTolerance()
        {
            PlayerInputCommand command = PlayerInputCommand.Create(7, new Vector2(0.5f, -0.75f), 271.3f, -23.45f, true, false, true);

            Assert.That(command.Seq, Is.EqualTo(7u));
            Assert.That(command.Move.x, Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(command.Move.y, Is.EqualTo(-0.75f).Within(0.01f));
            Assert.That(command.Yaw, Is.EqualTo(271.3f).Within(0.01f));
            Assert.That(command.Pitch, Is.EqualTo(-23.45f).Within(0.01f));
            Assert.That(command.IsSprintHeld, Is.True);
            Assert.That(command.IsGrabHeld, Is.False);
            Assert.That(command.IsAimHeld, Is.True);
        }

        [Test]
        public void Command_ClampsMoveAndWrapsYaw()
        {
            PlayerInputCommand command = PlayerInputCommand.Create(1, new Vector2(3f, 4f), -90f, 200f, false, true, false);

            Assert.That(command.Move.magnitude, Is.EqualTo(1f).Within(0.02f));
            Assert.That(command.Yaw, Is.EqualTo(270f).Within(0.01f));
            Assert.That(command.Pitch, Is.EqualTo(90f).Within(0.01f));
        }

        [Test]
        public void Command_RoundTripsThroughSerialization()
        {
            PlayerInputCommand command = PlayerInputCommand.Create(123456, new Vector2(-0.25f, 0.75f), 10f, 30f, false, true, true);

            PlayerInputCommand result = RoundTrip(command);

            Assert.That(result.Seq, Is.EqualTo(command.Seq));
            Assert.That(result.MoveX, Is.EqualTo(command.MoveX));
            Assert.That(result.MoveY, Is.EqualTo(command.MoveY));
            Assert.That(result.YawQuantized, Is.EqualTo(command.YawQuantized));
            Assert.That(result.PitchCentidegrees, Is.EqualTo(command.PitchCentidegrees));
            Assert.That(result.Buttons, Is.EqualTo(command.Buttons));
        }

        [Test]
        public void Batch_KeepsTheLastThreeNewestFirst()
        {
            var batch = new InputCommandBatch();
            for (uint seq = 1; seq <= 5; seq++)
            {
                batch.Push(PlayerInputCommand.Create(seq, Vector2.zero, 0f, 0f, false, false, false));
            }

            Assert.That(batch.Count, Is.EqualTo(InputCommandBatch.MaxCommands));
            Assert.That(batch.Get(0).Seq, Is.EqualTo(5u));
            Assert.That(batch.Get(1).Seq, Is.EqualTo(4u));
            Assert.That(batch.Get(2).Seq, Is.EqualTo(3u));
            Assert.That(batch.Get(3).Seq, Is.EqualTo(0u));
        }

        [Test]
        public void Batch_RoundTripsPartialAndFull()
        {
            var partial = new InputCommandBatch();
            partial.Push(PlayerInputCommand.Create(9, Vector2.up, 45f, 5f, true, false, false));

            InputCommandBatch partialResult = RoundTrip(partial);
            Assert.That(partialResult.Count, Is.EqualTo(1));
            Assert.That(partialResult.Get(0).Seq, Is.EqualTo(9u));
            Assert.That(partialResult.Get(0).IsSprintHeld, Is.True);

            var full = new InputCommandBatch();
            for (uint seq = 10; seq <= 13; seq++)
            {
                full.Push(PlayerInputCommand.Create(seq, Vector2.zero, seq, 0f, false, seq % 2 == 0, false));
            }

            InputCommandBatch fullResult = RoundTrip(full);
            Assert.That(fullResult.Count, Is.EqualTo(3));
            Assert.That(fullResult.Get(0).Seq, Is.EqualTo(13u));
            Assert.That(fullResult.Get(2).Seq, Is.EqualTo(11u));
            Assert.That(fullResult.Get(1).IsGrabHeld, Is.True);
        }

        [Test]
        public void CommandSource_RaisesGrabEdgesOnly()
        {
            var source = new NetworkCommandSource();
            int started = 0;
            int canceled = 0;
            source.GrabStarted += () => started++;
            source.GrabCanceled += () => canceled++;

            source.Apply(PlayerInputCommand.Create(1, Vector2.right, 0f, 0f, false, true, false));
            source.Apply(PlayerInputCommand.Create(2, Vector2.right, 0f, 0f, false, true, false));
            source.Apply(PlayerInputCommand.Create(3, Vector2.zero, 0f, 0f, false, false, false));

            Assert.That(started, Is.EqualTo(1));
            Assert.That(canceled, Is.EqualTo(1));
            Assert.That(source.IsGrabHeld, Is.False);
            Assert.That(source.Look, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void Command_PausedBitIsIndependentAndRoundTrips()
        {
            PlayerInputCommand paused = PlayerInputCommand.Create(4, Vector2.zero, 0f, 0f, false, false, true, true);
            PlayerInputCommand notPaused = PlayerInputCommand.Create(5, Vector2.zero, 0f, 0f, true, true, true);

            PlayerInputCommand result = RoundTrip(paused);

            Assert.That(result.IsPaused, Is.True);
            Assert.That(result.IsGrabHeld, Is.False);
            Assert.That(result.IsAimHeld, Is.True);
            Assert.That(result.IsSprintHeld, Is.False);
            Assert.That(notPaused.IsPaused, Is.False);
        }

        [Test]
        public void CommandSource_TracksPausedAndResets()
        {
            var source = new NetworkCommandSource();

            source.Apply(PlayerInputCommand.Create(1, Vector2.zero, 0f, 0f, false, false, false, true));
            Assert.That(source.IsPaused, Is.True);

            source.Apply(PlayerInputCommand.Create(2, Vector2.zero, 0f, 0f, false, false, false));
            Assert.That(source.IsPaused, Is.False);

            source.Apply(PlayerInputCommand.Create(3, Vector2.zero, 0f, 0f, false, false, false, true));
            source.Reset();
            Assert.That(source.IsPaused, Is.False);
        }

        private static T RoundTrip<T>(T value) where T : INetworkSerializable, new()
        {
            using (var writer = new FastBufferWriter(256, Allocator.Temp))
            {
                writer.WriteNetworkSerializable(value);
                byte[] bytes = writer.ToArray();
                using (var reader = new FastBufferReader(bytes, Allocator.Temp))
                {
                    reader.ReadNetworkSerializable(out T result);
                    return result;
                }
            }
        }
    }
}
