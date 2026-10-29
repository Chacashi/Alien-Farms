using System;
using UnityEngine;

namespace AlienFarms.Data
{
    /// <summary>One weighted animal type in <see cref="AnimalSpawnSettings"/>.</summary>
    [Serializable]
    public struct AnimalSpawnEntry
    {
        [SerializeField, Tooltip("Animal type to spawn.")]
        private AnimalData _data;

        [SerializeField, Min(1), Tooltip("Relative spawn probability.")]
        private int _weight;

        public AnimalData Data => _data;
        public int Weight => _weight;
    }
}
