using System;
// Core -> Animals reference accepted while there is a single runtime asmdef; revisit if Core gets its own assembly.
using AlienFarms.Animals;
using UnityEngine;

namespace AlienFarms.Core
{
    /// <summary>
    /// Global static events for decoupled communication between systems.
    /// Every per-player payload carries the <see cref="PlayerId"/> involved. Exception: match phase, countdown and
    /// match time are global to the round (shared by both players), so they carry no PlayerId.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<PlayerId> OnPlayerJoined;
        public static event Action<PlayerId> OnPlayerLeft;
        public static event Action<PlayerId> OnPauseRequested;

        /// <summary>A player pressed Confirm (e.g. restart from the results screen).</summary>
        public static event Action<PlayerId> OnConfirmPressed;

        /// <summary>A player picked up an animal.</summary>
        public static event Action<PlayerId, Animal> OnAnimalGrabbed;

        /// <summary>
        /// An animal left the hands of the given player (the previous holder). The animal is Airborne at this point,
        /// including <see cref="DropReason.Thrown"/> launches; where it lands decides capture.
        /// </summary>
        public static event Action<PlayerId, Animal, DropReason> OnAnimalDropped;

        /// <summary>
        /// An animal settled inside the given player's zone (the zone owner), whoever released it. It counts for that
        /// player until <see cref="OnAnimalLeftZone"/> is raised for it.
        /// </summary>
        public static event Action<PlayerId, Animal> OnAnimalCaptured;

        /// <summary>A captured animal left the given player's zone (grabbed by anyone or despawned).</summary>
        public static event Action<PlayerId, Animal> OnAnimalLeftZone;

        /// <summary>The given player's score changed to the new total.</summary>
        public static event Action<PlayerId, int> OnScoreChanged;

        /// <summary>Global: the match moved from the first phase (previous) to the second (current).</summary>
        public static event Action<MatchPhase, MatchPhase> OnMatchPhaseChanged;

        /// <summary>Global: whole seconds left in the pre-round countdown.</summary>
        public static event Action<int> OnCountdownTick;

        /// <summary>Global: the countdown reached zero; play starts. Payload: how long to show the 'GO' message (s).</summary>
        public static event Action<float> OnCountdownFinished;

        /// <summary>Global: whole seconds left in the round. Raised only when the displayed second changes (1 Hz).</summary>
        public static event Action<int> OnMatchTimeChanged;

        /// <summary>Global: the round ended with this result (contains both players' scores).</summary>
        public static event Action<MatchResult> OnMatchEnded;

        public static void RaisePlayerJoined(PlayerId playerId) => OnPlayerJoined?.Invoke(playerId);

        public static void RaisePlayerLeft(PlayerId playerId) => OnPlayerLeft?.Invoke(playerId);

        public static void RaisePauseRequested(PlayerId playerId) => OnPauseRequested?.Invoke(playerId);

        public static void RaiseConfirmPressed(PlayerId playerId) => OnConfirmPressed?.Invoke(playerId);

        public static void RaiseAnimalGrabbed(PlayerId playerId, Animal animal) => OnAnimalGrabbed?.Invoke(playerId, animal);

        public static void RaiseAnimalDropped(PlayerId playerId, Animal animal, DropReason reason) =>
            OnAnimalDropped?.Invoke(playerId, animal, reason);

        public static void RaiseAnimalCaptured(PlayerId playerId, Animal animal) => OnAnimalCaptured?.Invoke(playerId, animal);

        public static void RaiseAnimalLeftZone(PlayerId playerId, Animal animal) => OnAnimalLeftZone?.Invoke(playerId, animal);

        public static void RaiseScoreChanged(PlayerId playerId, int score) => OnScoreChanged?.Invoke(playerId, score);

        public static void RaiseMatchPhaseChanged(MatchPhase previous, MatchPhase current) =>
            OnMatchPhaseChanged?.Invoke(previous, current);

        public static void RaiseCountdownTick(int secondsLeft) => OnCountdownTick?.Invoke(secondsLeft);

        public static void RaiseCountdownFinished(float goMessageDuration) => OnCountdownFinished?.Invoke(goMessageDuration);

        public static void RaiseMatchTimeChanged(int secondsLeft) => OnMatchTimeChanged?.Invoke(secondsLeft);

        public static void RaiseMatchEnded(MatchResult result) => OnMatchEnded?.Invoke(result);

        // Enter Play Mode without domain reload keeps statics alive: clear stale subscribers.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            OnPlayerJoined = null;
            OnPlayerLeft = null;
            OnPauseRequested = null;
            OnConfirmPressed = null;
            OnAnimalGrabbed = null;
            OnAnimalDropped = null;
            OnAnimalCaptured = null;
            OnAnimalLeftZone = null;
            OnScoreChanged = null;
            OnMatchPhaseChanged = null;
            OnCountdownTick = null;
            OnCountdownFinished = null;
            OnMatchTimeChanged = null;
            OnMatchEnded = null;
        }
    }
}
