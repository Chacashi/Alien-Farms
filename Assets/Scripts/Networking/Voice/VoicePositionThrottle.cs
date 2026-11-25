using UnityEngine;

namespace AlienFarms.Networking.Voice
{
    public sealed class VoicePositionThrottle
    {
        private readonly double _interval;
        private double _nextSendTime = double.NegativeInfinity;

        public VoicePositionThrottle(int hz)
        {
            _interval = 1d / Mathf.Clamp(hz, 1, 4);
        }

        public bool ShouldSend(double nowUnscaled)
        {
            if (nowUnscaled < _nextSendTime)
            {
                return false;
            }

            // Advance on a fixed grid so frame jitter doesn't drift the rate below the target Hz.
            _nextSendTime += _interval;
            if (_nextSendTime <= nowUnscaled)
            {
                _nextSendTime = nowUnscaled + _interval;
            }

            return true;
        }

        public void Reset()
        {
            _nextSendTime = double.NegativeInfinity;
        }
    }
}
