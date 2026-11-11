using System.Reflection;
using AlienFarms.Core;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class GameEventsNetworkTests
    {
        private int _calls;
        private PlayerId _lastPlayer;
        private PlayerId _lastVictim;
        private int _lastInt;
        private bool _lastBool;
        private float _lastFloat;

        [SetUp]
        public void SetUp()
        {
            _calls = 0;
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.OnPenInventoryChanged -= HandlePen;
            GameEvents.OnPlayerConnectionChanged -= HandleConnection;
            GameEvents.OnTackleResolved -= HandleTackle;
        }

        [Test]
        public void RaisePenInventoryChanged_DeliversPayload()
        {
            GameEvents.OnPenInventoryChanged += HandlePen;
            GameEvents.RaisePenInventoryChanged(PlayerId.Player2, 1, 4);

            Assert.That(_calls, Is.EqualTo(1));
            Assert.That(_lastPlayer, Is.EqualTo(PlayerId.Player2));
            Assert.That(_lastInt, Is.EqualTo(4));
        }

        [Test]
        public void RaisePlayerConnectionChanged_DeliversPayload()
        {
            GameEvents.OnPlayerConnectionChanged += HandleConnection;
            GameEvents.RaisePlayerConnectionChanged(PlayerId.Player1, false, 20f);

            Assert.That(_calls, Is.EqualTo(1));
            Assert.That(_lastPlayer, Is.EqualTo(PlayerId.Player1));
            Assert.That(_lastBool, Is.False);
            Assert.That(_lastFloat, Is.EqualTo(20f));
        }

        [Test]
        public void RaiseTackleResolved_DeliversPayload()
        {
            GameEvents.OnTackleResolved += HandleTackle;
            GameEvents.RaiseTackleResolved(PlayerId.Player1, PlayerId.Player2, true);

            Assert.That(_calls, Is.EqualTo(1));
            Assert.That(_lastPlayer, Is.EqualTo(PlayerId.Player1));
            Assert.That(_lastVictim, Is.EqualTo(PlayerId.Player2));
            Assert.That(_lastBool, Is.True);
        }

        [Test]
        public void ResetStatics_ClearsNewEvents()
        {
            GameEvents.OnPenInventoryChanged += HandlePen;
            GameEvents.OnPlayerConnectionChanged += HandleConnection;
            GameEvents.OnTackleResolved += HandleTackle;

            MethodInfo reset = typeof(GameEvents).GetMethod("ResetStatics", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(reset, Is.Not.Null, "GameEvents.ResetStatics not found.");
            reset.Invoke(null, null);

            GameEvents.RaisePenInventoryChanged(PlayerId.Player1, 0, 1);
            GameEvents.RaisePlayerConnectionChanged(PlayerId.Player1, true, 0f);
            GameEvents.RaiseTackleResolved(PlayerId.Player1, PlayerId.Player2, false);

            Assert.That(_calls, Is.EqualTo(0));
        }

        private void HandlePen(PlayerId playerId, int typeIndex, int count)
        {
            _calls++;
            _lastPlayer = playerId;
            _lastInt = count;
        }

        private void HandleConnection(PlayerId playerId, bool connected, float graceSeconds)
        {
            _calls++;
            _lastPlayer = playerId;
            _lastBool = connected;
            _lastFloat = graceSeconds;
        }

        private void HandleTackle(PlayerId attacker, PlayerId victim, bool accepted)
        {
            _calls++;
            _lastPlayer = attacker;
            _lastVictim = victim;
            _lastBool = accepted;
        }
    }
}
