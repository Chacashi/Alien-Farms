namespace AlienFarms.Items
{
    /// <summary>
    /// Lets a network layer follow the authority's bomb lifecycle. Without a hook, bombs go straight back to their pool
    /// on despawn (local mode).
    /// </summary>
    public interface IBombNetworkHook
    {
        /// <summary>The authority just spawned <paramref name="bomb"/> (active, Idle).</summary>
        void OnSpawned(Bomb bomb);

        /// <summary>
        /// The authority is despawning <paramref name="bomb"/>; its gameplay state is already cleared (Inactive) and
        /// <c>GameEvents.OnBombDespawned</c> raised. Return true to take over returning it to its pool
        /// (<see cref="BombSpawner.ReleaseToPool"/>), false to let the bomb release itself. When taking over, release it
        /// in the same frame (or as soon as possible): until then it still counts in the spawner's active bombs and
        /// against MaxActive.
        /// </summary>
        bool TryDespawn(Bomb bomb);
    }
}
