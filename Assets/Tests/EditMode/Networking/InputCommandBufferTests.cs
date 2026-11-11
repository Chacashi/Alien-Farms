using AlienFarms.Networking.Player;
using NUnit.Framework;
using UnityEngine;

namespace AlienFarms.Networking.Tests
{
    /// <summary>Server jitter buffer: priming, duplicates, stale and lost commands, starvation and trimming.</summary>
    public sealed class InputCommandBufferTests
    {
        [Test]
        public void Consume_WaitsForTargetDepthBeforePlaying()
        {
            var buffer = new InputCommandBuffer(2);
            buffer.Enqueue(Command(1));

            PlayerInputCommand first = buffer.Consume(out bool firstFresh);
            buffer.Enqueue(Command(2));
            PlayerInputCommand second = buffer.Consume(out bool secondFresh);

            Assert.That(firstFresh, Is.False);
            Assert.That(first.Seq, Is.EqualTo(0u));
            Assert.That(secondFresh, Is.True);
            Assert.That(second.Seq, Is.EqualTo(1u));
            Assert.That(buffer.LastConsumedSeq, Is.EqualTo(1u));
        }

        [Test]
        public void Enqueue_DropsDuplicatesAndStale()
        {
            var buffer = new InputCommandBuffer(0);
            Assert.That(buffer.Enqueue(Command(1)), Is.True);
            Assert.That(buffer.Enqueue(Command(1)), Is.False);
            buffer.Consume(out _);

            Assert.That(buffer.Enqueue(Command(1)), Is.False);
            Assert.That(buffer.Enqueue(Command(0)), Is.False);
            Assert.That(buffer.PendingCount, Is.EqualTo(0));
        }

        [Test]
        public void Consume_PlaysInOrderEvenWhenReceivedOutOfOrder()
        {
            var buffer = new InputCommandBuffer(2);
            buffer.Enqueue(Command(3));
            buffer.Enqueue(Command(1));
            buffer.Enqueue(Command(2));

            Assert.That(buffer.Consume(out _).Seq, Is.EqualTo(1u));
            Assert.That(buffer.Consume(out _).Seq, Is.EqualTo(2u));
            Assert.That(buffer.Consume(out _).Seq, Is.EqualTo(3u));
        }

        [Test]
        public void Consume_SkipsAGapWhenALaterCommandArrived()
        {
            var buffer = new InputCommandBuffer(1);
            buffer.Enqueue(Command(1));
            buffer.Consume(out _);
            buffer.Enqueue(Command(3));

            PlayerInputCommand next = buffer.Consume(out bool fresh);

            Assert.That(fresh, Is.True);
            Assert.That(next.Seq, Is.EqualTo(3u));
        }

        [Test]
        public void Consume_RepeatsLastWhenStarved()
        {
            var buffer = new InputCommandBuffer(1);
            buffer.Enqueue(Command(1, grab: true));
            buffer.Consume(out _);

            PlayerInputCommand repeated = buffer.Consume(out bool fresh);

            Assert.That(fresh, Is.False);
            Assert.That(repeated.Seq, Is.EqualTo(1u));
            Assert.That(repeated.IsGrabHeld, Is.True);
        }

        [Test]
        public void Consume_TrimsBacklogBeyondTargetPlusExtra()
        {
            var buffer = new InputCommandBuffer(2);
            for (uint seq = 1; seq <= 10; seq++)
            {
                buffer.Enqueue(Command(seq));
            }

            PlayerInputCommand next = buffer.Consume(out _);

            // 10 queued, keep at most target + extra (4): the oldest are dropped, then the next one plays.
            Assert.That(next.Seq, Is.EqualTo(7u));
            Assert.That(buffer.PendingCount, Is.EqualTo(3));
        }

        [Test]
        public void Enqueue_FarAheadResynchronizes()
        {
            var buffer = new InputCommandBuffer(0);
            buffer.Enqueue(Command(1));
            buffer.Consume(out _);

            Assert.That(buffer.Enqueue(Command(1 + InputCommandBuffer.Capacity + 5)), Is.True);
            Assert.That(buffer.Consume(out _).Seq, Is.EqualTo((uint)(1 + InputCommandBuffer.Capacity + 5)));
        }

        [Test]
        public void Sequence_WrapsAroundAndSkipsZero()
        {
            var buffer = new InputCommandBuffer(0);
            buffer.Enqueue(Command(uint.MaxValue - 1));
            Assert.That(buffer.Consume(out _).Seq, Is.EqualTo(uint.MaxValue - 1));

            Assert.That(buffer.Enqueue(Command(uint.MaxValue)), Is.True);
            Assert.That(buffer.Enqueue(Command(1)), Is.True);
            Assert.That(buffer.Enqueue(Command(uint.MaxValue - 1)), Is.False);

            Assert.That(buffer.Consume(out _).Seq, Is.EqualTo(uint.MaxValue));
            Assert.That(buffer.Consume(out _).Seq, Is.EqualTo(1u));
        }

        [Test]
        public void Enqueue_AbsurdJumpIsIgnored()
        {
            var buffer = new InputCommandBuffer(0);
            buffer.Enqueue(Command(1));
            buffer.Consume(out _);

            Assert.That(buffer.Enqueue(Command(1 + InputCommandBuffer.MaxResyncDistance + 1)), Is.False);
            Assert.That(buffer.Enqueue(Command(2)), Is.True);
            Assert.That(buffer.Consume(out _).Seq, Is.EqualTo(2u));
        }

        [Test]
        public void Trim_CarriesAGrabFromDroppedCommands()
        {
            var buffer = new InputCommandBuffer(2);
            buffer.Enqueue(Command(1));
            buffer.Enqueue(Command(2, grab: true));
            for (uint seq = 3; seq <= 10; seq++)
            {
                buffer.Enqueue(Command(seq));
            }

            PlayerInputCommand next = buffer.Consume(out _);

            Assert.That(next.Seq, Is.EqualTo(7u));
            Assert.That(next.IsGrabHeld, Is.True);
            Assert.That(buffer.Consume(out _).IsGrabHeld, Is.False);
        }

        private static PlayerInputCommand Command(uint seq, bool grab = false)
        {
            return PlayerInputCommand.Create(seq, Vector2.up, 0f, 0f, false, grab, false);
        }
    }
}
