using Unity.Netcode;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// "The host is leaving" state of the server's connection handling (plain C#, owned by
    /// <see cref="ConnectionApprovalHandler"/>). Requested when the root starts the host's leave; from then the run's
    /// disconnections are the shutdown, not drops. It lasts exactly one run: cleared when NGO reports the server stopped
    /// (the seat table is cleared in the same step, so a late disconnection finds nothing to reserve) and again when a new
    /// session starts, since the root survives a Leave in the lobby and is reused for the next room.
    /// </summary>
    public sealed class ServerStopLatch
    {
        private bool _requested;

        /// <summary>True between <see cref="Request"/> and the next <see cref="OnServerStopped"/> / <see cref="Reset"/>.</summary>
        public bool IsRequested => _requested;

        /// <summary>The host starts leaving. Ignored off the server. True when it was not requested yet.</summary>
        public bool Request(bool isServer)
        {
            if (_requested || !isServer)
            {
                return false;
            }

            _requested = true;
            return true;
        }

        /// <summary>NGO stopped the server (after the run's slots were cleared).</summary>
        public void OnServerStopped()
        {
            _requested = false;
        }

        /// <summary>A new session is about to start (before StartHost / StartClient) or a new server started.</summary>
        public void Reset()
        {
            _requested = false;
        }

        /// <summary>
        /// Whether a connection request is refused because the host is leaving. The host's own connection is never refused
        /// (NGO cannot decline it), whatever state is left over.
        /// </summary>
        public static bool ShouldRejectJoin(ulong clientId, bool serverStopping)
        {
            return serverStopping && clientId != NetworkManager.ServerClientId;
        }
    }
}
