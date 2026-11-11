namespace AlienFarms.Animals
{
    /// <summary>
    /// Lets a network layer follow the authority's animal lifecycle. Without a hook, animals go straight back to their
    /// pool on despawn (local mode).
    /// </summary>
    public interface IAnimalNetworkHook
    {
        /// <summary>The authority just spawned <paramref name="animal"/> (active, Wandering).</summary>
        void OnSpawned(Animal animal);

        /// <summary>
        /// The authority is despawning <paramref name="animal"/>; its gameplay state is already cleared (Inactive).
        /// Return true to take over returning it to its pool (<see cref="AnimalTypePool.Release"/>), false to let the
        /// animal release itself. When taking over, release it in the same frame (or as soon as possible): until then
        /// the instance still counts as active in its pool and in the spawner's population.
        /// </summary>
        bool TryDespawn(Animal animal);
    }
}
