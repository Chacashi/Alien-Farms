using System;
using UnityEngine;

namespace AlienFarms.Editor
{
    /// <summary>
    /// Authoring parameters for <see cref="GrassMeshBuilder"/>. Serialized by <see cref="GrassFieldBaker"/>.
    /// </summary>
    [Serializable]
    public sealed class GrassBakeSettings
    {
        public const int MinSegments = 1;
        public const int MaxSegments = 3;

        [Header("Output")]
        [Tooltip("Output folder: Assets/Art/Models/Generated/GrassField_<Name>/ (one mesh asset per chunk)")]
        [SerializeField] private string _fieldName = "Arena";

        [Tooltip("Chunk edge length in meters. One mesh per chunk so each can be frustum culled.")]
        [Min(2f)]
        [SerializeField] private float _chunkSize = 10f;

        [Header("Area")]
        [Tooltip("Used when no reference object is set. World-space bounds; grass is placed on the top face (max.y).")]
        [SerializeField] private Bounds _manualArea = new Bounds(Vector3.zero, new Vector3(20f, 0f, 20f));

        [Tooltip("Meters kept free of grass along the area border.")]
        [Min(0f)]
        [SerializeField] private float _edgeMargin = 0.25f;

        [Header("Density")]
        [Tooltip("Clumps per square meter. Each clump holds several blades.")]
        [Range(0.1f, 30f)]
        [SerializeField] private float _clumpsPerSquareMeter = 3f;

        [Range(1, 12)]
        [SerializeField] private int _minBladesPerClump = 3;

        [Range(1, 12)]
        [SerializeField] private int _maxBladesPerClump = 5;

        [Tooltip("Radius in meters in which the blades of a clump are scattered.")]
        [Range(0f, 1f)]
        [SerializeField] private float _clumpRadius = 0.12f;

        [Tooltip("0 = uniform carpet, 1 = only a few tufts. Driven by Perlin noise.")]
        [Range(0f, 1f)]
        [SerializeField] private float _patchiness = 0.35f;

        [Tooltip("Frequency of the patchiness noise (1 per meter).")]
        [Range(0.01f, 1f)]
        [SerializeField] private float _patchScale = 0.15f;

        [Header("Blade Shape")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _bladeHeight = 0.4f;

        [Range(0f, 0.9f)]
        [SerializeField] private float _heightVariation = 0.35f;

        [Range(0.01f, 0.5f)]
        [SerializeField] private float _bladeWidth = 0.07f;

        [Range(0f, 0.9f)]
        [SerializeField] private float _widthVariation = 0.3f;

        [Tooltip("1 = 1 triangle, 2 = 3 triangles, 3 = 5 triangles per blade.")]
        [Range(MinSegments, MaxSegments)]
        [SerializeField] private int _segmentsPerBlade = 2;

        [Tooltip("Static forward lean of the tip, as a fraction of the blade height.")]
        [Range(0f, 1f)]
        [SerializeField] private float _maxLean = 0.3f;

        [Tooltip("Extra bounds padding (fraction of blade height) so wind-bent tips are not culled.")]
        [Range(0f, 2f)]
        [SerializeField] private float _windBoundsPadding = 0.6f;

        [Header("Placement")]
        [Tooltip("Deterministic seed: same seed and settings always produce the same field.")]
        [SerializeField] private int _seed = 1234;

        [Tooltip("Raycast down to snap each clump onto colliders in the Ground Mask.")]
        [SerializeField] private bool _snapToGround;

        [SerializeField] private LayerMask _groundMask;

        [Tooltip("Clumps overlapping colliders on these layers are skipped (fences, barns, props...).")]
        [SerializeField] private LayerMask _exclusionMask;

        [Min(0f)]
        [SerializeField] private float _exclusionRadius = 0.3f;

        [Tooltip("Also treat trigger colliders (e.g. zone volumes) as exclusion / ground hits.")]
        [SerializeField] private bool _includeTriggers;

        public string FieldName => _fieldName;
        public float ChunkSize => _chunkSize;
        public Bounds ManualArea => _manualArea;
        public float EdgeMargin => _edgeMargin;
        public float ClumpsPerSquareMeter => _clumpsPerSquareMeter;
        public int MinBladesPerClump => Mathf.Min(_minBladesPerClump, _maxBladesPerClump);
        public int MaxBladesPerClump => Mathf.Max(_minBladesPerClump, _maxBladesPerClump);
        public float ClumpRadius => _clumpRadius;
        public float Patchiness => _patchiness;
        public float PatchScale => _patchScale;
        public float BladeHeight => _bladeHeight;
        public float HeightVariation => _heightVariation;
        public float BladeWidth => _bladeWidth;
        public float WidthVariation => _widthVariation;
        public int SegmentsPerBlade => Mathf.Clamp(_segmentsPerBlade, MinSegments, MaxSegments);
        public float MaxLean => _maxLean;
        public float WindBoundsPadding => _windBoundsPadding;
        public int Seed => _seed;
        public bool SnapToGround => _snapToGround;
        public LayerMask GroundMask => _groundMask;
        public LayerMask ExclusionMask => _exclusionMask;
        public float ExclusionRadius => _exclusionRadius;
        public bool IncludeTriggers => _includeTriggers;

        public int VerticesPerBlade => SegmentsPerBlade * 2 + 1;
        public int TrianglesPerBlade => SegmentsPerBlade * 2 - 1;
    }
}
