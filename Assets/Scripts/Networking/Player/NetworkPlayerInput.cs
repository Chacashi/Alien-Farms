using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Input;
using AlienFarms.Player;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Player
{
    /// <summary>
    /// Input over the network, one command per physics step.
    /// Owner (pure client): samples its <see cref="PlayerInputHandler"/> and camera view and sends the last commands
    /// unreliably (redundant). Server, remote avatar: buffers them (<see cref="InputCommandBuffer"/>), applies one per
    /// step to a <see cref="NetworkCommandSource"/> injected into movement / grab / aim, and drives the camera view so
    /// movement is relative to the client's camera. The host's own avatar keeps its local handler.
    /// Runs before the gameplay components so they read this step's command.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkPlayerInput : NetworkBehaviour
    {
        private enum Mode
        {
            Idle = 0,
            OwnerSender = 1,
            ServerReceiver = 2
        }

        [Header("Config")]
        [SerializeField, Tooltip("Input redundancy and server jitter buffer depth.")]
        private NetworkSettings _settings;

        [Header("References")]
        [SerializeField, Tooltip("Local input of this avatar (sampled by the owner; restored on the server at despawn).")]
        private PlayerInputHandler _localInput;

        [SerializeField, Tooltip("Orbit camera rig (child CameraRig): view sampled by the owner, driven on the server.")]
        private ThirdPersonCamera _camera;

        [SerializeField, Tooltip("Movement on this root.")]
        private PlayerMovement _movement;

        [SerializeField, Tooltip("Grabber on this root.")]
        private PlayerGrabber _grabber;

        [SerializeField, Tooltip("Aim on this root.")]
        private PlayerAim _aim;

        private readonly NetworkCommandSource _source = new NetworkCommandSource();
        private InputCommandBuffer _buffer;
        private InputCommandBatch _history;
        private Mode _mode = Mode.Idle;
        private uint _nextSeq;
        private bool _grabLatched;

        /// <summary>Server, remote avatar: the network-fed commands its gameplay reads (null anywhere else).</summary>
        public IPlayerCommandSource ServerCommands => _mode == Mode.ServerReceiver ? _source : null;

        /// <summary>Server, remote avatar: the owner reports being paused in its last applied command.</summary>
        public bool IsRemotePaused => _mode == Mode.ServerReceiver && _source.IsPaused;

        /// <summary>Server: sequence of the last command applied to this avatar (0 = none, or the host's own avatar).</summary>
        public uint LastAppliedSeq => _buffer != null ? _buffer.LastConsumedSeq : 0u;

        public override void OnNetworkSpawn()
        {
            if (IsServer && !IsOwner)
            {
                StartReceiving();
            }
            else if (IsOwner && !IsServer)
            {
                StartSending();
            }
        }

        public override void OnNetworkDespawn()
        {
            if (_mode == Mode.ServerReceiver)
            {
                StopReceiving();
            }
            else if (_mode == Mode.OwnerSender && _localInput != null)
            {
                _localInput.GrabStarted -= HandleLocalGrabStarted;
            }

            _mode = Mode.Idle;
        }

        private void StartReceiving()
        {
            int depth = _settings != null ? _settings.InputBufferTarget : 2;
            _buffer = new InputCommandBuffer(depth);
            _source.Reset();

            if (_movement != null)
            {
                _movement.SetCommandSource(_source);
            }

            if (_grabber != null)
            {
                _grabber.SetCommandSource(_source);
            }

            if (_aim != null)
            {
                _aim.SetCommandSource(_source);
            }

            if (_camera != null)
            {
                _camera.SetCommandSource(_source);
            }

            _mode = Mode.ServerReceiver;
        }

        private void StopReceiving()
        {
            _source.Reset();

            if (_movement != null)
            {
                _movement.SetCommandSource(_localInput);
            }

            if (_grabber != null)
            {
                _grabber.SetCommandSource(_localInput);
            }

            if (_aim != null)
            {
                _aim.SetCommandSource(_localInput);
            }

            if (_camera != null)
            {
                _camera.SetCommandSource(_localInput);
                _camera.ReleaseDrivenView();
            }

            _buffer = null;
        }

        private void StartSending()
        {
            if (_localInput == null || _camera == null)
            {
                Debug.LogError($"[{nameof(NetworkPlayerInput)}] Missing local input or camera on '{name}'; this player cannot send input.", this);
                return;
            }

            _nextSeq = 1;
            _history.Clear();
            _grabLatched = false;
            _localInput.GrabStarted += HandleLocalGrabStarted;
            _mode = Mode.OwnerSender;
        }

        private void FixedUpdate()
        {
            switch (_mode)
            {
                case Mode.OwnerSender:
                    SendCommand();
                    break;
                case Mode.ServerReceiver:
                    ConsumeCommand();
                    break;
            }
        }

        private void SendCommand()
        {
            // A press shorter than a physics step still reaches the server as one step with Grab held.
            bool grab = _localInput.IsGrabHeld || _grabLatched;
            _grabLatched = false;

            uint seq = _nextSeq;
            unchecked
            {
                _nextSeq++;
            }

            // 0 means "no command": skip it when the sequence wraps around.
            if (_nextSeq == 0)
            {
                _nextSeq = 1;
            }

            // The grabber knows the local pause (menu open, or just resumed): the server must not read the Grab release
            // it causes as a drop.
            bool paused = _grabber != null && _grabber.IsPaused;
            PlayerInputCommand command = PlayerInputCommand.Create(
                seq, _localInput.Move, _camera.Yaw, _camera.Pitch, _localInput.IsSprintHeld, grab, _localInput.IsAimHeld, paused);
            _history.Push(command);

            int redundancy = _settings != null ? _settings.InputRedundancy : InputCommandBatch.MaxCommands;
            SubmitInputRpc(redundancy >= _history.Count ? _history : Truncate(_history, redundancy));
        }

        private void ConsumeCommand()
        {
            PlayerInputCommand command = _buffer.Consume(out _);
            if (command.Seq == 0)
            {
                // Nothing received yet: keep the avatar idle.
                return;
            }

            _source.Apply(command);
            if (_camera != null)
            {
                _camera.SetDrivenView(command.Yaw, command.Pitch);
            }
        }

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable, InvokePermission = RpcInvokePermission.Owner)]
        private void SubmitInputRpc(InputCommandBatch batch)
        {
            if (_buffer == null)
            {
                return;
            }

            // Oldest first, so a full batch after a gap fills it in order.
            for (int i = batch.Count - 1; i >= 0; i--)
            {
                _buffer.Enqueue(batch.Get(i));
            }
        }

        private void HandleLocalGrabStarted()
        {
            _grabLatched = true;
        }

        private static InputCommandBatch Truncate(in InputCommandBatch source, int count)
        {
            var batch = new InputCommandBatch();
            for (int i = Mathf.Min(count, source.Count) - 1; i >= 0; i--)
            {
                batch.Push(source.Get(i));
            }

            return batch;
        }

        private void OnValidate()
        {
            if (_localInput == null)
            {
                TryGetComponent(out _localInput);
            }

            if (_movement == null)
            {
                TryGetComponent(out _movement);
            }

            if (_grabber == null)
            {
                TryGetComponent(out _grabber);
            }

            if (_aim == null)
            {
                TryGetComponent(out _aim);
            }

            if (_settings == null)
            {
                Debug.LogWarning($"[{nameof(NetworkPlayerInput)}] Missing NetworkSettings on '{name}'.", this);
            }

            if (_camera == null)
            {
                Debug.LogWarning($"[{nameof(NetworkPlayerInput)}] Missing ThirdPersonCamera on '{name}'.", this);
            }
        }
    }
}
