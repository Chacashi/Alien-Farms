using AlienFarms.Data;
using AlienFarms.Networking.Prediction;
using AlienFarms.Player;
using Unity.Netcode;
using UnityEngine;

namespace AlienFarms.Networking.Player
{
    /// <summary>
    /// Player position sync with custom snapshots (no NetworkTransform: later prediction and lag compensation need to
    /// know which server time is drawn).
    /// Server: after each physics step sends a <see cref="PlayerSnapshot"/> to every client (unreliable).
    /// Clients: buffer them and show the pose at ServerTime - InterpolationDelay on a kinematic body, the owner too
    /// (no prediction yet). Teleports snap instead of sliding.
    /// Runs before input and movement so the snapshot holds the state the last physics step produced.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkPlayerMotion : NetworkBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Interpolation delay.")]
        private NetworkSettings _settings;

        [Header("References")]
        [SerializeField, Tooltip("Movement on this root (source on the server, replica pose on clients).")]
        private PlayerMovement _movement;

        [SerializeField, Tooltip("Rigidbody on this root (kinematic on clients).")]
        private Rigidbody _rb;

        [SerializeField, Tooltip("Network input on this root (ack of the last applied command).")]
        private NetworkPlayerInput _input;

        private readonly SnapshotBuffer _buffer = new SnapshotBuffer();
        private uint _tick;
        private double _clockOffset;
        private bool _clockReady;
        private byte _teleportEpoch;
        private bool _snapPending;

        /// <summary>Snapshots buffered on this client.</summary>
        public int BufferedSnapshots => _buffer.Count;

        public override void OnNetworkSpawn()
        {
            _buffer.Clear();
            _tick = 0;
            _clockReady = false;

            // Clients snap on their first snapshot (the buffer reports a reset), whatever the epoch.
            if (IsServer)
            {
                return;
            }

            if (_rb != null)
            {
                _rb.isKinematic = true;
                _rb.interpolation = RigidbodyInterpolation.None;
            }
        }

        /// <summary>Server: starts a new teleport epoch (call after an instant move such as a respawn); clients snap to it.</summary>
        public void MarkTeleport()
        {
            unchecked
            {
                _teleportEpoch++;
            }
        }

        private void Update()
        {
            if (!IsSpawned)
            {
                return;
            }

            if (IsServer)
            {
                CalibrateClock();
                return;
            }

            RenderInterpolated();
        }

        private void FixedUpdate()
        {
            // No snapshot before the first Update calibrated the clock (stamps would be off by up to a frame).
            if (!IsSpawned || !IsServer || _movement == null || !_clockReady)
            {
                return;
            }

            PlayerSnapshotFlags flags = PlayerSnapshotFlags.None;

            if (_movement.IsSprinting)
            {
                flags |= PlayerSnapshotFlags.Sprinting;
            }

            if (_movement.IsAiming)
            {
                flags |= PlayerSnapshotFlags.Aiming;
            }

            Vector3 facing = _movement.Facing;
            var snapshot = new PlayerSnapshot
            {
                Tick = ++_tick,
                // Physics time mapped onto the server clock: steps stay 1 fixedDeltaTime apart even when several run in a frame.
                ServerTime = Time.fixedTimeAsDouble + _clockOffset,
                Position = _rb != null ? _rb.position : transform.position,
                Velocity = _movement.Velocity,
                FacingYaw = Mathf.Atan2(facing.x, facing.z) * Mathf.Rad2Deg,
                StunRemaining = _movement.StunRemaining,
                AckSeq = _input != null ? _input.LastAppliedSeq : 0u,
                TeleportEpoch = _teleportEpoch,
                Flags = flags
            };

            SnapshotRpc(snapshot);
        }

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable, InvokePermission = RpcInvokePermission.Server)]
        private void SnapshotRpc(PlayerSnapshot snapshot)
        {
            if (_buffer.Add(snapshot) == SnapshotAddResult.Reset)
            {
                _snapPending = true;
            }
        }

        private void CalibrateClock()
        {
            // Only from Update: there Time.timeAsDouble is the frame time, the same instant ServerTime refers to.
            _clockOffset = NetworkManager.ServerTime.Time - Time.timeAsDouble;
            _clockReady = true;
        }

        private void RenderInterpolated()
        {
            if (_movement == null || _settings == null)
            {
                return;
            }

            double renderTime = NetworkManager.ServerTime.Time - _settings.InterpolationDelay;
            if (!SnapshotInterpolator.TrySample(_buffer, renderTime, out InterpolatedPose pose))
            {
                return;
            }

            _movement.ApplyReplicaPose(pose.Position, pose.FacingDirection, pose.StunRemaining, _snapPending);
            _snapPending = false;
        }

        private void OnValidate()
        {
            if (_movement == null)
            {
                TryGetComponent(out _movement);
            }

            if (_rb == null)
            {
                TryGetComponent(out _rb);
            }

            if (_input == null)
            {
                TryGetComponent(out _input);
            }

            if (_settings == null)
            {
                Debug.LogWarning($"[{nameof(NetworkPlayerMotion)}] Missing NetworkSettings on '{name}'.", this);
            }
        }
    }
}
