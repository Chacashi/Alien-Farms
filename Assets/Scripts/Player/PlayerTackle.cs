using AlienFarms.Core;
using AlienFarms.Data;
using UnityEngine;

namespace AlienFarms.Player
{
    /// <summary>
    /// Stealing by tackle: a sprinting player that runs into a carrier (fast enough and facing it)
    /// makes the carrier drop its animal, get knocked back and briefly stunned.
    /// Evaluated on the attacker's side of the collision.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerTackle : MonoBehaviour
    {
        private const float Epsilon = 0.0001f;

        [Header("Config")]
        [SerializeField, Tooltip("Shared tackle tuning.")]
        private PlayerSettings _settings;

        [Header("References")]
        [SerializeField, Tooltip("Movement on this root (sprint state, facing, knockback).")]
        private PlayerMovement _movement;

        [SerializeField, Tooltip("Grabber on this root (drops the animal when tackled).")]
        private PlayerGrabber _grabber;

        [SerializeField, Tooltip("Identity of this player.")]
        private PlayerIdentity _identity;

        private int _playerLayer;
        private float _immuneUntil;

        public PlayerId Id => _identity != null ? _identity.Id : PlayerId.None;

        /// <summary>True while carrying something and not protected by post-tackle immunity.</summary>
        public bool CanBeTackled => _grabber != null && _grabber.IsHolding && Time.time >= _immuneUntil;

        private void Awake()
        {
            _playerLayer = LayerMask.NameToLayer(LayerNames.Player);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_settings == null || _movement == null || !_movement.IsSprinting)
            {
                return;
            }

            Rigidbody other = collision.rigidbody;
            if (other == null || other.gameObject.layer != _playerLayer)
            {
                return;
            }

            Vector3 direction = other.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < Epsilon)
            {
                return;
            }

            direction.Normalize();
            if (Vector3.Dot(_movement.Facing, direction) < _settings.TackleFacingDot)
            {
                return;
            }

            // Closing speed = pre-solve relative velocity projected on the attacker->victim line. Using only this
            // component (not the full magnitude) lets a sprinter steal from a walking carrier from behind.
            // Abs makes it independent of relativeVelocity's sign convention: at contact the bodies are approaching,
            // and who is the attacker is already decided by IsSprinting + facing.
            float closingSpeed = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, direction));
            if (closingSpeed < _settings.TackleMinClosingSpeed)
            {
                return;
            }

            if (!other.TryGetComponent(out PlayerTackle victim) || victim == this || victim.Id == Id || !victim.CanBeTackled)
            {
                return;
            }

            victim.ReceiveTackle(direction);
        }

        /// <summary>Drops the carried animal (stolen), knocks this player back along <paramref name="planarDirection"/> and stuns it.</summary>
        public void ReceiveTackle(Vector3 planarDirection)
        {
            if (_settings == null)
            {
                return;
            }

            if (_grabber != null)
            {
                _grabber.ForceDrop(DropReason.Stolen, planarDirection * _settings.StealDropSpeed);
            }

            if (_movement != null)
            {
                _movement.ApplyKnockback(planarDirection * _settings.TackleKnockbackSpeed, _settings.TackleStunDuration);
            }

            _immuneUntil = Time.time + _settings.TackleImmunity;
        }

        private void OnValidate()
        {
            if (_settings == null)
            {
                Debug.LogWarning($"[{nameof(PlayerTackle)}] Missing PlayerSettings on '{name}'.", this);
            }

            if (_movement == null)
            {
                TryGetComponent(out _movement);
            }

            if (_grabber == null)
            {
                TryGetComponent(out _grabber);
            }

            if (_identity == null)
            {
                TryGetComponent(out _identity);
            }
        }
    }
}
