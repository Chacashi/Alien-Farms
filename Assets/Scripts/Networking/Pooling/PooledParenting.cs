using Unity.Netcode;

namespace AlienFarms.Networking.Pooling
{
    /// <summary>
    /// Lets a pool put a despawned <see cref="NetworkObject"/> back under its (non-network) container. NGO reverts and
    /// logs an error for any parent change of a NetworkObject that is not spawned, so parent sync is switched off for
    /// the duration of the release only. Call <see cref="BeginRelease"/> / <see cref="EndRelease"/> around the pool's
    /// Release, always after <c>Despawn</c>.
    /// </summary>
    public static class PooledParenting
    {
        public static void BeginRelease(NetworkObject networkObject, out bool previousSync)
        {
            previousSync = networkObject != null && networkObject.AutoObjectParentSync;
            if (networkObject != null && !networkObject.IsSpawned)
            {
                networkObject.AutoObjectParentSync = false;
            }
        }

        public static void EndRelease(NetworkObject networkObject, bool previousSync)
        {
            if (networkObject != null)
            {
                networkObject.AutoObjectParentSync = previousSync;
            }
        }
    }
}
