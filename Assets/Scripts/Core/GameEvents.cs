using System;
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
        public static event Action<PlayerId> OnPauseRequested;

        public static void RaisePlayerJoined(PlayerId playerId) => OnPlayerJoined?.Invoke(playerId);

        public static void RaisePauseRequested(PlayerId playerId) => OnPauseRequested?.Invoke(playerId);

        // Enter Play Mode without domain reload keeps statics alive: clear stale subscribers.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            OnPlayerJoined = null;
            OnPauseRequested = null;
        }
    }
}
