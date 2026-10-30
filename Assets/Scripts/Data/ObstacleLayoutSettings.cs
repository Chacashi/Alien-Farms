using System.Collections.Generic;
using UnityEngine;

namespace AlienFarms.Data
{
    /// <summary>Rules for the random obstacle layout of the Animal Zone (rolled every new round).</summary>
    [CreateAssetMenu(fileName = "ObstacleLayoutSettings", menuName = "Alien Farms/Obstacles/Obstacle Layout Settings")]
    public sealed class ObstacleLayoutSettings : ScriptableObject
    {
        /// <summary>Variant table entries map to bits of a 32-bit mask.</summary>
        public const int MaxVariants = 32;

        [Header("Count")]
        [SerializeField, Min(0), Tooltip("Minimum obstacles per round (a mirror pair counts as 2). May not be reached if slots are blocked.")]
        private int _minActive = 4;

        [SerializeField, Min(0), Tooltip("Maximum obstacles per round (a mirror pair counts as 2). Must be >= min.")]
        private int _maxActive = 8;

        [Header("Variants")]
        [SerializeField, Tooltip("Variant table with relative weights (max 32 entries). Slots only use the variants listed here.")]
        private ObstacleWeightEntry[] _variants = new ObstacleWeightEntry[0];

        [Header("Placement")]
        [SerializeField, Min(0f), Tooltip("Minimum planar distance between two active obstacles (m).")]
        private float _minSpacing = 5.5f;

        [SerializeField, Tooltip("Mirror pairs (x -> -x) are placed together with the same variant and mirrored yaw. "
            + "Off: every slot is picked independently.")]
        private bool _mirrorAcrossCenter = true;

        [SerializeField, Min(0f), Tooltip("Height of the clearance sphere center above each slot (m); a player inside it blocks the slot.")]
        private float _clearanceProbeHeight = 1f;

        [SerializeField, Min(0f), Tooltip("Slots closer than this to the Animal Zone edge are reported in the editor (m). Validation only.")]
        private float _minEdgeDistance = 4f;

        [Header("Random")]
        [SerializeField, Tooltip("Use the fixed seed below: the SEQUENCE of layouts is reproducible (each round still differs).")]
        private bool _useFixedSeed;

        [SerializeField, Tooltip("Seed used when 'Use Fixed Seed' is on.")]
        private int _seed = 12345;

        public int MinActive => _minActive;
        public int MaxActive => _maxActive;
        public IReadOnlyList<ObstacleWeightEntry> Variants => _variants;
        public float MinSpacing => _minSpacing;
        public bool MirrorAcrossCenter => _mirrorAcrossCenter;
        public float ClearanceProbeHeight => _clearanceProbeHeight;
        public float MinEdgeDistance => _minEdgeDistance;
        public bool UseFixedSeed => _useFixedSeed;
        public int Seed => _seed;

        /// <summary>Index of <paramref name="variant"/> in the variant table, or -1.</summary>
        public int IndexOf(ObstacleVariantData variant)
        {
            if (variant == null || _variants == null)
            {
                return -1;
            }

            int count = Mathf.Min(_variants.Length, MaxVariants);
            for (int i = 0; i < count; i++)
            {
                if (_variants[i].Variant == variant)
                {
                    return i;
                }
            }

            return -1;
        }

        private void OnValidate()
        {
            if (_maxActive < _minActive)
            {
                _maxActive = _minActive;
            }

            if (_variants == null)
            {
                return;
            }

            if (_variants.Length > MaxVariants)
            {
                Debug.LogWarning($"[{nameof(ObstacleLayoutSettings)}] '{name}' has {_variants.Length} variants; only the first {MaxVariants} are used.", this);
            }

            for (int i = 0; i < _variants.Length; i++)
            {
                ObstacleVariantData variant = _variants[i].Variant;
                if (variant == null)
                {
                    Debug.LogWarning($"[{nameof(ObstacleLayoutSettings)}] '{name}' variant entry {i} is empty.", this);
                    continue;
                }

                for (int j = 0; j < i; j++)
                {
                    if (_variants[j].Variant == variant)
                    {
                        Debug.LogWarning($"[{nameof(ObstacleLayoutSettings)}] '{name}' lists '{variant.name}' twice; only the first entry is used.", this);
                        break;
                    }
                }
            }
        }
    }
}
