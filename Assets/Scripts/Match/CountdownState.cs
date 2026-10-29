using AlienFarms.Core;
using UnityEngine;

namespace AlienFarms.Match
{
    /// <summary>Resets the round, freezes players and counts down before play starts.</summary>
    public sealed class CountdownState : MatchState
    {
        private float _remaining;
        private int _lastShown;

        public CountdownState(IMatchContext context) : base(context)
        {
        }

        public override MatchPhase Phase => MatchPhase.Countdown;

        public override void Enter()
        {
            Context.SetPlayerControlsEnabled(false);
            Context.ResetRound(true);

            _remaining = Context.Settings != null ? Context.Settings.CountdownSeconds : 0f;
            _lastShown = Mathf.CeilToInt(_remaining);
            GameEvents.RaiseCountdownTick(_lastShown);
        }

        public override void Tick(float deltaTime)
        {
            if (AbortIfPlayerMissing())
            {
                return;
            }

            _remaining -= deltaTime;
            if (_remaining <= 0f)
            {
                float goDuration = Context.Settings != null ? Context.Settings.GoMessageDuration : 0f;
                GameEvents.RaiseCountdownFinished(goDuration);
                Context.ChangeState(MatchPhase.Playing);
                return;
            }

            int shown = Mathf.CeilToInt(_remaining);
            if (shown != _lastShown)
            {
                _lastShown = shown;
                GameEvents.RaiseCountdownTick(shown);
            }
        }
    }
}
