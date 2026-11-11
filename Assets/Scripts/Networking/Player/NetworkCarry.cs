using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Input;
using AlienFarms.Player;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Player
{
    /// <summary>
    /// Release and throw of what a remote player carries.
    /// <list type="bullet">
    /// <item>Server, remote avatar: the grabber runs with <see cref="ReleaseMode.External"/>. Grab released without
    /// aiming drops (<see cref="PlayerGrabber.ReleaseHeld"/>). Released while aiming waits for the owner's throw request
    /// (<see cref="NetworkCarriable.TryRequestThrow"/>); the reported view is validated against the last view the server
    /// knows (± <see cref="NetworkSettings.ThrowViewTolerance"/>) before <see cref="PlayerGrabber.ThrowWithView"/>.
    /// No request within <see cref="NetworkSettings.ThrowRequestTimeout"/> = plain drop.</item>
    /// <item>Owner, pure client: releasing Grab while aiming sends the throw request through the carried object
    /// (which this client owns while carrying it) with the camera's current view.</item>
    /// <item>Host's own avatar: untouched (local input, <see cref="ReleaseMode.Auto"/>).</item>
    /// </list>
    /// Runs after the network input applied this step's command and before the grabber.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkCarry : NetworkBehaviour
    {
        private enum Mode
        {
            Idle = 0,
            ServerRemote = 1,
            OwnerClient = 2
        }

        [Header("Config")]
        [SerializeField, Tooltip("Throw view tolerance and request timeout.")]
        private NetworkSettings _settings;

        [Header("References")]
        [SerializeField, Tooltip("Grabber on this root.")]
        private PlayerGrabber _grabber;

        [SerializeField, Tooltip("Aim on this root (decides drop vs. throw).")]
        private PlayerAim _aim;

        [SerializeField, Tooltip("Orbit camera rig (child CameraRig): the owner's view, the server's last known view.")]
        private ThirdPersonCamera _camera;

        [SerializeField, Tooltip("Network input on this root (server: the remote player's commands).")]
        private NetworkPlayerInput _input;

        [SerializeField, Tooltip("Local input of this avatar (owner: Grab release).")]
        private PlayerInputHandler _localInput;

        private Mode _mode = Mode.Idle;
        private bool _throwPending;
        private float _pendingSince;

        /// <summary>Server: a throw request is awaited for the carried object.</summary>
        public bool IsThrowPending => _throwPending;

        public override void OnNetworkSpawn()
        {
            if (IsServer && !IsOwner)
            {
                if (_grabber != null)
                {
                    _grabber.SetReleaseMode(ReleaseMode.External);
                }

                _throwPending = false;
                _mode = Mode.ServerRemote;
            }
            else if (IsOwner && !IsServer && _localInput != null)
            {
                _localInput.GrabCanceled += HandleLocalGrabCanceled;
                _mode = Mode.OwnerClient;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (_mode == Mode.ServerRemote && _grabber != null)
            {
                _grabber.SetReleaseMode(ReleaseMode.Auto);
            }
            else if (_mode == Mode.OwnerClient && _localInput != null)
            {
                _localInput.GrabCanceled -= HandleLocalGrabCanceled;
            }

            _throwPending = false;
            _mode = Mode.Idle;
        }

        /// <summary>
        /// Server: a throw request for <paramref name="target"/> sent by <paramref name="senderClientId"/> (the object's
        /// owner). Thrown with the reported view when it matches the known one; otherwise rejected (logged) and dropped.
        /// </summary>
        public void ServerHandleThrowRequest(IGrabbable target, float yaw, float pitch, ulong senderClientId)
        {
            if (_mode != Mode.ServerRemote || _grabber == null)
            {
                Debug.Log($"[{nameof(NetworkCarry)}] Throw request from client {senderClientId} ignored on '{name}': not a remote avatar.", this);
                return;
            }

            if (senderClientId != OwnerClientId || !_grabber.IsHolding || !ReferenceEquals(_grabber.Held, target))
            {
                Debug.Log($"[{nameof(NetworkCarry)}] Throw request from client {senderClientId} rejected on '{name}': it does not carry that object.", this);
                return;
            }

            _throwPending = false;
            float knownYaw = _camera != null ? _camera.Yaw : yaw;
            float knownPitch = _camera != null ? _camera.Pitch : pitch;

            // Without settings there is no tolerance to validate against: the request becomes a drop.
            if (_settings != null && ThrowViewRules.IsWithinTolerance(yaw, pitch, knownYaw, knownPitch, _settings.ThrowViewTolerance))
            {
                _grabber.ThrowWithView(yaw, pitch);
            }
            else
            {
                Debug.Log($"[{nameof(NetworkCarry)}] Throw from client {senderClientId} rejected on '{name}': view ({yaw:F1}, {pitch:F1}) too far from the server's ({knownYaw:F1}, {knownPitch:F1}). Dropped instead.", this);
                _grabber.ForceDrop(DropReason.Released, Vector3.zero);
            }

            // The Grab press may still read as held for a few buffered steps: never re-grab right away.
            _grabber.RequireGrabRepress();
        }

        private void FixedUpdate()
        {
            if (_mode != Mode.ServerRemote || _grabber == null || _input == null)
            {
                return;
            }

            IPlayerCommandSource commands = _input.ServerCommands;
            if (commands == null || !_grabber.IsHolding || commands.IsGrabHeld)
            {
                _throwPending = false;
                return;
            }

            // A paused client stops sending Grab (its gameplay map is disabled for the menu): keep what it carries, as
            // the host's own grabber does while paused. After resuming the Grab bit decides again.
            if (_input.IsRemotePaused || _grabber.IsPaused)
            {
                _throwPending = false;
                return;
            }

            if (_throwPending)
            {
                // Only entered with settings (see below).
                float timeout = _settings.ThrowRequestTimeout;
                if (Time.time - _pendingSince >= timeout)
                {
                    _throwPending = false;
                    Debug.Log($"[{nameof(NetworkCarry)}] No throw request from client {OwnerClientId} within {timeout:F2} s on '{name}': dropped.", this);
                    _grabber.ForceDrop(DropReason.Released, Vector3.zero);
                }

                return;
            }

            if (_aim != null && _aim.IsAiming && _settings != null)
            {
                // The owner sends the exact view it aimed with; wait for it.
                _throwPending = true;
                _pendingSince = Time.time;
                return;
            }

            _grabber.ReleaseHeld();
        }

        private void HandleLocalGrabCanceled()
        {
            if (_grabber == null || _aim == null || _camera == null || !_grabber.IsHolding || !_aim.IsAiming)
            {
                return;
            }

            // Input event, not per frame: resolving the carried object's adapter here is fine.
            if (_grabber.Held is Component held && held != null && held.TryGetComponent(out NetworkCarriable carriable))
            {
                carriable.TryRequestThrow(_camera.Yaw, _camera.Pitch);
            }
        }

        private void OnValidate()
        {
            if (_grabber == null)
            {
                TryGetComponent(out _grabber);
            }

            if (_aim == null)
            {
                TryGetComponent(out _aim);
            }

            if (_input == null)
            {
                TryGetComponent(out _input);
            }

            if (_localInput == null)
            {
                TryGetComponent(out _localInput);
            }

            if (_settings == null)
            {
                Debug.LogWarning($"[{nameof(NetworkCarry)}] Missing NetworkSettings on '{name}'.", this);
            }

            if (_camera == null)
            {
                Debug.LogWarning($"[{nameof(NetworkCarry)}] Missing ThirdPersonCamera on '{name}'.", this);
            }
        }
    }
}
