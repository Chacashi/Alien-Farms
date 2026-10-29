using UnityEngine;

namespace AlienFarms.Match
{
    /// <summary>Countdown of the round time. Driven externally with <see cref="Tick"/> (no Time.* access).</summary>
    public sealed class MatchTimer
    {
        private float _remaining;

        public MatchTimer(float duration)
        {
            Reset(duration);
        }

        public float Remaining => _remaining;

        public bool IsFinished => _remaining <= 0f;

        /// <summary>Whole seconds shown to players (299.2 -> 300, 0.01 -> 1, 0 -> 0).</summary>
        public int DisplaySeconds => Mathf.CeilToInt(_remaining);

        public void Reset(float duration)
        {
            _remaining = Mathf.Max(0f, duration);
        }

        /// <summary>Advances the timer. Returns true only on the tick that makes it reach zero.</summary>
        public bool Tick(float deltaTime)
        {
            if (_remaining <= 0f)
            {
                return false;
            }

            _remaining = Mathf.Max(0f, _remaining - deltaTime);
            return _remaining <= 0f;
        }
    }
}
