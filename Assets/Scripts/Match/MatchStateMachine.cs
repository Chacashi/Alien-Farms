using AlienFarms.Core;

namespace AlienFarms.Match
{
    /// <summary>
    /// Plain C# match state machine: owns one state per <see cref="MatchPhase"/> and performs transitions.
    /// Side effects (players, animals, score, timer) live in the <see cref="IMatchContext"/>.
    /// </summary>
    public sealed class MatchStateMachine
    {
        // Indexed by (int)MatchPhase; slot 0 (None) stays null.
        private readonly MatchState[] _states;
        private readonly IMatchContext _context;
        private MatchState _current;

        public MatchStateMachine(IMatchContext context)
        {
            _context = context;
            _states = new MatchState[(int)MatchPhase.Results + 1];
            _states[(int)MatchPhase.WaitingForPlayers] = new WaitingForPlayersState(context);
            _states[(int)MatchPhase.Countdown] = new CountdownState(context);
            _states[(int)MatchPhase.Playing] = new PlayingState(context);
            _states[(int)MatchPhase.Results] = new ResultsState(context);
        }

        public MatchPhase CurrentPhase => _current != null ? _current.Phase : MatchPhase.None;

        /// <summary>
        /// Exit current -> switch -> raise phase event -> Enter next. The event goes before Enter so listeners such as
        /// the CaptureReferee are already updated when Enter forces drops. Returning to WaitingForPlayers from any other
        /// phase aborts the round: ResetRound(false) runs before Enter (score/timer cleared, animals repopulated,
        /// remaining player not teleported). The very first Waiting does not reset.
        /// Returns false for a phase without state.
        /// </summary>
        public bool ChangeState(MatchPhase next)
        {
            int index = (int)next;
            if (index <= 0 || index >= _states.Length || _states[index] == null)
            {
                return false;
            }

            MatchPhase previousPhase = CurrentPhase;
            _current?.Exit();

            _current = _states[index];
            GameEvents.RaiseMatchPhaseChanged(previousPhase, next);

            if (next == MatchPhase.WaitingForPlayers && previousPhase != MatchPhase.None)
            {
                _context.ResetRound(false);
            }

            _current.Enter();
            return true;
        }

        public void Tick(float deltaTime) => _current?.Tick(deltaTime);

        public void OnConfirmPressed(PlayerId playerId) => _current?.OnConfirmPressed(playerId);
    }
}
