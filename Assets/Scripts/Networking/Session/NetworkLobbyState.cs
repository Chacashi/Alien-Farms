using System;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Networking.Data;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Networked online room (prefab LobbyState, spawned in MainMenu by <see cref="LobbyStateSpawner"/> on the host).
    /// The server owns the seats and picks (<see cref="LobbyRules"/> on a 2-entry mirror) and starts the match; every
    /// peer republishes the replicated state into the <see cref="OnlineLobbyChannel"/> the menu reads, and forwards the
    /// menu's character requests to the server. Everything is event driven: no per-frame work.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkLobbyState : NetworkBehaviour
    {
        private const int MaxSeats = OnlineLobbyChannel.MaxSeats;

        [Header("Config")]
        [SerializeField, Tooltip("Runtime anchor of the NetworkRoot (seats, scene loader).")]
        private NetworkRuntimeAnchor _anchor;

        [SerializeField, Tooltip("Max players: seats required to start (capped at 2).")]
        private NetworkSettings _settings;

        [SerializeField, Tooltip("Characters that can be picked (its Count bounds the requested index).")]
        private CharacterRoster _roster;

        [SerializeField, Tooltip("Channel the online room menu reads and writes requests to.")]
        private OnlineLobbyChannel _lobbyChannel;

        [SerializeField, Tooltip("Optional: runtime session channel. Every session change republishes the room (safety net "
            + "against a stale Unavailable written while the session was still joining).")]
        private OnlineSessionChannel _session;

        // Replicated state (server writes).
        private readonly NetworkList<LobbySlotEntry> _entries = new NetworkList<LobbySlotEntry>();
        private readonly NetworkVariable<OnlineLobbyPhase> _phase = new NetworkVariable<OnlineLobbyPhase>(OnlineLobbyPhase.Open);

        // Server mirror the rules run on, plus reconcile buffers.
        private LobbySlotEntry[] _mirror = new LobbySlotEntry[MaxSeats];
        private LobbySlotEntry[] _rebuild = new LobbySlotEntry[MaxSeats];
        private readonly PlayerSlot[] _seatBuffer = new PlayerSlot[ServerSlotDirectory.Capacity];
        private int _mirrorCount;

        // Publish buffers (every peer); names are converted to string only when they change.
        private readonly LobbySlotEntry[] _publishEntries = new LobbySlotEntry[MaxSeats];
        private readonly OnlineLobbySeat[] _publishSeats = new OnlineLobbySeat[MaxSeats];
        private readonly FixedString32Bytes[] _nameSources = new FixedString32Bytes[MaxSeats];
        private readonly string[] _names = new string[MaxSeats];

        private NetworkManager _manager;
        private bool _serverSubscribed;
        private bool _peerSubscribed;

        private int RequiredSeats => Mathf.Min(_settings != null ? _settings.MaxPlayers : MaxSeats, MaxSeats);

        private int RosterCount => _roster != null ? _roster.Count : 0;

        public override void OnNetworkSpawn()
        {
            _manager = NetworkManager;
            for (int i = 0; i < MaxSeats; i++)
            {
                _names[i] = null;
                _nameSources[i] = default;
            }

            if (IsServer)
            {
                _manager.OnClientConnectedCallback += HandleClientConnected;
                _manager.OnClientDisconnectCallback += HandleClientDisconnected;
                if (_lobbyChannel != null)
                {
                    _lobbyChannel.StartRequested += ServerTryStart;
                }

                _serverSubscribed = true;
                _phase.Value = OnlineLobbyPhase.Open;
                ServerRebuild(0, false);
                Debug.Log($"[{nameof(NetworkLobbyState)}] Room open ({_mirrorCount} seat(s)).", this);
            }

            _entries.OnListChanged += HandleEntriesChanged;
            _phase.OnValueChanged += HandlePhaseChanged;
            if (_lobbyChannel != null)
            {
                _lobbyChannel.CharacterRequested += HandleCharacterRequested;
            }

            if (_session != null)
            {
                _session.Changed += Publish;
            }

            _peerSubscribed = true;

            // A refusal from a previous room must not show in this one.
            if (_lobbyChannel != null)
            {
                _lobbyChannel.ClearReject();
            }

            Publish();
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                Debug.Log($"[{nameof(NetworkLobbyState)}] Room despawned.", this);
            }

            Unsubscribe();
            if (_lobbyChannel == null)
            {
                return;
            }

            // R2: the Starting value may not arrive before the despawn. A despawn while the session keeps running only
            // happens when the match starts; one during shutdown means the room is gone.
            bool sessionGone = _manager == null || _manager.ShutdownInProgress || !_manager.IsListening;
            _lobbyChannel.SetPhase(sessionGone ? OnlineLobbyPhase.Unavailable : OnlineLobbyPhase.Starting);
        }

        public override void OnDestroy()
        {
            Unsubscribe();
            base.OnDestroy();
        }

        // ---------------------------------------------------------------- Requests

        private void HandleCharacterRequested(int characterIndex)
        {
            if (!IsSpawned)
            {
                return;
            }

            // Out-of-range values stay out of range (254) instead of turning into a release (255).
            byte wire = characterIndex < 0
                ? ConnectionPayload.NoCharacter
                : (byte)Math.Min(characterIndex, ConnectionPayload.NoCharacter - 1);
            RequestCharacterRpc(wire);
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestCharacterRpc(byte characterIndex, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            int requested = characterIndex == ConnectionPayload.NoCharacter ? LobbyRules.Release : characterIndex;
            LobbyRequestResult result = LobbyRules.ApplyRequest(_mirror, _mirrorCount, sender, requested, RosterCount,
                _phase.Value == OnlineLobbyPhase.Open);

            if (result == LobbyRequestResult.Accepted || result == LobbyRequestResult.Released)
            {
                PushMirror();
                return;
            }

            // Silent refusals (locked pick) have no key: nothing to report.
            if (LobbyRules.IsRejected(result) && LobbyRules.ToErrorKey(result) != null)
            {
                CharacterRejectedRpc((byte)result, RpcTarget.Single(sender, RpcTargetUse.Temp));
            }
        }

        [Rpc(SendTo.SpecifiedInParams, Delivery = RpcDelivery.Reliable, InvokePermission = RpcInvokePermission.Server)]
        private void CharacterRejectedRpc(byte result, RpcParams rpcParams)
        {
            if (_lobbyChannel != null)
            {
                _lobbyChannel.SetReject(LobbyRules.ToErrorKey((LobbyRequestResult)result));
            }
        }

        // ---------------------------------------------------------------- Server

        private void HandleClientConnected(ulong clientId)
        {
            ServerRebuild(0, false);
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            // The seat may still be held if the approval handler hears the disconnect after us: drop it here too.
            ServerRebuild(clientId, true);
        }

        private void ServerRebuild(ulong leavingClientId, bool hasLeavingClient)
        {
            // Disconnect callbacks also fire while NGO shuts down: never touch the NetworkList then.
            if (!IsServer || !IsSpawned || _manager == null || _manager.ShutdownInProgress)
            {
                return;
            }

            ServerSlotDirectory slots = _anchor != null ? _anchor.Slots : null;
            for (int i = 0; i < _seatBuffer.Length; i++)
            {
                PlayerSlot seat = default;
                if (slots != null && slots.TryGet((PlayerId)(i + 1), out PlayerSlot held)
                    && !(hasLeavingClient && held.ClientId == leavingClientId))
                {
                    seat = held;
                }

                _seatBuffer[i] = seat;
            }

            int count = LobbyRules.Reconcile(_mirror, _mirrorCount, _seatBuffer, _rebuild);
            (_mirror, _rebuild) = (_rebuild, _mirror);
            _mirrorCount = count;
            PushMirror();
        }

        /// <summary>Copies the mirror into the NetworkList, touching only what changed.</summary>
        private void PushMirror()
        {
            for (int i = 0; i < _mirrorCount; i++)
            {
                if (i < _entries.Count)
                {
                    _entries.Set(i, _mirror[i]);
                }
                else
                {
                    _entries.Add(_mirror[i]);
                }
            }

            for (int i = _entries.Count - 1; i >= _mirrorCount; i--)
            {
                _entries.RemoveAt(i);
            }
        }

        private void ServerTryStart()
        {
            if (!IsServer || !IsSpawned)
            {
                return;
            }

            if (_phase.Value != OnlineLobbyPhase.Open)
            {
                // Already starting: StartRequested only fires on the host, so the host is the requester.
                if (_lobbyChannel != null)
                {
                    _lobbyChannel.SetReject(OnlineSessionErrorKeys.Busy);
                }

                return;
            }

            ServerSlotDirectory slots = _anchor != null ? _anchor.Slots : null;
            if (slots == null || !LobbyRules.CanStart(_mirror, _mirrorCount, RequiredSeats))
            {
                _lobbyChannel.SetReject(OnlineSessionErrorKeys.CannotStart);
                return;
            }

            for (int i = 0; i < _mirrorCount; i++)
            {
                if (!slots.TrySetCharacter(_mirror[i].Id, _mirror[i].CharacterIndex))
                {
                    _lobbyChannel.SetReject(OnlineSessionErrorKeys.CannotStart);
                    return;
                }
            }

            _phase.Value = OnlineLobbyPhase.Starting;
            NetworkSceneLoader loader = _anchor.SceneLoader;
            if (loader == null || !loader.LoadGameplay())
            {
                _phase.Value = OnlineLobbyPhase.Open;
                _lobbyChannel.SetReject(OnlineSessionErrorKeys.StartFailed);
                return;
            }

            // The Single scene load may already have despawned this object (destroyWithScene); only despawn it otherwise.
            Debug.Log($"[{nameof(NetworkLobbyState)}] Match starting.", this);
            if (IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
        }

        // ---------------------------------------------------------------- Every peer

        private void HandleEntriesChanged(NetworkListEvent<LobbySlotEntry> change)
        {
            Publish();
        }

        private void HandlePhaseChanged(OnlineLobbyPhase previous, OnlineLobbyPhase current)
        {
            Publish();
        }

        private void Publish()
        {
            if (_lobbyChannel == null || !IsSpawned)
            {
                return;
            }

            ulong localClientId = _manager.LocalClientId;
            int count = Mathf.Min(_entries.Count, MaxSeats);
            PlayerId localId = PlayerId.None;
            for (int i = 0; i < count; i++)
            {
                LobbySlotEntry entry = _entries[i];
                _publishEntries[i] = entry;

                if (_names[i] == null || !_nameSources[i].Equals(entry.DisplayName))
                {
                    _nameSources[i] = entry.DisplayName;
                    _names[i] = entry.DisplayName.ToString();
                }

                bool isLocal = entry.ClientId == localClientId;
                if (isLocal)
                {
                    localId = entry.Id;
                }

                int character = entry.HasCharacter ? entry.CharacterIndex : OnlineLobbySeat.NoCharacter;
                _publishSeats[i] = new OnlineLobbySeat(entry.Id, character, _names[i], isLocal,
                    entry.ClientId == NetworkManager.ServerClientId);
            }

            bool isSpectator = count >= MaxSeats && localId == PlayerId.None;
            bool canStart = LobbyRules.CanStart(_publishEntries, count, RequiredSeats);

            _lobbyChannel.SetPhase(_phase.Value);
            _lobbyChannel.SetSeats(_publishSeats, count, localId, isSpectator, canStart);
        }

        private void Unsubscribe()
        {
            if (_serverSubscribed)
            {
                _serverSubscribed = false;
                if (_manager != null)
                {
                    _manager.OnClientConnectedCallback -= HandleClientConnected;
                    _manager.OnClientDisconnectCallback -= HandleClientDisconnected;
                }

                if (_lobbyChannel != null)
                {
                    _lobbyChannel.StartRequested -= ServerTryStart;
                }
            }

            if (_peerSubscribed)
            {
                _peerSubscribed = false;
                _entries.OnListChanged -= HandleEntriesChanged;
                _phase.OnValueChanged -= HandlePhaseChanged;
                if (_lobbyChannel != null)
                {
                    _lobbyChannel.CharacterRequested -= HandleCharacterRequested;
                }

                if (_session != null)
                {
                    _session.Changed -= Publish;
                }
            }
        }

        private void OnValidate()
        {
            if (_anchor == null)
            {
                Debug.LogWarning($"[{nameof(NetworkLobbyState)}] Missing NetworkRuntimeAnchor on '{name}'.", this);
            }

            if (_settings == null)
            {
                Debug.LogWarning($"[{nameof(NetworkLobbyState)}] Missing NetworkSettings on '{name}'.", this);
            }

            if (_roster == null)
            {
                Debug.LogWarning($"[{nameof(NetworkLobbyState)}] Missing CharacterRoster on '{name}'.", this);
            }

            if (_lobbyChannel == null)
            {
                Debug.LogWarning($"[{nameof(NetworkLobbyState)}] Missing OnlineLobbyChannel on '{name}'.", this);
            }
        }
    }
}
