using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Match;
using AlienFarms.Networking.Data;
using AlienFarms.Networking.Persistence;
using AlienFarms.Networking.Player;
using AlienFarms.Networking.Session;

namespace AlienFarms.Networking.Match
{
    /// <summary>
    /// Online match exit. Back to title: records a loss first when leaving during live Playing (persisted before the
    /// shutdown, see <see cref="OnlineMatchStatsRecorder.RecordLeaveLoss"/>), clears the session, on a pure client sends the voluntary-leave notice and drops
    /// any rejoin offer, tears the NetworkRoot down (shutdown + destroy; the
    /// MainMenu brings a fresh one) and loads the main menu, which opens on the online hub. Rematch: marks the local player ready; the server's
    /// <see cref="NetworkMatchExit"/> restarts the match when both players are.
    /// </summary>
    public sealed class OnlineMatchExitHandler : IMatchExitHandler
    {
        private readonly NetworkRuntimeAnchor _anchor;
        private readonly MatchSession _session;
        private readonly OnlineMatchStatsRecorder _stats;

        /// <param name="stats">Optional: records the loss of leaving during Playing (null = nothing recorded).</param>
        public OnlineMatchExitHandler(NetworkRuntimeAnchor anchor, MatchSession session, OnlineMatchStatsRecorder stats = null)
        {
            _anchor = anchor;
            _session = session;
            _stats = stats;
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
            // Before anything is torn down: the ledger flushes the loss to PlayerPrefs on enqueue.
            if (_stats != null)
            {
                _stats.RecordLeaveLoss();
            }

            if (_session != null)
            {
                _session.Clear();
                // Leaving an online match lands on the online hub, not on the title.
                _session.RequestReturnToOnlineMenu();
            }

            NetworkRootLifetime root = _anchor != null ? _anchor.Root : null;
            if (root != null)
            {
                // Pure client leaving on purpose: the server frees the seat at once instead of holding it for the grace
                // (NGO flushes the notice before closing; if it is lost, the session's PlayerHasLeft or the grace does it).
                if (_anchor.IsPureClient)
                {
                    if (root.LeaveNotice != null)
                    {
                        root.LeaveNotice.SendLeavingNotice();
                    }

                    if (root.SessionService != null)
                    {
                        root.SessionService.ClearRejoinOffer();
                    }
                    else
                    {
                        _anchor.ClearRejoinSession();
                    }
                }

                root.Teardown();
            }

            return SceneFlow.LoadMainMenu();
        }
    }
}
