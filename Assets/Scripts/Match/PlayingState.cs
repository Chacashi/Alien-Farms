using AlienFarms.Core;

namespace AlienFarms.Match
{
    /// <summary>Round in progress: timer runs (1 Hz event) and captures are enabled.</summary>
    public sealed class PlayingState : MatchState
    {
        public PlayingState(IMatchContext context) : base(context)
        {
        }

        public override MatchPhase Phase => MatchPhase.Playing;

        public override void Enter()
        {
            Context.SetPlayerControlsEnabled(true);
            GameEvents.RaiseMatchTimeChanged(Context.Timer.DisplaySeconds);
        }

        public override void Tick(float deltaTime)
        {
            if (AbortIfPlayerMissing())
            {
                return;
            }

            MatchTimer timer = Context.Timer;
            int previous = timer.DisplaySeconds;
            bool finished = timer.Tick(deltaTime);

            int current = timer.DisplaySeconds;
            if (current != previous)
            {
                GameEvents.RaiseMatchTimeChanged(current);
            }

            if (finished)
            {
                Context.ChangeState(MatchPhase.Results);
            }
        }
    }
}
