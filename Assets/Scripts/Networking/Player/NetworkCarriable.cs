using System;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Networking.Data;
using AlienFarms.Player;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace AlienFarms.Networking.Player
{
    /// <summary>
    /// Shared network side of something a player carries (animals, bombs).
    /// <list type="bullet">
    /// <item>Server: while carried the object is owned by the holder's client and parented under its Player_Net
    /// (network parenting); on release it is unparented, teleported to its release pose and (unless the derived class
    /// keeps it) given back to the server. The transform stays server-authoritative (NetworkTransform in Server mode),
    /// so physics never moves to the owner.</item>
    /// <item>Clients: the holder's grabber mirrors the object (<c>SetReplicatedHeld</c>) so aim and throw preview work on
    /// the owner. The owner asks the server to throw with <see cref="TryRequestThrow"/>.</item>
    /// </list>
    /// The pool handlers / spawn registry inject the runtime anchor with <see cref="Bind"/> (no prefab wiring).
    /// Presentation (ownership tint, debug label) listens to <see cref="ViewChanged"/>: raised on every instance after
    /// spawn, on each ownership change, when the replicated <see cref="Claimant"/> may have changed and on despawn.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public abstract class NetworkCarriable : NetworkBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Online settings: ownership log switch and debug label default.")]
        private NetworkSettings _settings;

        [Header("Network carry")]
        [SerializeField, Tooltip("NetworkTransform on this root (Server authority); teleported on release.")]
        private NetworkTransform _networkTransform;

        private NetworkRuntimeAnchor _anchor;
        private PlayerId _serverHolder = PlayerId.None;
        private bool _serverParented;
        private PlayerId _mirroredHolder = PlayerId.None;
        private PlayerId _pendingMirror = PlayerId.None;
        private bool _inPlay;

        // Seat of the current owner's avatar, cached for the next change's log: when a client disconnects NGO hands its
        // avatar (DontDestroyWithOwner) to the server first, so the previous owner can no longer be looked up.
        private PlayerId _ownerSeat = PlayerId.None;

        /// <summary>Raised on every instance when the owner changes. Payload: this object, previous and new owner client id.</summary>
        public event Action<NetworkCarriable, ulong, ulong> OwnershipChanged;

        /// <summary>Raised on every instance when what the ownership views show may have changed (see class summary).</summary>
        public event Action<NetworkCarriable> ViewChanged;

        /// <summary>The gameplay object this adapter replicates.</summary>
        public abstract IGrabbable Grabbable { get; }

        /// <summary>
        /// Seat that claims this object according to the replicated state, identical on every instance: the holder while
        /// carried (an armed bomb: its thrower); None otherwise.
        /// </summary>
        public abstract PlayerId Claimant { get; }

        /// <summary>Runtime anchor injected with <see cref="Bind"/> (players by seat / client, local player).</summary>
        public NetworkRuntimeAnchor Anchor => _anchor;

        public NetworkSettings Settings => _settings;

        /// <summary>Between the end of OnNetworkSpawn and OnNetworkDespawn (IsSpawned is still true during despawn).</summary>
        public bool IsInPlay => _inPlay && IsSpawned;

        /// <summary>Owner as the views show it (server, a seat, or a client not resolved yet). No allocation.</summary>
        public CarriableOwner ResolveOwner()
        {
            if (!IsInPlay)
            {
                return CarriableOwner.Server;
            }

            ulong owner = OwnerClientId;
            PlayerId seat = SeatOfClient(owner);
            return CarriableOwnerRules.Resolve(owner, NetworkManager.ServerClientId, Claimant, seat);
        }

        /// <summary>Injects the runtime anchor (players by seat). Called before Spawn on the server and on instantiation on clients.</summary>
        public void Bind(NetworkRuntimeAnchor anchor)
        {
            _anchor = anchor;
        }

        /// <summary>
        /// Owner on a pure client: asks the server to throw the carried object with this view (deg). False when this
        /// machine cannot ask (not the owner yet, server, not spawned).
        /// </summary>
        public bool TryRequestThrow(float yaw, float pitch)
        {
            if (!IsSpawned || IsServer || !IsOwner)
            {
                return false;
            }

            RequestThrowRpc(yaw, pitch);
            return true;
        }

        protected virtual void Awake()
        {
            if (_networkTransform == null)
            {
                TryGetComponent(out _networkTransform);
            }
        }

        protected override void OnNetworkPostSpawn()
        {
            base.OnNetworkPostSpawn();
            _inPlay = true;
            _ownerSeat = SeatOfClient(OwnerClientId);
            NotifyViewChanged();
        }

        public override void OnNetworkDespawn()
        {
            ClearMirror();
            _pendingMirror = PlayerId.None;
            _serverHolder = PlayerId.None;
            _serverParented = false;
            _inPlay = false;
            _ownerSeat = PlayerId.None;
            NotifyViewChanged();
        }

        protected override void OnOwnershipChanged(ulong previous, ulong current)
        {
            base.OnOwnershipChanged(previous, current);
            PlayerId previousSeat = CarriableOwnerRules.PreviousOwnerSeat(SeatOfClient(previous), _ownerSeat, Claimant);
            _ownerSeat = SeatOfClient(current);
            if (_settings != null && _settings.LogOwnership)
            {
                ulong server = NetworkManager.ServerClientId;
                Debug.Log($"[{GetType().Name}] '{name}' owner: {CarriableOwnerRules.DescribeClient(previous, server, previousSeat)} -> {CarriableOwnerRules.DescribeClient(current, server, _ownerSeat)} (here: IsOwner {IsOwner}, HasAuthority {HasAuthority}).", this);
            }

            // NGO can change ownership on its own (the thrower of an armed bomb disconnects) and the NetworkRigidbody
            // resets the body's kinematic state on every change: put the simulation's setup back (idempotent).
            if (IsServer)
            {
                RestoreAuthorityPhysics();
            }

            OwnershipChanged?.Invoke(this, previous, current);
            NotifyViewChanged();
        }

        public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
        {
            base.OnNetworkObjectParentChanged(parentNetworkObject);

            // NGO moves children to the root when their parent despawns (e.g. the holder's avatar): never try to
            // unparent again later.
            if (IsServer && parentNetworkObject == null)
            {
                _serverParented = false;
            }
        }

        /// <summary>Derived classes call it after the replicated state that feeds <see cref="Claimant"/> was written or received.</summary>
        protected void NotifyViewChanged()
        {
            ViewChanged?.Invoke(this);
        }

        private PlayerId SeatOfClient(ulong clientId) =>
            _anchor != null && _anchor.TryGetPlayerByClient(clientId, out NetworkPlayer player) ? player.PlayerId : PlayerId.None;

        /// <summary>
        /// The NetworkRigidbody resets the body's kinematic state when ownership changes: the derived class puts the
        /// simulation's own physics setup back (e.g. kinematic while carried).
        /// </summary>
        protected abstract void RestoreAuthorityPhysics();

        /// <summary>
        /// Server: follows the simulation's holder. Grabbed: owner = holder's client, parented under its avatar. Released:
        /// unparented and teleported (no slide on clients); ownership back to the server unless
        /// <paramref name="keepOwnership"/>.
        /// </summary>
        protected void ServerSyncCarry(PlayerId holder, bool keepOwnership)
        {
            if (!IsSpawned || !IsServer || holder == _serverHolder)
            {
                return;
            }

            if (holder != PlayerId.None)
            {
                if (_anchor != null && _anchor.TryGetPlayer(holder, out NetworkPlayer player) && player.IsSpawned)
                {
                    if (NetworkObject.OwnerClientId != player.OwnerClientId)
                    {
                        NetworkObject.ChangeOwnership(player.OwnerClientId);
                    }

                    _serverParented = NetworkObject.TrySetParent(player.NetworkObject, true);
                }
                else
                {
                    Debug.LogWarning($"[{GetType().Name}] No spawned avatar for {holder}; '{name}' is carried without ownership or parenting.", this);
                }
            }
            else
            {
                if (_serverParented)
                {
                    NetworkObject.TryRemoveParent(true);
                    _serverParented = false;
                }

                if (!keepOwnership)
                {
                    ServerReturnOwnership();
                }

                // The simulation already placed it at its release pose: snap clients there instead of interpolating.
                if (_networkTransform != null && _networkTransform.CanCommitToTransform)
                {
                    Transform root = transform;
                    _networkTransform.Teleport(root.position, root.rotation, root.localScale);
                }
            }

            _serverHolder = holder;
            RestoreAuthorityPhysics();
        }

        /// <summary>
        /// Server, before <c>Despawn(false)</c> on any path out of play: unparents the object from the holder's avatar and
        /// gives ownership back to the server while it is still spawned (NGO does neither for a despawned object, which
        /// would go back to its pool parented under the Player_Net). Idempotent.
        /// </summary>
        public void ServerDetach()
        {
            if (!IsSpawned || !IsServer)
            {
                return;
            }

            if (_serverParented)
            {
                NetworkObject.TryRemoveParent(true);
                _serverParented = false;
            }

            ServerReturnOwnership();
            _serverHolder = PlayerId.None;
        }

        /// <summary>Server: gives ownership back to the server (no-op if it already owns it).</summary>
        protected void ServerReturnOwnership()
        {
            if (IsSpawned && IsServer && !NetworkObject.IsOwnedByServer)
            {
                NetworkObject.RemoveOwnership();
                RestoreAuthorityPhysics();
            }
        }

        /// <summary>Clients: the holder's grabber mirrors this object; the previous holder's forgets it.</summary>
        protected void ReplicaSyncHolder(PlayerId holder)
        {
            if (IsServer || holder == _mirroredHolder)
            {
                return;
            }

            ClearMirror();
            _pendingMirror = PlayerId.None;
            if (holder != PlayerId.None && !TryMirror(holder))
            {
                // The holder's avatar is not spawned here yet (late join): retried from Update.
                _pendingMirror = holder;
            }
        }

        protected virtual void Update()
        {
            if (_pendingMirror == PlayerId.None)
            {
                return;
            }

            if (TryMirror(_pendingMirror))
            {
                _pendingMirror = PlayerId.None;
            }
        }

        private bool TryMirror(PlayerId holder)
        {
            if (_anchor == null || !_anchor.TryGetPlayer(holder, out NetworkPlayer player) || player.Rig == null || player.Rig.Grabber == null)
            {
                return false;
            }

            player.Rig.Grabber.SetReplicatedHeld(Grabbable);
            _mirroredHolder = holder;
            return true;
        }

        private void ClearMirror()
        {
            if (_mirroredHolder == PlayerId.None)
            {
                return;
            }

            if (_anchor != null && _anchor.TryGetPlayer(_mirroredHolder, out NetworkPlayer player) && player.Rig != null)
            {
                PlayerGrabber grabber = player.Rig.Grabber;
                if (grabber != null && ReferenceEquals(grabber.Held, Grabbable))
                {
                    grabber.SetReplicatedHeld(null);
                }
            }

            _mirroredHolder = PlayerId.None;
        }

        // Everyone, validated by hand: ownership may already be back on the server when a late request arrives (dropped
        // by timeout, despawned...), and NGO would log an error for an Owner-only RPC. A stale request is expected.
        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestThrowRpc(float yaw, float pitch, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            IGrabbable grabbable = Grabbable;
            PlayerId holder = grabbable != null ? grabbable.Holder : PlayerId.None;
            if (holder == PlayerId.None || _anchor == null || !_anchor.TryGetPlayer(holder, out NetworkPlayer player) || player.Carry == null)
            {
                Debug.Log($"[{GetType().Name}] Throw request for '{name}' from client {sender} rejected: not carried.", this);
                return;
            }

            if (sender != player.OwnerClientId || sender != OwnerClientId)
            {
                Debug.Log($"[{GetType().Name}] Throw request for '{name}' from client {sender} rejected: the holder is client {player.OwnerClientId} and the object is owned by client {OwnerClientId}.", this);
                return;
            }

            player.Carry.ServerHandleThrowRequest(grabbable, yaw, pitch, sender);
        }

        protected virtual void OnValidate()
        {
            if (_settings == null)
            {
                Debug.LogWarning($"[{GetType().Name}] Missing NetworkSettings on '{name}'; ownership changes are not logged.", this);
            }

            if (_networkTransform == null)
            {
                TryGetComponent(out _networkTransform);
            }

            if (_networkTransform == null)
            {
                Debug.LogWarning($"[{GetType().Name}] Missing NetworkTransform on '{name}'; releases cannot teleport.", this);
            }
            else if (_networkTransform.AuthorityMode != NetworkTransform.AuthorityModes.Server)
            {
                Debug.LogWarning($"[{GetType().Name}] NetworkTransform on '{name}' must use Server authority (physics stays on the server while a client owns it).", this);
            }
        }
    }
}
