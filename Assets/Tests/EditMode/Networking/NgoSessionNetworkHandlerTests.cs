using System.Threading;
using System.Threading.Tasks;
using AlienFarms.Networking.Session;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace AlienFarms.Networking.Tests
{
    /// <summary>
    /// Single-use rule of <see cref="NgoSessionNetworkHandler"/>: once a start failed or the network was stopped, a later
    /// start (the SDK re-joining a failed session's network from an async void) neither starts NGO nor throws.
    /// </summary>
    public sealed class NgoSessionNetworkHandlerTests
    {
        private const float ConnectTimeoutSeconds = 5f;

        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
                _root = null;
            }
        }

        [Test]
        public void NewHandler_IsNotRetired()
        {
            var handler = new NgoSessionNetworkHandler(null, null, ConnectTimeoutSeconds);

            Assert.That(handler.Retired, Is.False);
            Assert.That(handler.Started, Is.False);
        }

        [Test]
        public void FailedStart_RetiresHandler()
        {
            var handler = new NgoSessionNetworkHandler(null, null, ConnectTimeoutSeconds);

            Task start = handler.StartAsync(null, CancellationToken.None);

            Assert.That(start.IsFaulted, Is.True);
            Assert.That(start.Exception?.InnerException, Is.TypeOf<SessionException>());
            Assert.That(handler.Retired, Is.True);
        }

        [Test]
        public void Shutdown_WhenNeverStarted_RetiresAndLeavesManagerAlone()
        {
            var handler = new NgoSessionNetworkHandler(null, null, ConnectTimeoutSeconds);

            Assert.That(handler.Shutdown(), Is.False);
            Assert.That(handler.Retired, Is.True);
        }

        [Test]
        public void StopAsync_WhenNeverStarted_RetiresHandler()
        {
            var handler = new NgoSessionNetworkHandler(null, null, ConnectTimeoutSeconds);

            Task stop = handler.StopAsync();

            Assert.That(stop.Status, Is.EqualTo(TaskStatus.RanToCompletion));
            Assert.That(handler.Retired, Is.True);
        }

        [Test]
        public void RetiredHandler_StartAsync_CompletesWithoutStartingNetworkManager()
        {
            _root = new GameObject(nameof(NgoSessionNetworkHandlerTests));
            UnityTransport transport = _root.AddComponent<UnityTransport>();
            NetworkManager manager = _root.AddComponent<NetworkManager>();
            var handler = new NgoSessionNetworkHandler(manager, transport, ConnectTimeoutSeconds);
            handler.Retire();

            // Null configuration: a non-retired handler would fail reading it; a retired one must not even look.
            Task first = handler.StartAsync(null, CancellationToken.None);
            Task second = handler.StartAsync(null);

            Assert.That(first.Status, Is.EqualTo(TaskStatus.RanToCompletion));
            Assert.That(second.Status, Is.EqualTo(TaskStatus.RanToCompletion));
            Assert.That(manager.IsListening, Is.False);
            Assert.That(handler.Started, Is.False);
            Assert.That(handler.Retired, Is.True);
        }

        [Test]
        public void HandlerWithManager_StartFailure_RetiresWithoutStartingNetworkManager()
        {
            _root = new GameObject(nameof(NgoSessionNetworkHandlerTests));
            UnityTransport transport = _root.AddComponent<UnityTransport>();
            NetworkManager manager = _root.AddComponent<NetworkManager>();
            var handler = new NgoSessionNetworkHandler(manager, transport, ConnectTimeoutSeconds);

            // Not retired: the start runs and fails on the missing configuration, which retires the handler.
            Task start = handler.StartAsync(null, CancellationToken.None);

            Assert.That(start.IsFaulted, Is.True);
            Assert.That(handler.Retired, Is.True);
            Assert.That(manager.IsListening, Is.False);

            Assert.That(handler.StartAsync(null).Status, Is.EqualTo(TaskStatus.RanToCompletion));
        }
    }
}
