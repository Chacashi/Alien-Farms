using AlienFarms.Items;
using UnityEngine;

namespace AlienFarms.Data
{
    /// <summary>Definition of the stun bomb: prefab, carry/throw tuning, blast and VFX.</summary>
    [CreateAssetMenu(fileName = "BombData", menuName = "Alien Farms/Items/Bomb Data")]
    public sealed class BombData : ScriptableObject
    {
        [Header("Prefab")]
        [SerializeField, Tooltip("Pooled bomb prefab (root on the Bomb layer).")]
        private Bomb _prefab;

        [Header("Carry & Throw")]
        [SerializeField, Tooltip("Throw tuning for the bomb. Empty: the thrower's default throw settings.")]
        private ThrowSettings _throwSettings;

        [SerializeField, Range(0f, 1f), Tooltip("Walk speed multiplier while carrying the bomb.")]
        private float _carrySpeedMultiplier = 0.9f;

        [SerializeField, Tooltip("Local offset from the hold point while carried (m).")]
        private Vector3 _holdOffset = Vector3.zero;

        [SerializeField, Min(0f), Tooltip("After an inert drop, the bomb cannot be picked up again for this long (s).")]
        private float _grabCooldown = 0.25f;

        [Header("Blast")]
        [SerializeField, Min(0f), Tooltip("Players within this radius of the blast center are stunned (m).")]
        private float _blastRadius = 3.5f;

        [SerializeField, Min(0f), Tooltip("Stun duration of a blast (s). Does not stack with an active stun.")]
        private float _stunDuration = 2.5f;

        [SerializeField, Min(0f), Tooltip("Horizontal knockback speed applied away from the blast (m/s).")]
        private float _knockbackSpeed = 5f;

        [SerializeField, Min(0.1f), Tooltip("An armed bomb that has not touched anything explodes after this long (s).")]
        private float _maxArmedTime = 4f;

        [SerializeField, Min(0.01f), Tooltip("Blink period of the armed indicator (s).")]
        private float _armedBlinkInterval = 0.12f;

        [Header("Animals")]
        [SerializeField, Tooltip("Free animals near the blast flee from it.")]
        private bool _scaresAnimals = true;

        [SerializeField, Min(0f), Tooltip("Free animals within this radius of the blast flee (m).")]
        private float _animalScareRadius = 5f;

        [SerializeField, Min(0f), Tooltip("Minimum flee time of a scared animal (s).")]
        private float _animalScareDuration = 1.5f;

        [Header("VFX")]
        [SerializeField, Tooltip("Pooled blast VFX prefab (expanding sphere).")]
        private BombBlastVfx _blastVfxPrefab;

        [SerializeField, Min(0.05f), Tooltip("Duration of the blast VFX (s).")]
        private float _blastVfxDuration = 0.4f;

        public Bomb Prefab => _prefab;
        public ThrowSettings ThrowSettings => _throwSettings;
        public float CarrySpeedMultiplier => _carrySpeedMultiplier;
        public Vector3 HoldOffset => _holdOffset;
        public float GrabCooldown => _grabCooldown;
        public float BlastRadius => _blastRadius;
        public float StunDuration => _stunDuration;
        public float KnockbackSpeed => _knockbackSpeed;
        public float MaxArmedTime => _maxArmedTime;
        public float ArmedBlinkInterval => _armedBlinkInterval;
        public bool ScaresAnimals => _scaresAnimals;
        public float AnimalScareRadius => _animalScareRadius;
        public float AnimalScareDuration => _animalScareDuration;
        public BombBlastVfx BlastVfxPrefab => _blastVfxPrefab;
        public float BlastVfxDuration => _blastVfxDuration;

        private void OnValidate()
        {
            if (_prefab == null)
            {
                Debug.LogWarning($"[{nameof(BombData)}] '{name}' has no bomb prefab.", this);
            }

            if (_blastRadius <= 0f)
            {
                Debug.LogWarning($"[{nameof(BombData)}] '{name}' has a blast radius of 0; the bomb will never stun anyone.", this);
            }

            if (_blastVfxPrefab == null)
            {
                Debug.LogWarning($"[{nameof(BombData)}] '{name}' has no blast VFX prefab; explosions will be invisible.", this);
            }
        }
    }
}
