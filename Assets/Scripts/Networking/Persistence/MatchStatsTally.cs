using AlienFarms.Core;

namespace AlienFarms.Networking.Persistence
{
    /// <summary>
    /// Decides which round this instance records (plain C#): only one whose Playing phase it saw live, once. A
    /// reconnection that enters straight into Results (the replica re-raises the result) never counts again.
    /// Countdown and WaitingForPlayers start over.
    /// </summary>
    public sealed class MatchStatsTally
    {
        private MatchPhase _phase = MatchPhase.None;
        private bool _sawPlaying;
        private bool _completed;

        /// <summary>This instance saw the current round's Playing phase.</summary>
        public bool SawPlayingLive => _sawPlaying;

        /// <summary>The current round was already recorded.</summary>
        public bool IsCompleted => _completed;

        /// <summary>In Playing, seen live and not recorded yet (what an early leave or a host drop would record).</summary>
        public bool IsRoundInProgress => _phase == MatchPhase.Playing && _sawPlaying && !_completed;

        public void OnPhaseChanged(MatchPhase previous, MatchPhase current)
        {
            _phase = current;
            switch (current)
            {
                case MatchPhase.Countdown:
                case MatchPhase.WaitingForPlayers:
                case MatchPhase.None:
                    _sawPlaying = false;
                    _completed = false;
                    break;
                case MatchPhase.Playing:
                    _sawPlaying = true;
                    break;
            }
        }

        /// <summary>
        /// The round ended with <paramref name="result"/>: true (once) with the local player's increment when it was seen
        /// live and <paramref name="local"/> is a player (spectators record nothing).
        /// </summary>
        public bool TryComplete(PlayerId local, in MatchResult result, int animals, int tackles, out PlayerStatsSummary delta)
        {
            delta = default;
            if (!_sawPlaying || _completed || (local != PlayerId.Player1 && local != PlayerId.Player2))
            {
                return false;
            }

            _completed = true;
            delta = PlayerStatsRules.ForMatch(local, result, animals, tackles);
            return true;
        }

        /// <summary>
        /// The round stops early for this instance during live Playing: <paramref name="localWon"/> false = this player left
        /// on purpose (loss), true = the host left (win). True (once) with the increment.
        /// </summary>
        public bool TryCompleteEarly(bool localWon, int animals, int tackles, out PlayerStatsSummary delta)
        {
            delta = default;
            if (!IsRoundInProgress)
            {
                return false;
            }

            _completed = true;
            delta = localWon ? PlayerStatsRules.Win(animals, tackles) : PlayerStatsRules.Loss(animals, tackles);
            return true;
        }
    }
}
