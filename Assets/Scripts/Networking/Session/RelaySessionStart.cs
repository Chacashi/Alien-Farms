using Unity.Services.Multiplayer;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Outcome of one create / join through <see cref="RelaySessionConnection"/>: the session and the network handler of
    /// that start only. Late cleanup (a start that finished after a Leave or a root teardown) acts on this instance, never
    /// on whatever session is current by then.
    /// </summary>
    internal sealed class RelaySessionStart
    {
        private RelaySessionStart(ISession session, NgoSessionNetworkHandler handler, string errorKey)
        {
            Session = session;
            Handler = handler;
            ErrorKey = errorKey;
        }

        /// <summary>Session created or joined (null when the start failed or was cancelled).</summary>
        public ISession Session { get; }

        /// <summary>Handler that started NGO for this session (null when the start failed before creating one).</summary>
        public NgoSessionNetworkHandler Handler { get; }

        /// <summary>Error key (<see cref="AlienFarms.Core.OnlineSessionErrorKeys"/>) of a failure; null on success or cancellation.</summary>
        public string ErrorKey { get; }

        public bool IsSuccess => Session != null;

        public static RelaySessionStart Succeeded(ISession session, NgoSessionNetworkHandler handler)
        {
            return new RelaySessionStart(session, handler, null);
        }

        public static RelaySessionStart Failed(string errorKey)
        {
            return new RelaySessionStart(null, null, errorKey);
        }
    }
}
