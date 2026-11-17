using AlienFarms.Core;

namespace AlienFarms.Match
{
    /// <summary>
    /// Round over: publishes the result, freezes players and restarts on Confirm after a short delay. A forfeit result
    /// (the rival left during Playing) stays on screen with one player; the rematch still needs both.
    /// </summary>
    public sealed class ResultsState : MatchState
    {
        private float _elapsed;
        private bool _isForfeit;

        public ResultsState(IMatchContext context) : base(context)
        {
        }

        public override MatchPhase Phase => MatchPhase.Results;

        /// <summary>
        /// Both players present (a missing one means the next Tick aborts to Waiting, except after a forfeit) and the results input delay has
        /// elapsed (scaled time, the match clock). Single source of truth for the rematch and the results menu.
        /// </summary>
        public override bool IsRematchReady
        {
            get
            {
                return Context.JoinedPlayerCount >= RequiredPlayers && IsResultsInputReady;
            }
        }

        /// <summary>The results input delay has elapsed (scaled time, the match clock), players present or not.</summary>
        public override bool IsResultsInputReady
        {
            get
            {
                float delay = Context.Settings != null ? Context.Settings.ResultsInputDelay : 0f;
                return _elapsed >= delay;
            }
        }

        public override void Enter()
        {
            Context.SetPlayerControlsEnabled(false);
            _elapsed = 0f;
            MatchResult result = MatchResultRules.Resolve(Context.Score.Evaluate(), Context.ForfeitWinner);
            _isForfeit = result.IsForfeit;
            GameEvents.RaiseMatchEnded(result);
        }

        public override void Tick(float deltaTime)
        {
            // After a forfeit the missing player is expected: keep the results instead of aborting the round.
            if (!_isForfeit && AbortIfPlayerMissing())
            {
                return;
            }

            _elapsed += deltaTime;
        }

        public override void OnConfirmPressed(PlayerId playerId)
        {
            if (IsRematchReady)
            {
                Context.ChangeState(MatchPhase.Countdown);
            }
        }
    }
}
