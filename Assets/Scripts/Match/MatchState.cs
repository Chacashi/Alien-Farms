using AlienFarms.Core;

namespace AlienFarms.Match
{
    /// <summary>
    /// Base of the match State pattern. Plain C# (testable in EditMode).
    /// Rule: no state calls <see cref="IMatchContext.ChangeState"/> from <see cref="Enter"/>; transitions happen only in
    /// <see cref="Tick"/> or <see cref="OnConfirmPressed"/>, so the phase event always precedes the next Enter.
    /// </summary>
    public abstract class MatchState
    {
        protected const int RequiredPlayers = 2;

        protected MatchState(IMatchContext context)
        {
            Context = context;
        }

        public abstract MatchPhase Phase { get; }

        protected IMatchContext Context { get; }

        /// <summary>True when <see cref="OnConfirmPressed"/> would start a rematch right now.</summary>
        public virtual bool IsRematchReady => false;

        public virtual void Enter()
        {
        }

        public virtual void Tick(float deltaTime)
        {
        }

        public virtual void Exit()
        {
        }

        public virtual void OnConfirmPressed(PlayerId playerId)
        {
        }

        /// <summary>True (and transitions to WaitingForPlayers) when a player is missing.</summary>
        protected bool AbortIfPlayerMissing()
        {
            if (Context.JoinedPlayerCount >= RequiredPlayers)
            {
                return false;
            }

            Context.ChangeState(MatchPhase.WaitingForPlayers);
            return true;
        }
    }
}
