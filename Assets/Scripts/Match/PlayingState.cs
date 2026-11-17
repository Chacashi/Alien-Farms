using AlienFarms.Core;

namespace AlienFarms.Match
{
    /// <summary>
    /// Round in progress: timer runs (1 Hz event) and score changes are published. A missing player either aborts the
    /// round (local) or ends it as a forfeit won by the player who stayed (<see cref="IMatchContext.ForfeitOnPlayerMissing"/>).
    /// </summary>
    public sealed class PlayingState : MatchState
    {
        public PlayingState(IMatchContext context) : base(context)
        {
        }

        public override MatchPhase Phase => MatchPhase.Playing;

        // Invariant: no animal is Captured on entry (the countdown's ResetRound despawns them all and controls are off).
        // The score only tracks captures made while playing, so an animal already in a pen here would never count.
        public override void Enter()
        {
            Context.SetPlayerControlsEnabled(true);
            GameEvents.RaiseMatchTimeChanged(Context.Timer.DisplaySeconds);
        }

        public override void Tick(float deltaTime)
        {
            if (TryForfeitIfPlayerMissing() || AbortIfPlayerMissing())
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

        /// <summary>
        /// True (and the round ends in Results) when forfeits are enabled and exactly one player is still present.
        /// With nobody left the round aborts as usual.
        /// </summary>
        private bool TryForfeitIfPlayerMissing()
        {
            if (!Context.ForfeitOnPlayerMissing || Context.JoinedPlayerCount >= RequiredPlayers)
            {
                return false;
            }

            bool player1 = Context.IsPlayerPresent(PlayerId.Player1);
            bool player2 = Context.IsPlayerPresent(PlayerId.Player2);
            if (!MatchResultRules.TryGetSoleRemaining(player1, player2, out PlayerId winner))
            {
                return false;
            }

            Context.EndByForfeit(winner);
            return true;
        }
    }
}
