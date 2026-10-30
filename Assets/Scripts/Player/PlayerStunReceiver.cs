using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Utilities;
using UnityEngine;

namespace AlienFarms.Player
{
    /// <summary>
    /// Blast target on the player root: drops whatever is carried (thrown away from the blast), knocks the player back
    /// and stuns it. Stuns never stack: the longest remaining one wins (<see cref="PlayerMovement.ApplyKnockback"/>).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerStunReceiver : MonoBehaviour, IStunnable
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

        public PlayerId Id => _identity != null ? _identity.Id : PlayerId.None;

        public void ReceiveStun(PlayerId source, Vector3 origin, float duration, float knockbackSpeed)
        {
            if (_movement == null)
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
