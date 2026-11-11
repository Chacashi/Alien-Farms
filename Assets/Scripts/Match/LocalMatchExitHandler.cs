using AlienFarms.Core;
using AlienFarms.Data;

namespace AlienFarms.Match
{
    /// <summary>
    /// Local split-screen exit: rematch through the <see cref="MatchManager"/>; returning to the title clears the
    /// <see cref="MatchSession"/> and loads the main menu (time and audio are restored by <see cref="SceneFlow"/>).
    /// </summary>
    public sealed class LocalMatchExitHandler : IMatchExitHandler
    {
        private readonly MatchManager _match;
        private readonly MatchSession _session;

        public LocalMatchExitHandler(MatchManager match, MatchSession session)
        {
            _match = match;
            _session = session;
        }

        public void RequestRematch()
        {
            // Unity null check: the match may have been destroyed during teardown.
            if (_match != null)
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

            return SceneFlow.LoadMainMenu();
        }
    }
}
