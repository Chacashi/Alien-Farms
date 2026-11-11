using System.Threading.Tasks;
using Unity.Netcode;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// How a session is started: direct IP (<see cref="DirectConnection"/>) or, later, Sessions + Relay. Asynchronous so a
    /// relay allocation fits the same contract. The caller writes <see cref="NetworkConfig.ConnectionData"/> first.
    /// </summary>
    public interface IConnectionStrategy
    {
        /// <summary>Configures the transport and starts <paramref name="manager"/> as host. False when it could not start.</summary>
        Task<bool> StartHostAsync(NetworkManager manager);

        /// <summary>Configures the transport and starts <paramref name="manager"/> as client. False when it could not start.</summary>
        Task<bool> StartClientAsync(NetworkManager manager);
    }
}
