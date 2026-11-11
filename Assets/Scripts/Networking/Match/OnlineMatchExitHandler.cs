using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Match;
using AlienFarms.Networking.Data;
using AlienFarms.Networking.Session;

namespace AlienFarms.Networking.Match
{
    /// <summary>
    /// Online match exit (version 1.5). Back to title: clears the session, tears the NetworkRoot down (shutdown +
    /// destroy; the MainMenu brings a fresh one) and loads the main menu. Rematch: only the server restarts the match;
    /// a client's request is ignored until NetworkMatchExit (1.6) forwards it.
    /// </summary>
    public sealed class OnlineMatchExitHandler : IMatchExitHandler
    {
        private readonly NetworkRuntimeAnchor _anchor;
        private readonly MatchManager _match;
        private readonly MatchSession _session;

        public OnlineMatchExitHandler(NetworkRuntimeAnchor anchor, MatchManager match, MatchSession session)
        {
            _anchor = anchor;
            _match = match;
            _session = session;
        }

        public void RequestRematch()
        {
            // Unity null check: the match may be gone during teardown.
            if (_anchor != null && _anchor.IsServer && _match != null)
            {
                _match.RequestRematch();
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
