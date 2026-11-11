using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Utilities;
using UnityEngine;

namespace AlienFarms.Player
{
    /// <summary>
    /// Blast target on the player root: drops whatever is carried (thrown away from the blast), knocks the player back
    /// and stuns it. Stuns never stack: the longest remaining one wins (<see cref="PlayerMovement.ApplyKnockback"/>).
    /// As a <see cref="SimulationRole.Replica"/> blasts are ignored (the authority applies them).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerStunReceiver : MonoBehaviour, IStunnable, ISimulationRoleTarget
    {
        [Header("Config")]
        [SerializeField, Tooltip("Shared player tuning (StealDropSpeed: how hard the carried object is thrown off).")]
        private PlayerSettings _settings;

        [Header("References")]
        [SerializeField, Tooltip("Identity of this player.")]
        private PlayerIdentity _identity;

        [SerializeField, Tooltip("Movement on this root (knockback and stun).")]
        private PlayerMovement _movement;

        [SerializeField, Tooltip("Grabber on this root (drops the carried object).")]
        private PlayerGrabber _grabber;

        private SimulationRole _role = SimulationRole.Authority;

        public PlayerId Id => _identity != null ? _identity.Id : PlayerId.None;

        public SimulationRole Role => _role;

        /// <summary>Seconds until the current stun (blast or tackle) ends; 0 when not stunned.</summary>
        public float StunRemaining => _movement != null ? _movement.StunRemaining : 0f;

        public void SetSimulationRole(SimulationRole role)
        {
            _role = role;
        }

        public void ReceiveStun(PlayerId source, Vector3 origin, float duration, float knockbackSpeed)
        {
            if (_movement == null || _role == SimulationRole.Replica)
            {
                return;
            }

            Vector3 direction = PlanarMath.KnockbackDirection(origin, transform.position, -_movement.Facing);

            if (_grabber != null)
            {
                float dropSpeed = _settings != null ? _settings.StealDropSpeed : 0f;
                _grabber.ForceDrop(DropReason.Stunned, direction * dropSpeed);
            }

            _movement.ApplyKnockback(direction * knockbackSpeed, duration);
            GameEvents.RaisePlayerStunned(Id, source, duration);
        }

        private void OnValidate()
        {
            if (_settings == null)
            {
                Debug.LogWarning($"[{nameof(PlayerStunReceiver)}] Missing PlayerSettings on '{name}'.", this);
            }

            if (_identity == null)
            {
                TryGetComponent(out _identity);
            }

            if (_movement == null)
            {
                TryGetComponent(out _movement);
            }

            if (_grabber == null)
            {
                TryGetComponent(out _grabber);
            }
        }
    }
}
