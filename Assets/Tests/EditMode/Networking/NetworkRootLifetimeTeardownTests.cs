using System.Threading.Tasks;
using AlienFarms.Networking.Session;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AlienFarms.Networking.Tests
{
    /// <summary>
    /// Root teardown without a running NetworkManager: the deferred continuation runs once, a second teardown is ignored,
    /// and a destroy in the middle of the graceful shutdown releases the task and drops the continuation (B1).
    /// </summary>
    public sealed class NetworkRootLifetimeTeardownTests
    {
        private const float Notice = 0.5f;

        private GameObject _go;
        private NetworkRootLifetime _root;
        private int _tornDown;

        [SetUp]
        public void SetUp()
        {
            _tornDown = 0;
            _go = new GameObject("RootUnderTest");

            // The bare root has no references: keep its OnValidate warnings out of the console.
            bool logEnabled = Debug.unityLogger.logEnabled;
            Debug.unityLogger.logEnabled = false;
            try
            {
                _root = _go.AddComponent<NetworkRootLifetime>();
            }
            finally
            {
                Debug.unityLogger.logEnabled = logEnabled;
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
            }
        }

        private void CountTornDown()
        {
            _tornDown++;
        }

        [Test]
        public void Teardown_NoSession_DestroysAndRunsTheContinuationOnce()
        {
            _root.Teardown(CountTornDown);

            Assert.That(_tornDown, Is.EqualTo(1));
            Assert.That(_go == null, Is.True);
        }

        [Test]
        public void Teardown_DuringGracefulShutdown_WaitsThenRunsTheContinuationOnce()
        {
            _root.BeginHostShutdown(0d, Notice);
            _root.Teardown(CountTornDown);

            Assert.That(_tornDown, Is.Zero, "the root stays alive until NGO stopped");
            Assert.That(_go != null, Is.True);

            // No NetworkManager: not listening, so the sequence goes to Stopping, then completes.
            _root.StepShutdown(0.1d);
            _root.StepShutdown(0.2d);

            Assert.That(_tornDown, Is.EqualTo(1));
            Assert.That(_go == null, Is.True);
        }

        [Test]
        public void Teardown_SecondCall_IsIgnored()
        {
            int second = 0;
            _root.BeginHostShutdown(0d, Notice);
            _root.Teardown(CountTornDown);

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Teardown already in progress"));
            _root.Teardown(() => second++);

            _root.StepShutdown(0.1d);
            _root.StepShutdown(0.2d);

            Assert.That(_tornDown, Is.EqualTo(1));
            Assert.That(second, Is.Zero);
        }

        [Test]
        public void DestroyedMidSequence_CompletesTheTaskAndDropsTheContinuation()
        {
            Task task = _root.BeginHostShutdown(0d, Notice);
            _root.Teardown(CountTornDown);

            _root.ReleaseShutdownOnDestroy();

            Assert.That(task.IsCompleted, Is.True);
            Assert.That(_root.IsShutdownPending, Is.False);

            _root.StepShutdown(5d);
            Assert.That(_tornDown, Is.Zero);
        }

        [Test]
        public void BeginHostShutdown_RaisesHostLeaveStartedOnce_AndShutdownTaskIsShared()
        {
            int raised = 0;
            _root.HostLeaveStarted += () => raised++;

            Task task = _root.BeginHostShutdown(0d, Notice);

            Assert.That(raised, Is.EqualTo(1));
            Assert.That(_root.IsShutdownPending, Is.True);
            Assert.That(_root.ShutdownSessionAsync(), Is.SameAs(task));
            Assert.That(task.IsCompleted, Is.False);

            _root.StepShutdown(0.1d);
            _root.StepShutdown(0.2d);

            Assert.That(task.IsCompleted, Is.True);
            Assert.That(raised, Is.EqualTo(1));
        }
    }
}
