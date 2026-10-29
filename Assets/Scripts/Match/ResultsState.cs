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
            // A missing player means the next Tick aborts to Waiting; never restart a round that cannot be played.
            if (Context.JoinedPlayerCount < RequiredPlayers)
            {
                return;
            }

            float delay = Context.Settings != null ? Context.Settings.ResultsInputDelay : 0f;
            if (_elapsed >= delay)
            {
                Context.ChangeState(MatchPhase.Countdown);
            }
        }
    }
}
