using System;
// Core -> Animals reference accepted while there is a single runtime asmdef; revisit if Core gets its own assembly.
using AlienFarms.Animals;
using UnityEngine;

namespace AlienFarms.Core
{
    /// <summary>
    /// Global static events for decoupled communication between systems.
    /// Every payload carries the <see cref="PlayerId"/> involved.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<PlayerId> OnPlayerJoined;
        public static event Action<PlayerId> OnPlayerLeft;
        public static event Action<PlayerId> OnPauseRequested;

        /// <summary>A player picked up an animal.</summary>
        public static event Action<PlayerId, Animal> OnAnimalGrabbed;

        /// <summary>An animal left the hands of the given player (the previous holder).</summary>
        public static event Action<PlayerId, Animal, DropReason> OnAnimalDropped;

        public static void RaisePlayerJoined(PlayerId playerId) => OnPlayerJoined?.Invoke(playerId);

        public static void RaisePlayerLeft(PlayerId playerId) => OnPlayerLeft?.Invoke(playerId);

        public static void RaisePauseRequested(PlayerId playerId) => OnPauseRequested?.Invoke(playerId);

        public static void RaiseAnimalGrabbed(PlayerId playerId, Animal animal) => OnAnimalGrabbed?.Invoke(playerId, animal);

        public static void RaiseAnimalDropped(PlayerId playerId, Animal animal, DropReason reason) =>
            OnAnimalDropped?.Invoke(playerId, animal, reason);

        // Enter Play Mode without domain reload keeps statics alive: clear stale subscribers.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            OnPlayerJoined = null;
            OnPlayerLeft = null;
            OnPauseRequested = null;
            OnAnimalGrabbed = null;
            OnAnimalDropped = null;
        }
    }
}
