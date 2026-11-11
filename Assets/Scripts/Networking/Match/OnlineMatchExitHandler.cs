using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Match;
using AlienFarms.Networking.Data;
using AlienFarms.Networking.Player;
using AlienFarms.Networking.Session;

namespace AlienFarms.Networking.Match
{
    /// <summary>
    /// Online match exit. Back to title: clears the session, tears the NetworkRoot down (shutdown + destroy; the
    /// MainMenu brings a fresh one) and loads the main menu. Rematch: marks the local player ready; the server's
    /// <see cref="NetworkMatchExit"/> restarts the match when both players are.
    /// </summary>
    public sealed class OnlineMatchExitHandler : IMatchExitHandler
    {
        private readonly NetworkRuntimeAnchor _anchor;
        private readonly MatchSession _session;

        public OnlineMatchExitHandler(NetworkRuntimeAnchor anchor, MatchSession session)
        {
            _anchor = anchor;
            _session = session;
        }

        /// <summary>
        /// Marks this machine's player as ready (owner-written NetworkVariable); <see cref="NetworkMatchExit"/> on the
        /// server starts the rematch once both players are ready.
        /// </summary>
        public void RequestRematch()
        {
            NetworkPlayer local = _anchor != null ? _anchor.LocalPlayer : null;
            if (local != null)
            {
                local.SetReadyForRematch(true);
            }
        }

        public bool ReturnToTitle()
        {
            if (_session != null)
            {
                _session.Clear();
            }

            NetworkRootLifetime root = _anchor != null ? _anchor.Root : null;
            if (root != null)
            {
                root.Teardown();
            }

            return SceneFlow.LoadMainMenu();
        }
    }
}
