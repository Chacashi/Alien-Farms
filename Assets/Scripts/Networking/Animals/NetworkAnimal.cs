using AlienFarms.Animals;
using AlienFarms.Core;
using AlienFarms.Networking.Player;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Animals
{
    /// <summary>
    /// Network adapter of a pooled <see cref="Animal"/>. Pose: NetworkTransform + NetworkRigidbody (server authority).
    /// Server: mirrors <see cref="Animal.StateChanged"/> (and the drop reason) into <see cref="AnimalNetState"/> and
    /// drives ownership / parenting while it is carried (<see cref="NetworkCarriable"/>).
    /// Clients: the animal is a Replica; the replicated state is applied with <see cref="Animal.ApplyReplicatedState"/>
    /// and the matching grab / drop / capture GameEvents are raised here (never on the server: no double events on the
    /// host). Inactive is never applied from the state: the despawn (pool handler) takes it out of play.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkAnimal : NetworkCarriable
    {
        [Header("References")]
        [SerializeField, Tooltip("Animal on this root.")]
        private Animal _animal;

        private readonly NetworkVariable<AnimalNetState> _state = new NetworkVariable<AnimalNetState>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private AnimalNetState _applied;
        private DropReason _lastDrop = DropReason.Released;
        private bool _serverListening;

        public Animal Animal => _animal;

        public AnimalNetState ReplicatedState => _state.Value;

        public override IGrabbable Grabbable => _animal;

        public override PlayerId Claimant =>
            _state.Value.State == AnimalState.Carried ? _state.Value.Holder : PlayerId.None;

        protected override void Awake()
        {
            base.Awake();
            if (_animal == null)
            {
                TryGetComponent(out _animal);
            }
        }

        /// <summary>Server, before Spawn of a pooled instance: clears the previous use's replicated state.</summary>
        public void ServerPrepareForSpawn()
        {
            _state.Reset(default);
            _lastDrop = DropReason.Released;
        }

        public override void OnNetworkSpawn()
        {
            if (_animal == null)
            {
                return;
            }

            if (IsServer)
            {
                _animal.StateChanged += HandleStateChanged;
                GameEvents.OnAnimalDropped += HandleAnimalDropped;
                _serverListening = true;
                _state.Value = Capture();
                return;
            }

            _applied = default;
            _state.OnValueChanged += HandleReplicatedStateChanged;
            ApplyReplicated(_state.Value);
        }

        public override void OnNetworkDespawn()
        {
            if (_serverListening)
            {
                _serverListening = false;
                if (_animal != null)
                {
                    _animal.StateChanged -= HandleStateChanged;

                    // Still in play: the session ended (shutdown) without Animal.Despawn, and the NetworkRigidbody has
                    // just made the body kinematic. Stop the simulation; the spawner restores Authority on reuse.
                    if (_animal.State != AnimalState.Inactive)
                    {
                        _animal.SetSimulationRole(SimulationRole.Replica);
                    }
                }

                GameEvents.OnAnimalDropped -= HandleAnimalDropped;
            }
            else if (!IsServer)
            {
                _state.OnValueChanged -= HandleReplicatedStateChanged;
                RaiseLeavingPlay();
                _applied = default;
            }

            base.OnNetworkDespawn();
        }

        protected override void RestoreAuthorityPhysics()
        {
            // Re-applying the role restores the simulation's kinematic state (carried = kinematic) after NGO touched it.
            if (_animal != null && _animal.Role == SimulationRole.Authority)
            {
                _animal.SetSimulationRole(SimulationRole.Authority);
            }
        }

        private AnimalNetState Capture() => new AnimalNetState(_animal.State, _animal.Holder, _animal.Owner, _lastDrop);

        private void HandleStateChanged(Animal animal)
        {
            if (animal.State != AnimalState.Inactive)
            {
                ServerSyncCarry(animal.State == AnimalState.Carried ? animal.Holder : PlayerId.None, false);
            }
            else
            {
                // Leaving play: a carried animal is released first, but never despawn still parented or client-owned.
                ServerDetach();
            }

            _state.Value = Capture();
            NotifyViewChanged();
        }

        /// <summary>Raised right after the transition out of Carried: only the reason is new.</summary>
        private void HandleAnimalDropped(PlayerId previousHolder, Animal animal, DropReason reason)
        {
            if (animal != _animal)
            {
                return;
            }

            _lastDrop = reason;
            _state.Value = Capture();
        }

        private void HandleReplicatedStateChanged(AnimalNetState previous, AnimalNetState current)
        {
            ApplyReplicated(current);
            NotifyViewChanged();
        }

        private void ApplyReplicated(AnimalNetState state)
        {
            if (IsServer || state.State == AnimalState.Inactive)
            {
                return;
            }

            AnimalNetState previous = _applied;
            _applied = state;
            _animal.ApplyReplicatedState(state.State, state.ZoneOwner, state.Holder);
            ReplicaSyncHolder(state.State == AnimalState.Carried ? state.Holder : PlayerId.None);
            RaiseTransitions(previous, state);
        }

        /// <summary>Same order as the server's simulation: left zone, dropped, grabbed, captured.</summary>
        private void RaiseTransitions(AnimalNetState previous, AnimalNetState current)
        {
            bool wasCaptured = previous.State == AnimalState.Captured;
            bool isCaptured = current.State == AnimalState.Captured;
            bool wasCarried = previous.State == AnimalState.Carried;
            bool isCarried = current.State == AnimalState.Carried;

            if (wasCaptured && (!isCaptured || previous.ZoneOwner != current.ZoneOwner))
            {
                GameEvents.RaiseAnimalLeftZone(previous.ZoneOwner, _animal);
            }

            if (wasCarried && (!isCarried || previous.Holder != current.Holder))
            {
                GameEvents.RaiseAnimalDropped(previous.Holder, _animal, current.LastDrop);
            }

            if (isCarried && (!wasCarried || previous.Holder != current.Holder))
            {
                GameEvents.RaiseAnimalGrabbed(current.Holder, _animal);
            }

            if (isCaptured && (!wasCaptured || previous.ZoneOwner != current.ZoneOwner))
            {
                GameEvents.RaiseAnimalCaptured(current.ZoneOwner, _animal);
            }
        }

        /// <summary>Despawned while carried or penned: balance the events the server raised before leaving play.</summary>
        private void RaiseLeavingPlay()
        {
            if (_animal == null)
            {
                return;
            }

            if (_applied.State == AnimalState.Carried)
            {
                GameEvents.RaiseAnimalDropped(_applied.Holder, _animal, DropReason.Forced);
            }
            else if (_applied.State == AnimalState.Captured)
            {
                GameEvents.RaiseAnimalLeftZone(_applied.ZoneOwner, _animal);
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (_animal == null)
            {
                TryGetComponent(out _animal);
            }

            if (_animal == null)
            {
                Debug.LogWarning($"[{nameof(NetworkAnimal)}] Missing Animal on '{name}'.", this);
            }
        }
    }
}
