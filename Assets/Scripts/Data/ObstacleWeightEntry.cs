using System;
using UnityEngine;

namespace AlienFarms.Data
{
    /// <summary>One weighted obstacle variant in <see cref="ObstacleLayoutSettings"/>.</summary>
    [Serializable]
    public struct ObstacleWeightEntry
    {
        [SerializeField, Tooltip("Obstacle variant this entry refers to.")]
        private ObstacleVariantData _variant;

        [SerializeField, Min(0), Tooltip("Relative pick probability (0 = never placed).")]
        private int _weight;

        public ObstacleVariantData Variant => _variant;
        public int Weight => _weight;
    }
}
