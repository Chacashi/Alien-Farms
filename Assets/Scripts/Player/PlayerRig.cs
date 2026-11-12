using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Input;
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

        [SerializeField, Tooltip("Menu input on this root (pause and results menus).")]
        private PlayerMenuInput _menu;

        [SerializeField, Tooltip("Aim on this root (auto-wired from this root when empty).")]
        private PlayerAim _aim;

        [SerializeField, Tooltip("Tackle on this root (auto-wired from this root when empty).")]
        private PlayerTackle _tackle;

        [SerializeField, Tooltip("Blast stun receiver on this root (auto-wired from this root when empty).")]
        private PlayerStunReceiver _stunReceiver;

        public PlayerId Id => _identity != null ? _identity.Id : PlayerId.None;

        public PlayerGrabber Grabber => _grabber;

        public PlayerMenuInput Menu => _menu;

        public PlayerMovement Movement => _movement;

        public PlayerAim Aim => _aim;

        public PlayerTackle Tackle => _tackle;

        public ThirdPersonCamera Camera => _camera;

        public PlayerStunReceiver StunReceiver => _stunReceiver;

        private void Awake()
        {
            // Same-root components added after the prefab was last saved: resolved once here.
            if (_aim == null)
            {
                TryGetComponent(out _aim);
            }

            if (_tackle == null)
            {
                TryGetComponent(out _tackle);
            }

            if (_stunReceiver == null)
            {
                TryGetComponent(out _stunReceiver);
            }
        }

        /// <summary>Assigns identity, places the player at the spawn, aligns its camera and shows its character visual.</summary>
        public void Configure(PlayerId id, Transform spawn, CharacterDefinition character)
        {
            if (_identity != null)
            {
                _identity.Initialize(id);
            }

            Respawn(spawn);

            if (_appearance != null)
            {
                _appearance.ApplyCharacter(character);
            }
        }

        /// <summary>Drops anything carried (Forced), moves the player to <paramref name="spawn"/> and aligns its camera.</summary>
        public void Respawn(Transform spawn)
        {
            if (spawn == null)
            {
                return;
            }

            PlaceAt(spawn.position, spawn.eulerAngles.y);
        }

        /// <summary>
        /// Places the player at a saved pose without going through a spawn point (e.g. an online seat restored after a
        /// reconnection): drops anything carried (Forced), teleports it facing <paramref name="yaw"/> (degrees) and aligns
        /// its camera.
        /// </summary>
        public void PlaceAt(Vector3 position, float yaw)
        {
            if (_grabber != null)
            {
                _grabber.ForceDrop(DropReason.Forced, Vector3.zero);
            }

            if (_movement != null)
            {
                _movement.Teleport(position, Quaternion.Euler(0f, yaw, 0f));
            }

            if (_camera != null)
            {
                _camera.SnapYaw(yaw);
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

        /// <summary>
        /// Game pause: carried objects survive the input map switch (Grab 'canceled' fired by disabling the map is ignored).
        /// </summary>
        public void SetPaused(bool paused)
        {
            if (_grabber != null)
            {
                _grabber.SetPaused(paused);
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

            if (_menu == null)
            {
                TryGetComponent(out _menu);
            }

            if (_aim == null)
            {
                TryGetComponent(out _aim);
            }

            if (_tackle == null)
            {
                TryGetComponent(out _tackle);
            }

            if (_stunReceiver == null)
            {
                TryGetComponent(out _stunReceiver);
            }
        }
    }
}
