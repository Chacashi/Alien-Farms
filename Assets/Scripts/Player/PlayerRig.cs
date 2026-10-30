using AlienFarms.Core;
using UnityEngine;

namespace AlienFarms.Player
{
    /// <summary>
    /// Facade on the player root that groups the per-player components, so scene composition roots
    /// configure a spawned player through a single reference.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerIdentity))]
    public sealed class PlayerRig : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Identity on this root.")]
        private PlayerIdentity _identity;

        [SerializeField, Tooltip("Movement on this root.")]
        private PlayerMovement _movement;

        [SerializeField, Tooltip("Orbit camera rig (child CameraRig).")]
        private ThirdPersonCamera _camera;

        [SerializeField, Tooltip("Appearance on this root.")]
        private PlayerAppearance _appearance;

        [SerializeField, Tooltip("Grabber on this root.")]
        private PlayerGrabber _grabber;

        public PlayerId Id => _identity != null ? _identity.Id : PlayerId.None;

        public PlayerGrabber Grabber => _grabber;

        /// <summary>Assigns identity, places the player at the spawn, aligns its camera and applies its color.</summary>
        public void Configure(PlayerId id, Transform spawn, Material material)
        {
            if (_identity != null)
            {
                _identity.Initialize(id);
            }

            Respawn(spawn);

            if (_appearance != null)
            {
                _appearance.ApplyMaterial(material);
            }
        }

        /// <summary>Drops anything carried (Forced), moves the player to <paramref name="spawn"/> and aligns its camera.</summary>
        public void Respawn(Transform spawn)
        {
            if (spawn == null)
            {
                return;
            }

            if (_grabber != null)
            {
                _grabber.ForceDrop(DropReason.Forced, Vector3.zero);
            }

            if (_movement != null)
            {
                _movement.Teleport(spawn.position, spawn.rotation);
            }

            if (_camera != null)
            {
                _camera.SnapYaw(spawn.eulerAngles.y);
            }
        }

        /// <summary>Freezes (false) or unfreezes (true) movement and grabbing. The camera stays free.</summary>
        public void SetControlsEnabled(bool enabled)
        {
            if (_movement != null)
            {
                _movement.SetInputEnabled(enabled);
            }

            if (_grabber != null)
            {
                _grabber.SetInputEnabled(enabled);
            }
        }

        private void OnValidate()
        {
            if (_identity == null)
            {
                TryGetComponent(out _identity);
            }

            if (_movement == null)
            {
                Debug.LogWarning($"[{nameof(PlayerRig)}] Missing PlayerMovement on '{name}'.", this);
            }

            if (_camera == null)
            {
                Debug.LogWarning($"[{nameof(PlayerRig)}] Missing ThirdPersonCamera on '{name}'.", this);
            }

            if (_appearance == null)
            {
                Debug.LogWarning($"[{nameof(PlayerRig)}] Missing PlayerAppearance on '{name}'.", this);
            }

            // Lives on this same root: auto-wire instead of warning.
            if (_grabber == null)
            {
                TryGetComponent(out _grabber);
            }
        }
    }
}
