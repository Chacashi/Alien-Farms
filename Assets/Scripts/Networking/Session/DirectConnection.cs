using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Direct IP connection (Multiplayer Play Mode, LAN or the relay fallback): sets the UnityTransport address and port,
    /// then starts NGO synchronously.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DirectConnection : MonoBehaviour, IConnectionStrategy
    {
        [Header("References")]
        [SerializeField, Tooltip("UnityTransport on this root.")]
        private UnityTransport _transport;

        [Header("Endpoint")]
        [SerializeField, Tooltip("Address the client connects to.")]
        private string _address = "127.0.0.1";

        [SerializeField, Range(1, 65535), Tooltip("Port the host listens on and the client connects to.")]
        private int _port = 7777;

        [SerializeField, Tooltip("Address the host listens on (0.0.0.0 = every interface, so LAN clients can join).")]
        private string _listenAddress = "0.0.0.0";

        public string Address => _address;

        public int Port => _port;

        /// <summary>Overrides the address clients connect to (e.g. typed in a dev panel).</summary>
        public void SetAddress(string address)
        {
            if (!string.IsNullOrWhiteSpace(address))
            {
                _address = address.Trim();
            }
        }

        public Task<bool> StartHostAsync(NetworkManager manager)
        {
            if (!Configure(manager))
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(manager.StartHost());
        }

        public Task<bool> StartClientAsync(NetworkManager manager)
        {
            if (!Configure(manager))
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(manager.StartClient());
        }

        private bool Configure(NetworkManager manager)
        {
            if (manager == null || _transport == null)
            {
                Debug.LogError($"[{nameof(DirectConnection)}] Missing NetworkManager or UnityTransport on '{name}'.", this);
                return false;
            }

            if (manager.IsListening || manager.ShutdownInProgress)
            {
                Debug.LogWarning($"[{nameof(DirectConnection)}] NetworkManager is already running or shutting down; start ignored.", this);
                return false;
            }

            _transport.SetConnectionData(_address, (ushort)_port, _listenAddress);
            return true;
        }

        private void OnValidate()
        {
            if (_transport == null)
            {
                TryGetComponent(out _transport);
            }

            if (string.IsNullOrWhiteSpace(_address))
            {
                Debug.LogWarning($"[{nameof(DirectConnection)}] Address is empty on '{name}'.", this);
            }
        }
    }
}
