using AlienFarms.Core;

namespace AlienFarms.Match
{
    /// <summary>Free roam until both players have joined; captures are not scored (the countdown resets the round).</summary>
    public sealed class WaitingForPlayersState : MatchState
    {
        public WaitingForPlayersState(IMatchContext context) : base(context)
        {
        }

        public override MatchPhase Phase => MatchPhase.WaitingForPlayers;

        public override void Enter()
        {
            Context.SetPlayerControlsEnabled(true);
        }

        public override void Tick(float deltaTime)
        {
            if (Context.JoinedPlayerCount >= RequiredPlayers)
            {
                Context.ChangeState(MatchPhase.Countdown);
            }
        }
    }
}
