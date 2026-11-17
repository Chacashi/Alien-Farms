namespace AlienFarms.Networking.Session
{
    /// <summary>What <see cref="HostShutdownSequence.Step"/> asks the root to do on this frame.</summary>
    public enum HostShutdownStep
    {
        /// <summary>Nothing yet: keep the transport running.</summary>
        Wait = 0,

        /// <summary>Disconnect every remote client with <see cref="DisconnectReasons.HostDisconnected"/>.</summary>
        DisconnectClients = 1,

        /// <summary>Call <c>NetworkManager.Shutdown()</c>.</summary>
        Shutdown = 2,

        /// <summary>NGO stopped (or the stop timed out): the shutdown is over.</summary>
        Complete = 3
    }
}
