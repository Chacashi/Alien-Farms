using AlienFarms.Core;
using AlienFarms.Items;
using AlienFarms.Networking.Interest;
using AlienFarms.Networking.Player;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Items
{
    /// <summary>
    /// Network adapter of a pooled <see cref="Bomb"/>. Pose: NetworkTransform + NetworkRigidbody (server authority).
    /// Server: mirrors <see cref="Bomb.StateChanged"/> into <see cref="BombNetState"/>; the holder owns it from the grab
    /// until it detonates or leaves play (a thrown bomb keeps its thrower as owner; dropped inert, it goes back to the
    /// server). The detonation (event, not state) reaches clients through <see cref="DetonatedRpc"/>, and the stuns it
    /// causes through <see cref="PlayerStunnedRpc"/> (the stun itself is replicated in the player snapshot).
    /// Clients: the bomb is a Replica; the state is applied with <see cref="Bomb.ApplyReplicatedState"/> and the bomb
    /// instance-specific GameEvents are raised here.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkBomb : NetworkCarriable
    {
        [Header("References")]
        [SerializeField, Tooltip("Bomb on this root.")]
        private Bomb _bomb;

        private readonly NetworkVariable<BombNetState> _state = new NetworkVariable<BombNetState>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private BombNetState _applied;
        private bool _serverListening;

        // Server: true between the detonation and the despawn, the window in which its stuns are applied.
        private bool _detonating;

        public Bomb Bomb => _bomb;

        public BombNetState ReplicatedState => _state.Value;

        public override IGrabbable Grabbable => _bomb;

        public override InterestKind InterestKind => InterestKind.Bomb;
        public override bool IsInterestExempt => _bomb.State is BombState.Carried or BombState.Armed;

        /// <summary>Carried: the holder. Armed: the thrower (it keeps ownership until the detonation).</summary>
        public override PlayerId Claimant
        {
            get
            {
                BombNetState state = _state.Value;
                return state.State == BombState.Carried ? state.Holder
                    : state.State == BombState.Armed ? state.Thrower
                    : PlayerId.None;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (_bomb == null)
            {
                TryGetComponent(out _bomb);
            }
        }

        /// <summary>Server, before Spawn of a pooled instance: clears the previous use's replicated state.</summary>
        public void ServerPrepareForSpawn()
        {
            _state.Reset(default);
            _detonating = false;
        }

        public override void OnNetworkSpawn()
        {
            if (_bomb == null)
            {
                return;
            }

            if (IsServer)
            {
                _bomb.StateChanged += HandleStateChanged;
                _bomb.Detonated += HandleDetonated;
                GameEvents.OnPlayerStunned += HandlePlayerStunned;
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
                _detonating = false;
                if (_bomb != null)
                {
                    _bomb.StateChanged -= HandleStateChanged;
                    _bomb.Detonated -= HandleDetonated;

                    // Still in play: the session ended (shutdown) without Bomb.Despawn. Stop the simulation (an armed
                    // bomb would otherwise detonate offline); the spawner restores Authority on reuse.
                    if (_bomb.State != BombState.Inactive)
                    {
                        _bomb.SetSimulationRole(SimulationRole.Replica);
                    }
                }

                GameEvents.OnPlayerStunned -= HandlePlayerStunned;
            }
            else if (!IsServer)
            {
                _state.OnValueChanged -= HandleReplicatedStateChanged;
                // Armed bombs are exempt from interest filtering and may still own a looping fuse.
                if (_bomb != null && _applied.State == BombState.Armed)
                {
                    GameEvents.RaiseBombDespawned(_bomb);
                }

                _applied = default;
            }

            base.OnNetworkDespawn();
        }

        protected override void RestoreAuthorityPhysics()
        {
            // Re-applying the role restores the simulation's kinematic state (carried = kinematic) after NGO touched it.
            if (_bomb != null && _bomb.Role == SimulationRole.Authority)
            {
                _bomb.SetSimulationRole(SimulationRole.Authority);
            }
        }

        private BombNetState Capture() => new BombNetState(_bomb.State, _bomb.Holder, _bomb.Thrower);

        private void HandleStateChanged(Bomb bomb)
        {
            BombState state = bomb.State;
            if (state == BombState.Inactive)
            {
                // Leaving play (Despawn goes Carried -> Inactive without a release): never despawn still parented.
                _detonating = false;
                ServerDetach();
            }
            else
            {
                // Thrown (Armed): the thrower keeps ownership until it detonates; dropped inert: back to the server.
                ServerSyncCarry(state == BombState.Carried ? bomb.Holder : PlayerId.None, state == BombState.Armed);
            }

            _state.Value = Capture();
            NotifyViewChanged();
        }

        private void HandleDetonated(Bomb bomb, Vector3 center, float radius)
        {
            _detonating = true;
            DetonatedRpc(bomb.Thrower, center, radius);
        }

        private void HandlePlayerStunned(PlayerId victim, PlayerId source, float duration)
        {
            if (_detonating)
            {
                PlayerStunnedRpc(victim, source, duration);
            }
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Reliable, InvokePermission = RpcInvokePermission.Server)]
        private void DetonatedRpc(PlayerId thrower, Vector3 center, float radius)
        {
            if (!IsServer)
            {
                GameEvents.RaiseBombDetonated(thrower, center, radius);
            }
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Reliable, InvokePermission = RpcInvokePermission.Server)]
        private void PlayerStunnedRpc(PlayerId victim, PlayerId source, float duration)
        {
            if (!IsServer)
            {
                GameEvents.RaisePlayerStunned(victim, source, duration);
            }
        }

        private void HandleReplicatedStateChanged(BombNetState previous, BombNetState current)
        {
            ApplyReplicated(current);
            NotifyViewChanged();
        }

        private void ApplyReplicated(BombNetState state)
        {
            if (IsServer || state.State == BombState.Inactive)
            {
                return;
            }

            BombNetState previous = _applied;
            _applied = state;
            _bomb.ApplyReplicatedState(state.State, state.Holder, state.Thrower);
            ReplicaSyncHolder(state.State == BombState.Carried ? state.Holder : PlayerId.None);

            if (state.State == BombState.Carried && (previous.State != BombState.Carried || previous.Holder != state.Holder))
            {
                GameEvents.RaiseBombGrabbed(state.Holder, _bomb);
            }

            if (state.State == BombState.Armed && previous.State != BombState.Armed)
            {
                GameEvents.RaiseBombArmed(state.Thrower, _bomb);
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (_bomb == null)
            {
                TryGetComponent(out _bomb);
            }

            if (_bomb == null)
            {
                Debug.LogWarning($"[{nameof(NetworkBomb)}] Missing Bomb on '{name}'.", this);
            }
        }
    }
}
