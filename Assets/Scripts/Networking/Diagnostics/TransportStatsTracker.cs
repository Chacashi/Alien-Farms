namespace AlienFarms.Networking.Diagnostics
{
    /// <summary>
    /// Pure per-session state of the transport lines of <see cref="PoolStatsView"/>: detects a new session (the driver
    /// started), tracks the longest frame of the session and turns the driver's cumulative received-packet count into a
    /// rate. Ticked every frame (visible or not); the rate is re-based whenever the view asks (e.g. shown again), so a
    /// hidden period never shows up as one averaged sample. A pause (editor pause or step, application suspended) is
    /// not a frame: <see cref="NotifyPaused"/> drops the next frame from the longest-frame record and re-bases the rate.
    /// </summary>
    public sealed class TransportStatsTracker
    {
        private ulong _lastRxPackets;
        private float _lastRxTime;
        private bool _needsBase = true;
        private bool _skipFrame;

        /// <summary>True while the transport driver is running.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>Longest frame since the current session started (s).</summary>
        public float LongestFrame { get; private set; }

        /// <summary>Received packets per second between the last two samples (0 right after a re-base).</summary>
        public int RxPerSecond { get; private set; }

        /// <summary>Every frame. Returns true on the frame a new session is detected (its counters start over).</summary>
        public bool Tick(bool running, float frameSeconds)
        {
            bool started = running && !IsRunning;
            IsRunning = running;
            if (started)
            {
                LongestFrame = 0f;
                RequestRebase();
            }

            if (_skipFrame)
            {
                _skipFrame = false;
                return started;
            }

            if (running && frameSeconds > LongestFrame)
            {
                LongestFrame = frameSeconds;
            }

            return started;
        }

        /// <summary>
        /// The next ticked frame spans a pause: it is not recorded as a frame (its delta is wall time, not a hitch) and
        /// the packet rate is re-based so the pause is not averaged into it.
        /// </summary>
        public void NotifyPaused()
        {
            _skipFrame = true;
            RequestRebase();
        }

        /// <summary>The next sample only sets the baseline (rate 0).</summary>
        public void RequestRebase()
        {
            _needsBase = true;
            RxPerSecond = 0;
        }

        /// <summary>Feeds the driver's cumulative received-packet count; returns the current rate.</summary>
        public int SampleRx(ulong totalRxPackets, float now)
        {
            if (_needsBase || totalRxPackets < _lastRxPackets)
            {
                _needsBase = false;
                _lastRxPackets = totalRxPackets;
                _lastRxTime = now;
                RxPerSecond = 0;
                return RxPerSecond;
            }

            float elapsed = now - _lastRxTime;
            if (elapsed > 0f)
            {
                ulong received = totalRxPackets - _lastRxPackets;
                RxPerSecond = (int)(received / elapsed + 0.5f);
                _lastRxPackets = totalRxPackets;
                _lastRxTime = now;
            }

            return RxPerSecond;
        }
    }
}
