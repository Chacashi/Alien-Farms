using AlienFarms.Input;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class DeferredDisableGateTests
    {
        [Test]
        public void EnableWhileDisabled_EnablesImmediately()
        {
            var gate = new DeferredDisableGate();

            Assert.That(gate.Request(true, false), Is.True);
            Assert.That(gate.IsDisablePending, Is.False);
        }

        [Test]
        public void EnableWhileEnabled_DoesNothing()
        {
            var gate = new DeferredDisableGate();

            Assert.That(gate.Request(true, true), Is.False);
            Assert.That(gate.ConsumePendingDisable(), Is.False);
        }

        [Test]
        public void DisableWhileEnabled_IsDeferred()
        {
            var gate = new DeferredDisableGate();

            Assert.That(gate.Request(false, true), Is.False);
            Assert.That(gate.IsDisablePending, Is.True);
        }

        [Test]
        public void DisableWhileDisabled_IsNotPending()
        {
            var gate = new DeferredDisableGate();

            gate.Request(false, false);

            Assert.That(gate.ConsumePendingDisable(), Is.False);
        }

        [Test]
        public void PendingDisable_IsConsumedOnce()
        {
            var gate = new DeferredDisableGate();
            gate.Request(false, true);

            Assert.That(gate.ConsumePendingDisable(), Is.True);
            Assert.That(gate.ConsumePendingDisable(), Is.False);
        }

        [Test]
        public void LaterEnable_CancelsPendingDisable()
        {
            var gate = new DeferredDisableGate();
            gate.Request(false, true);

            Assert.That(gate.Request(true, true), Is.False);
            Assert.That(gate.ConsumePendingDisable(), Is.False);
        }
    }
}
