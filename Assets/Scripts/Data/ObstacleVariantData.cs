using System.Collections.Generic;
using UnityEngine;

namespace AlienFarms.Data
{
    /// <summary>One obstacle type (fence, UFO disc...) and the yaws it may be placed with.</summary>
    [CreateAssetMenu(fileName = "ObstacleVariant", menuName = "Alien Farms/Obstacles/Obstacle Variant")]
    public sealed class ObstacleVariantData : ScriptableObject
    {
        private const float FullTurn = 360f;
        private const float AngleTolerance = 0.01f;

        [Header("Placement")]
        [SerializeField, Tooltip("Yaws (degrees, around Y) this obstacle may be placed with; one is picked at random. "
            + "The mirrored slot uses -yaw, so the set should be symmetric (see Rotational Symmetry).")]
        private float[] _allowedYaws = { 0f };

        [SerializeField, Min(1), Tooltip("How many times the shape repeats in a full turn (1 = none, 2 = looks the same at 180 deg, "
            + "4 = every 90 deg). Only used to validate that mirrored yaws (-yaw) stay in the allowed set.")]
        private int _rotationalSymmetry = 1;

        [SerializeField, Tooltip("The shape equals its mirror image (true for fences, discs, engines). Off for chiral shapes "
            + "(e.g. scrap piles): they are only placed on single slots, never on mirror pairs.")]
        private bool _isMirrorSymmetric = true;

        public IReadOnlyList<float> AllowedYaws => _allowedYaws;
        public int RotationalSymmetry => _rotationalSymmetry;
        public bool IsMirrorSymmetric => _isMirrorSymmetric;

        private void OnValidate()
        {
            if (_allowedYaws == null || _allowedYaws.Length == 0)
            {
                Debug.LogWarning($"[{nameof(ObstacleVariantData)}] '{name}' has no allowed yaws; 0 will be used.", this);
                return;
            }

            for (int i = 0; i < _allowedYaws.Length; i++)
            {
                _allowedYaws[i] = Mathf.Repeat(_allowedYaws[i], FullTurn);
            }

            float period = FullTurn / Mathf.Max(1, _rotationalSymmetry);
            for (int i = 0; i < _allowedYaws.Length; i++)
            {
                float mirrored = Mathf.Repeat(-_allowedYaws[i], period);
                if (!ContainsModulo(mirrored, period))
                {
                    Debug.LogWarning(
                        $"[{nameof(ObstacleVariantData)}] '{name}': mirrored yaw {Mathf.Repeat(-_allowedYaws[i], FullTurn)} of {_allowedYaws[i]} "
                        + "is not in the allowed set; mirrored slots will use it anyway.", this);
                }
            }
        }

        private bool ContainsModulo(float angle, float period)
        {
            for (int i = 0; i < _allowedYaws.Length; i++)
            {
                float delta = Mathf.Abs(Mathf.Repeat(_allowedYaws[i], period) - angle);
                if (delta < AngleTolerance || Mathf.Abs(delta - period) < AngleTolerance)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
