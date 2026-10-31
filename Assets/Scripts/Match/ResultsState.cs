using AlienFarms.Core;

namespace AlienFarms.Match
{
    /// <summary>Round over: publishes the result, freezes players and restarts on Confirm after a short delay.</summary>
    public sealed class ResultsState : MatchState
    {
        private float _elapsed;

        public ResultsState(IMatchContext context) : base(context)
        {
        }

        public override MatchPhase Phase => MatchPhase.Results;

        /// <summary>
        /// Both players present (a missing one means the next Tick aborts to Waiting) and the results input delay has
        /// elapsed (scaled time, the match clock). Single source of truth for the rematch and the results menu.
        /// </summary>
        public override bool IsRematchReady
        {
            get
            {
                if (Context.JoinedPlayerCount < RequiredPlayers)
                {
                    return false;
                }

                float delay = Context.Settings != null ? Context.Settings.ResultsInputDelay : 0f;
                return _elapsed >= delay;
            }
        }

        public override void Enter()
        {
            Context.SetPlayerControlsEnabled(false);
            _elapsed = 0f;
            GameEvents.RaiseMatchEnded(Context.Score.Evaluate());
        }

        public override void Tick(float deltaTime)
        {
            if (AbortIfPlayerMissing())
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
