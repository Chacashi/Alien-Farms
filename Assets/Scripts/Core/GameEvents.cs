using System;
// Core -> Animals / Data / Items references accepted while there is a single runtime asmdef; revisit if Core gets its own assembly.
using AlienFarms.Animals;
using AlienFarms.Data;
using AlienFarms.Items;
using UnityEngine;

namespace AlienFarms.Core
{
    /// <summary>
    /// Global static events for decoupled communication between systems.
    /// Every per-player payload carries the <see cref="PlayerId"/> involved. Exception: match phase, countdown and
    /// match time are global to the round (shared by both players), so they carry no PlayerId; neither do bomb
    /// spawn/despawn, which belong to no player (the bomb lies in the shared Animal Zone or leaves play).
    /// </summary>
    public static class GameEvents
    {
        public static event Action<PlayerId> OnPlayerJoined;
        public static event Action<PlayerId> OnPlayerLeft;

        /// <summary>A player held Pause long enough to request the pause menu.</summary>
        public static event Action<PlayerId> OnPauseRequested;

        /// <summary>
        /// Hold-to-pause progress of the given player changed (0..1). Raised only while the value changes
        /// (holding, or once with 0 on release).
        /// </summary>
        public static event Action<PlayerId, float> OnPauseHoldProgress;

        /// <summary>The game was paused; payload: the player who owns the pause menu.</summary>
        public static event Action<PlayerId> OnGamePaused;

        /// <summary>The game was resumed; payload: the player who owned the pause menu.</summary>
        public static event Action<PlayerId> OnGameResumed;

        /// <summary>A player pressed Cancel while navigating a menu.</summary>
        public static event Action<PlayerId> OnMenuCancelPressed;

        /// <summary>A player's PlayerInput lost one of its paired devices (e.g. gamepad unplugged).</summary>
        public static event Action<PlayerId> OnPlayerDeviceLost;

        /// <summary>A player's PlayerInput regained a previously lost device.</summary>
        public static event Action<PlayerId> OnPlayerDeviceRegained;

        /// <summary>A joined player was assigned a character. Raised right before <see cref="OnPlayerJoined"/>.</summary>
        public static event Action<PlayerId, CharacterDefinition> OnPlayerCharacterAssigned;

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

        /// <summary>The given player's score changed to the new total (sum of the points of the animals in its zone).</summary>
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

        /// <summary>Global: a bomb appeared (inert) in the Animal Zone.</summary>
        public static event Action<Bomb> OnBombSpawned;

        /// <summary>A player picked up a bomb.</summary>
        public static event Action<PlayerId, Bomb> OnBombGrabbed;

        /// <summary>A player threw a bomb: it is armed (fuse lit) until it detonates or leaves play.</summary>
        public static event Action<PlayerId, Bomb> OnBombArmed;

        /// <summary>
        /// Global: a bomb left play (detonated, fell off the map or the round ended). Raised after
        /// <see cref="OnBombDetonated"/> when it exploded.
        /// </summary>
        public static event Action<Bomb> OnBombDespawned;

        /// <summary>
        /// A thrown bomb exploded. Payload: thrower (the player who threw it), blast center and blast radius (m).
        /// </summary>
        public static event Action<PlayerId, Vector3, float> OnBombDetonated;

        /// <summary>
        /// A player was stunned by a blast. Payload: victim, source (the thrower) and stun duration (s).
        /// Tackle stuns do not raise it.
        /// </summary>
        public static event Action<PlayerId, PlayerId, float> OnPlayerStunned;

        /// <summary>
        /// The given player's pen now holds <c>count</c> animals of the type at <c>animalTypeIndex</c> (index in the
        /// spawn settings' entries).
        /// </summary>
        public static event Action<PlayerId, int, int> OnPenInventoryChanged;

        /// <summary>
        /// Online: the given player's connection changed. Payload: player, connected, and the reconnection grace left (s)
        /// when it disconnected (0 when connected).
        /// </summary>
        public static event Action<PlayerId, bool, float> OnPlayerConnectionChanged;

        /// <summary>A tackle claim was resolved. Payload: attacker, victim and whether the tackle was accepted.</summary>
        public static event Action<PlayerId, PlayerId, bool> OnTackleResolved;

        public static void RaisePlayerJoined(PlayerId playerId) => OnPlayerJoined?.Invoke(playerId);

        public static void RaisePlayerLeft(PlayerId playerId) => OnPlayerLeft?.Invoke(playerId);

        public static void RaisePauseRequested(PlayerId playerId) => OnPauseRequested?.Invoke(playerId);

        public static void RaisePauseHoldProgress(PlayerId playerId, float normalized) =>
            OnPauseHoldProgress?.Invoke(playerId, normalized);

        public static void RaiseGamePaused(PlayerId owner) => OnGamePaused?.Invoke(owner);

        public static void RaiseGameResumed(PlayerId owner) => OnGameResumed?.Invoke(owner);

        public static void RaiseMenuCancelPressed(PlayerId playerId) => OnMenuCancelPressed?.Invoke(playerId);

        public static void RaisePlayerDeviceLost(PlayerId playerId) => OnPlayerDeviceLost?.Invoke(playerId);

        public static void RaisePlayerDeviceRegained(PlayerId playerId) => OnPlayerDeviceRegained?.Invoke(playerId);

        public static void RaisePlayerCharacterAssigned(PlayerId playerId, CharacterDefinition character) =>
            OnPlayerCharacterAssigned?.Invoke(playerId, character);

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

        public static void RaiseBombSpawned(Bomb bomb) => OnBombSpawned?.Invoke(bomb);

        public static void RaiseBombGrabbed(PlayerId playerId, Bomb bomb) => OnBombGrabbed?.Invoke(playerId, bomb);

        public static void RaiseBombArmed(PlayerId thrower, Bomb bomb) => OnBombArmed?.Invoke(thrower, bomb);

        public static void RaiseBombDespawned(Bomb bomb) => OnBombDespawned?.Invoke(bomb);

        public static void RaiseBombDetonated(PlayerId thrower, Vector3 position, float radius) =>
            OnBombDetonated?.Invoke(thrower, position, radius);

        public static void RaisePlayerStunned(PlayerId victim, PlayerId source, float duration) =>
            OnPlayerStunned?.Invoke(victim, source, duration);

        public static void RaisePenInventoryChanged(PlayerId playerId, int animalTypeIndex, int count)
        {
            PenInventorySnapshot.Set(playerId, animalTypeIndex, count);
            OnPenInventoryChanged?.Invoke(playerId, animalTypeIndex, count);
        }

        /// <summary>
        /// Raises 0 for every cached non-zero pen count, then clears the snapshot: listeners and late views both read
        /// empty pens. Used when a new match or online session starts.
        /// </summary>
        public static void ClearPenInventory()
        {
            for (int p = (int)PlayerId.Player1; p <= (int)PlayerId.Player2; p++)
            {
                var playerId = (PlayerId)p;
                int capacity = PenInventorySnapshot.TypeCapacity(playerId);
                for (int t = 0; t < capacity; t++)
                {
                    if (PenInventorySnapshot.Get(playerId, t) != 0)
                    {
                        RaisePenInventoryChanged(playerId, t, 0);
                    }
                }
            }

            PenInventorySnapshot.Reset();
        }

        public static void RaisePlayerConnectionChanged(PlayerId playerId, bool connected, float graceSeconds) =>
            OnPlayerConnectionChanged?.Invoke(playerId, connected, graceSeconds);

        public static void RaiseTackleResolved(PlayerId attacker, PlayerId victim, bool accepted) =>
            OnTackleResolved?.Invoke(attacker, victim, accepted);

        // Enter Play Mode without domain reload keeps statics alive: clear stale subscribers.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            OnPlayerJoined = null;
            OnPlayerLeft = null;
            OnPauseRequested = null;
            OnPauseHoldProgress = null;
            OnGamePaused = null;
            OnGameResumed = null;
            OnMenuCancelPressed = null;
            OnPlayerDeviceLost = null;
            OnPlayerDeviceRegained = null;
            OnPlayerCharacterAssigned = null;
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
            OnBombSpawned = null;
            OnBombGrabbed = null;
            OnBombArmed = null;
            OnBombDespawned = null;
            OnBombDetonated = null;
            OnPlayerStunned = null;
            OnPenInventoryChanged = null;
            OnPlayerConnectionChanged = null;
            OnTackleResolved = null;
            PenInventorySnapshot.Reset();
        }
    }
}
