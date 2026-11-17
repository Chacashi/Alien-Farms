namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Graceful shutdown of a host that still has remote clients (pure logic, driven every frame by
    /// <see cref="NetworkRootLifetime"/>). The host already sent the leaving notice when it begins:
    /// <list type="number">
    /// <item>Announcing: the connections stay open for the notice time, so the reliable notice is delivered (and resent).</item>
    /// <item>Disconnecting: every remote client is disconnected with a reason; NGO sends it and closes them on its next
    /// late update, so the transport is not disposed in the same frame (a short flush wait at most).</item>
    /// <item>Stopping: <c>NetworkManager.Shutdown()</c>, then wait until it stops listening (bounded by a timeout).</item>
    /// </list>
    /// NGO stopping by itself at any point (shutdown elsewhere, transport failure) skips to Stopping. Times are real time.
    /// </summary>
    public sealed class HostShutdownSequence
    {
        /// <summary>Longest wait for NGO to drop the disconnected clients before shutting down anyway (s).</summary>
        public const float DisconnectFlushSeconds = 0.25f;

        /// <summary>Longest wait for NGO to stop listening after Shutdown() (s).</summary>
        public const float StopTimeoutSeconds = 1f;

        private enum Phase
        {
            Idle,
            Announcing,
            Disconnecting,
            Stopping
        }

        private Phase _phase;
        private double _deadline;

        /// <summary>True between <see cref="Begin"/> and the <see cref="HostShutdownStep.Complete"/> step (or <see cref="Reset"/>).</summary>
        public bool IsRunning => _phase != Phase.Idle;

        /// <summary>
        /// Whether a shutdown is worth deferring: only a listening host with remote clients that got the leaving notice
        /// and a positive notice time. Otherwise the root shuts down at once.
        /// </summary>
        public static bool ShouldDefer(bool isServer, int notifiedClients, float noticeSeconds)
        {
            return isServer && notifiedClients > 0 && noticeSeconds > 0f;
        }

        /// <summary>Starts the sequence at <paramref name="now"/>; the notice was just sent.</summary>
        public void Begin(double now, float noticeSeconds)
        {
            _phase = Phase.Announcing;
            _deadline = now + (noticeSeconds > 0f ? noticeSeconds : 0f);
        }

        /// <summary>Back to idle without completing (root destroyed, Play Mode exited).</summary>
        public void Reset()
        {
            _phase = Phase.Idle;
            _deadline = 0d;
        }

        /// <param name="now">Real time (s).</param>
        /// <param name="isListening">NetworkManager.IsListening.</param>
        /// <param name="shutdownInProgress">NetworkManager.ShutdownInProgress.</param>
        /// <param name="hasRemoteClients">Any connected client besides the host's own.</param>
        public HostShutdownStep Step(double now, bool isListening, bool shutdownInProgress, bool hasRemoteClients)
        {
            switch (_phase)
            {
                case Phase.Announcing:
                    if (!isListening || shutdownInProgress)
                    {
                        EnterStopping(now);
                        return HostShutdownStep.Wait;
                    }

                    // Every client already gone (dropped by itself): nothing left to wait for.
                    if (now < _deadline && hasRemoteClients)
                    {
                        return HostShutdownStep.Wait;
                    }

                    _phase = Phase.Disconnecting;
                    _deadline = now + DisconnectFlushSeconds;
                    return HostShutdownStep.DisconnectClients;

                case Phase.Disconnecting:
                    if (!isListening || shutdownInProgress)
                    {
                        EnterStopping(now);
                        return HostShutdownStep.Wait;
                    }

                    if (now < _deadline && hasRemoteClients)
                    {
                        return HostShutdownStep.Wait;
                    }

                    EnterStopping(now);
                    return HostShutdownStep.Shutdown;

                case Phase.Stopping:
                    if (isListening && now < _deadline)
                    {
                        return HostShutdownStep.Wait;
                    }

                    Reset();
                    return HostShutdownStep.Complete;

                default:
                    return HostShutdownStep.Wait;
            }
        }

        private void EnterStopping(double now)
        {
            _phase = Phase.Stopping;
            _deadline = now + StopTimeoutSeconds;
        }
    }
}
