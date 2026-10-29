using AlienFarms.Core;

namespace AlienFarms.Match
{
    /// <summary>Free roam until both players have joined; no captures.</summary>
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
