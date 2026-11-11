using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Networking.Data;
using AlienFarms.Networking.Session;
using AlienFarms.Player;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AlienFarms.Networking.Player
{
    /// <summary>
    /// Network identity of a Player_Net avatar. The roster hands PlayerId, character and name over before spawning and
    /// the server writes them in OnNetworkSpawn (NetworkVariables cannot be written earlier); every instance applies
    /// them to <see cref="PlayerIdentity"/> / <see cref="PlayerAppearance"/>.
    /// Only the owner enables input, UI input, camera (full screen) and audio listener, and registers itself as the
    /// anchor's local player. The orbit camera rig runs on the owner and, on the server, on remote avatars (driven by
    /// the client's view). Off the server the simulation runs as Replica (no prediction yet). On pure clients this adapter raises the join/character/leave <see cref="GameEvents"/>; on the
    /// server the roster does (no double events on the host).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkPlayer : NetworkBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Characters by index (CharacterIndex refers to this order).")]
        private CharacterRoster _characters;

        [SerializeField, Tooltip("Runtime anchor: the owner registers itself as the local player (menus route to it).")]
        private NetworkRuntimeAnchor _anchor;

        [Header("Gameplay references")]
        [SerializeField, Tooltip("Rig facade on this root.")]
        private PlayerRig _rig;

        [SerializeField, Tooltip("Identity on this root.")]
        private PlayerIdentity _identity;

        [SerializeField, Tooltip("Appearance on this root.")]
        private PlayerAppearance _appearance;

        [SerializeField, Tooltip("Movement on this root (Replica off the server).")]
        private PlayerMovement _movement;

        [SerializeField, Tooltip("Grabber on this root (Replica off the server).")]
        private PlayerGrabber _grabber;

        [SerializeField, Tooltip("Blast stun receiver on this root (Replica off the server).")]
        private PlayerStunReceiver _stunReceiver;

        [SerializeField, Tooltip("Snapshot sync on this root (the roster flags teleports through it).")]
        private NetworkPlayerMotion _motion;

        [SerializeField, Tooltip("Network carry on this root (server-side release and throw validation).")]
        private NetworkCarry _carry;

        [Header("Owner-only (saved disabled in Player_Net)")]
        [SerializeField, Tooltip("PlayerInput on this root.")]
        private PlayerInput _playerInput;

        [SerializeField, Tooltip("Player camera (child Camera).")]
        private Camera _camera;

        [SerializeField, Tooltip("Optional audio listener of this player.")]
        private AudioListener _audioListener;

        [SerializeField, Tooltip("Other owner-only behaviours: UIInput's MultiplayerEventSystem and InputSystemUIInputModule.")]
        private Behaviour[] _ownerOnlyBehaviours = new Behaviour[0];

        private readonly NetworkVariable<PlayerId> _playerId = new NetworkVariable<PlayerId>(
            PlayerId.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<byte> _characterIndex = new NetworkVariable<byte>(
            PlayerSlot.NoCharacter, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<FixedString32Bytes> _displayName = new NetworkVariable<FixedString32Bytes>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // Written by the owner only (the server reads both players' flags to start the rematch).
        private readonly NetworkVariable<bool> _readyForRematch = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private PlayerId _raisedJoinFor = PlayerId.None;
        private bool _watchingPhase;
        private bool _hasPendingInit;
        private PlayerId _pendingId;
        private byte _pendingCharacter = PlayerSlot.NoCharacter;
        private FixedString32Bytes _pendingName;

        public PlayerId PlayerId => _playerId.Value;

        public int CharacterIndex => _characterIndex.Value == PlayerSlot.NoCharacter ? -1 : _characterIndex.Value;

        public FixedString32Bytes DisplayName => _displayName.Value;

        public NetworkPlayerMotion Motion => _motion;

        public NetworkCarry Carry => _carry;

        /// <summary>This player asked for a rematch on the current results screen.</summary>
        public bool IsReadyForRematch => _readyForRematch.Value;

        public PlayerRig Rig => _rig;

        /// <summary>Character for <see cref="CharacterIndex"/> (null when none or out of range).</summary>
        public CharacterDefinition Character => _characters != null ? _characters.Get(CharacterIndex) : null;

        /// <summary>
        /// Server, before Spawn: values every instance receives. They are written to the NetworkVariables in
        /// OnNetworkSpawn (clients get them with the spawn or through OnValueChanged).
        /// </summary>
        public void ServerInitialize(PlayerId id, int characterIndex, in FixedString32Bytes displayName)
        {
            _pendingId = id;
            _pendingCharacter = characterIndex >= 0 && characterIndex < PlayerSlot.NoCharacter
                ? (byte)characterIndex
                : PlayerSlot.NoCharacter;
            _pendingName = displayName;
            _hasPendingInit = true;
        }

        private void Awake()
        {
            if (_rig == null)
            {
                TryGetComponent(out _rig);
            }

            if (_carry == null)
            {
                TryGetComponent(out _carry);
            }
        }

        /// <summary>Owner only: marks (or clears) this player as ready for a rematch. Ignored on other instances.</summary>
        public void SetReadyForRematch(bool ready)
        {
            if (IsSpawned && IsOwner && _readyForRematch.Value != ready)
            {
                _readyForRematch.Value = ready;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer && _hasPendingInit)
            {
                _hasPendingInit = false;
                _playerId.Value = _pendingId;
                _characterIndex.Value = _pendingCharacter;
                _displayName.Value = _pendingName;
            }

            _playerId.OnValueChanged += HandlePlayerIdChanged;
            _characterIndex.OnValueChanged += HandleCharacterChanged;

            ApplyOwnership();
            ApplyRoles();
            ApplyIdentity();
            ApplyAppearance();
            RaiseClientJoin();

            if (IsOwner && _anchor != null)
            {
                _anchor.SetLocalPlayer(this);
            }

            if (IsOwner)
            {
                // The flag only means something on the results screen: the owner clears it when the phase moves on,
                // and never starts a session with a stale one (reconnect, pooled instance).
                SetReadyForRematch(false);
                GameEvents.OnMatchPhaseChanged += HandleMatchPhaseChanged;
                _watchingPhase = true;
            }
        }

        public override void OnGainedOwnership()
        {
            base.OnGainedOwnership();
            SetReadyForRematch(false);
        }

        public override void OnNetworkDespawn()
        {
            if (_anchor != null)
            {
                _anchor.ClearLocalPlayer(this);
                _anchor.UnregisterPlayer(this);
            }

            if (_watchingPhase)
            {
                GameEvents.OnMatchPhaseChanged -= HandleMatchPhaseChanged;
                _watchingPhase = false;
            }

            _playerId.OnValueChanged -= HandlePlayerIdChanged;
            _characterIndex.OnValueChanged -= HandleCharacterChanged;

            if (!IsServer && _raisedJoinFor != PlayerId.None)
            {
                GameEvents.RaisePlayerLeft(_raisedJoinFor);
            }

            _raisedJoinFor = PlayerId.None;
        }

        private void HandleMatchPhaseChanged(MatchPhase previous, MatchPhase current)
        {
            if (current != MatchPhase.Results)
            {
                SetReadyForRematch(false);
            }
        }

        private void HandlePlayerIdChanged(PlayerId previous, PlayerId current)
        {
            ApplyIdentity();
            RaiseClientJoin();
        }

        private void HandleCharacterChanged(byte previous, byte current)
        {
            ApplyAppearance();
            if (!IsServer && _raisedJoinFor != PlayerId.None)
            {
                GameEvents.RaisePlayerCharacterAssigned(_raisedJoinFor, Character);
            }
        }

        private void ApplyOwnership()
        {
            bool owner = IsOwner;

            if (_playerInput != null)
            {
                _playerInput.enabled = owner;
            }

            if (_camera != null)
            {
                _camera.enabled = owner;
                if (owner)
                {
                    // Online there is one player per screen.
                    _camera.rect = new Rect(0f, 0f, 1f, 1f);
                }
            }

            if (_audioListener != null)
            {
                _audioListener.enabled = owner;
            }

            // The orbit rig is needed by the owner (its view) and by the server for remote avatars (driven view that
            // movement reads); anywhere else it would only spend a sphere cast per frame.
            ThirdPersonCamera orbit = _rig != null ? _rig.Camera : null;
            if (orbit != null)
            {
                orbit.enabled = owner || IsServer;
            }

            if (_ownerOnlyBehaviours == null)
            {
                return;
            }

            for (int i = 0; i < _ownerOnlyBehaviours.Length; i++)
            {
                if (_ownerOnlyBehaviours[i] != null)
                {
                    _ownerOnlyBehaviours[i].enabled = owner;
                }
            }
        }

        private void ApplyRoles()
        {
            // No prediction yet: off the server every avatar (the owner's too) only shows the snapshots.
            SimulationRole role = IsServer ? SimulationRole.Authority : SimulationRole.Replica;

            if (_movement != null)
            {
                _movement.SetSimulationRole(role);
            }

            if (_grabber != null)
            {
                _grabber.SetSimulationRole(role);
            }

            if (_stunReceiver != null)
            {
                _stunReceiver.SetSimulationRole(role);
            }
        }

        private void ApplyIdentity()
        {
            if (_playerId.Value == PlayerId.None)
            {
                return;
            }

            if (_identity != null)
            {
                _identity.Initialize(_playerId.Value);
            }

            if (_anchor != null)
            {
                _anchor.RegisterPlayer(this);
            }
        }

        private void ApplyAppearance()
        {
            if (_appearance != null && CharacterIndex >= 0)
            {
                _appearance.ApplyCharacter(Character);
            }
        }

        /// <summary>Pure clients only: the server-side roster already raised these for the host.</summary>
        private void RaiseClientJoin()
        {
            if (IsServer)
            {
                return;
            }

            PlayerId id = _playerId.Value;
            if (id == PlayerId.None || id == _raisedJoinFor)
            {
                return;
            }

            if (_raisedJoinFor != PlayerId.None)
            {
                GameEvents.RaisePlayerLeft(_raisedJoinFor);
            }

            _raisedJoinFor = id;

            // Character first, as in local mode: listeners know who the player is when PlayerJoined arrives.
            GameEvents.RaisePlayerCharacterAssigned(id, Character);
            GameEvents.RaisePlayerJoined(id);
        }

        private void OnValidate()
        {
            if (_rig == null)
            {
                TryGetComponent(out _rig);
            }

            if (_anchor == null)
            {
                Debug.LogWarning($"[{nameof(NetworkPlayer)}] Missing NetworkRuntimeAnchor on '{name}'; online menus cannot find the local player.", this);
            }

            if (_identity == null)
            {
                TryGetComponent(out _identity);
            }

            if (_appearance == null)
            {
                TryGetComponent(out _appearance);
            }

            if (_movement == null)
            {
                TryGetComponent(out _movement);
            }

            if (_grabber == null)
            {
                TryGetComponent(out _grabber);
            }

            if (_stunReceiver == null)
            {
                TryGetComponent(out _stunReceiver);
            }

            if (_motion == null)
            {
                TryGetComponent(out _motion);
            }

            if (_carry == null)
            {
                TryGetComponent(out _carry);
            }

            if (_playerInput == null)
            {
                TryGetComponent(out _playerInput);
            }

            if (_characters == null)
            {
                Debug.LogWarning($"[{nameof(NetworkPlayer)}] Missing CharacterRoster on '{name}'.", this);
            }

            if (_camera == null)
            {
                Debug.LogWarning($"[{nameof(NetworkPlayer)}] Missing player camera on '{name}'.", this);
            }

            if (_ownerOnlyBehaviours == null || _ownerOnlyBehaviours.Length == 0)
            {
                Debug.LogWarning($"[{nameof(NetworkPlayer)}] No owner-only behaviours on '{name}'; UIInput stays active on every avatar.", this);
            }
        }
    }
}
