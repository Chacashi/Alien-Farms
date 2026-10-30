using AlienFarms.Utilities;
using UnityEngine;

namespace AlienFarms.Items
{
    /// <summary>
    /// Pure spawn timer for bombs. Counts down only while running and while there is a free slot (the timer pauses
    /// while the max number of bombs is in play). Once due, it stays due until the caller reports the outcome with
    /// <see cref="OnSpawned"/> or <see cref="OnSpawnFailed"/>.
    /// </summary>
    public sealed class BombSpawnSchedule
    {
        // Absorbs float error when the accumulated deltas add up to exactly the interval (s).
        private const float DueTolerance = 0.0001f;

        private float _timeUntilNext;
        private bool _isRunning;

        public bool IsRunning => _isRunning;
        public float TimeUntilNext => _timeUntilNext;

        /// <summary>Starts (or restarts) the schedule: the first spawn is due after <paramref name="initialDelay"/> seconds.</summary>
        public void Start(float initialDelay)
        {
            _isRunning = true;
            _timeUntilNext = Mathf.Max(0f, initialDelay);
        }

        public void Stop()
        {
            _isRunning = false;
            _timeUntilNext = 0f;
        }

        /// <summary>Advances the timer. Returns true when a spawn is due now.</summary>
        public bool Tick(float deltaTime, int activeCount, int maxActive)
        {
            if (!_isRunning || activeCount >= maxActive)
            {
                return false;
            }

            if (_timeUntilNext > DueTolerance)
            {
                _timeUntilNext -= deltaTime;
            }

            return _timeUntilNext <= DueTolerance;
        }

        /// <summary>A bomb was placed: the next one is due after <paramref name="nextInterval"/> seconds of free slot.</summary>
        public void OnSpawned(float nextInterval)
        {
            _timeUntilNext = Mathf.Max(0f, nextInterval);
        }

        /// <summary>No valid spot this time: retry after <paramref name="retryDelay"/> seconds.</summary>
        public void OnSpawnFailed(float retryDelay)
        {
            _timeUntilNext = Mathf.Max(0f, retryDelay);
        }

        /// <summary>Random interval in [<paramref name="range"/>.x, <paramref name="range"/>.y].</summary>
        public static float NextInterval(Vector2 range, IRandomSource rng)
        {
            return Mathf.Lerp(range.x, range.y, rng.Value01());
        }
    }
}
