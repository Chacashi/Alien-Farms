using UnityEngine;

namespace AlienFarms.Networking.Voice
{
    public sealed class VoicePositionThrottle
    {
        public const double KeepAliveInterval = 2d;
        private const float PositionEpsilon = 0.05f;
        private const float AngleEpsilon = 2f;
        private static readonly float MinForwardDot = Mathf.Cos(AngleEpsilon * Mathf.Deg2Rad);

        private readonly double _interval;
        private double _nextSendTime = double.NegativeInfinity;

        public VoicePositionThrottle(int hz)
        {
            _interval = 1d / Mathf.Clamp(hz, 1, 4);
        }

        public bool ShouldSend(double nowUnscaled, double latestSampleTime = double.PositiveInfinity)
        {
            if (nowUnscaled >= latestSampleTime && nowUnscaled < _nextSendTime)
            {
                _nextSendTime = nowUnscaled + _interval;
                return true;
            }

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

        // Forward vectors are normalized by the reporter.
        public static bool ShouldSendPose(Vector3 position, Vector3 forward, Vector3 lastPosition,
            Vector3 lastForward, double elapsed)
        {
            return elapsed >= KeepAliveInterval ||
                (position - lastPosition).sqrMagnitude > PositionEpsilon * PositionEpsilon ||
                Vector3.Dot(forward, lastForward) < MinForwardDot;
        }

        public void Reset()
        {
            _nextSendTime = double.NegativeInfinity;
        }
    }
}
