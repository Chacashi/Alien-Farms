using System;
using AlienFarms.Core;
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
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public abstract class NetworkCarriable : NetworkBehaviour
    {
        [Header("Network carry")]
        [SerializeField, Tooltip("NetworkTransform on this root (Server authority); teleported on release.")]
        private NetworkTransform _networkTransform;

        [SerializeField, Tooltip("Logs every ownership change of this object (debug / course demo).")]
        private bool _logOwnership;

        private NetworkRuntimeAnchor _anchor;
        private PlayerId _serverHolder = PlayerId.None;
        private bool _serverParented;
        private PlayerId _mirroredHolder = PlayerId.None;
        private PlayerId _pendingMirror = PlayerId.None;

        /// <summary>Raised on every instance when the owner changes. Payload: this object, previous and new owner client id.</summary>
        public event Action<NetworkCarriable, ulong, ulong> OwnershipChanged;

        /// <summary>The gameplay object this adapter replicates.</summary>
        public abstract IGrabbable Grabbable { get; }

        protected NetworkRuntimeAnchor Anchor => _anchor;

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

        public override void OnNetworkDespawn()
        {
            ClearMirror();
            _pendingMirror = PlayerId.None;
            _serverHolder = PlayerId.None;
            _serverParented = false;
        }

        protected override void OnOwnershipChanged(ulong previous, ulong current)
        {
            base.OnOwnershipChanged(previous, current);
            if (_logOwnership)
            {
                Debug.Log($"[{GetType().Name}] '{name}' owner: client {previous} -> client {current} (IsOwner {IsOwner}, server authority).", this);
            }

            OwnershipChanged?.Invoke(this, previous, current);
        }

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
