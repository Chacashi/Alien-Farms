namespace AlienFarms.Input
{
    /// <summary>
    /// Plain C# hold-to-confirm timer: accumulates while a button is held and reports completion once. After completing,
    /// the button must be released before it can complete again; releasing early resets the progress.
    /// </summary>
    public sealed class HoldToPauseTracker
    {
        public const float MinDuration = 0.01f;

        private readonly float _duration;
        private float _elapsed;
        private bool _awaitingRelease;

        public HoldToPauseTracker(float duration)
        {
            // Also rejects NaN (comparison is false).
            _duration = duration >= MinDuration ? duration : MinDuration;
        }

        public float Duration => _duration;

        /// <summary>Normalized hold progress (0..1). Stays at 1 after completion until the button is released.</summary>
        public float Progress => _elapsed / _duration;

        /// <summary>
        /// Advances the hold by <paramref name="unscaledDeltaTime"/> while <paramref name="held"/>.
        /// Returns true only on the tick that completes the hold.
        /// </summary>
        public bool Tick(bool held, float unscaledDeltaTime)
        {
            if (!held)
            {
                Reset();
                return false;
            }

            if (_awaitingRelease)
            {
                return false;
            }

            if (unscaledDeltaTime > 0f)
            {
                _elapsed += unscaledDeltaTime;
            }

            if (_elapsed < _duration)
            {
                return false;
            }

            _elapsed = _duration;
            _awaitingRelease = true;
            return true;
        }

        /// <summary>Clears progress and re-arms the tracker.</summary>
        public void Reset()
        {
            _elapsed = 0f;
            _awaitingRelease = false;
        }
    }
}
