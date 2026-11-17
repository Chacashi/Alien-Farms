using System;
using UnityEngine;

namespace AlienFarms.Networking.Persistence
{
    /// <summary>
    /// JsonUtility shape of the tail segment of the local pending stats (<see cref="PendingStatsCodec"/>, v2). Field names
    /// are the JSON names, so they deliberately break the <c>_camelCase</c> convention.
    /// </summary>
    [Serializable]
    internal sealed class PendingSegmentJson
    {
#pragma warning disable IDE1006 // Naming: the JSON names.
        [SerializeField] private string batch;
        [SerializeField] private int matches;
        [SerializeField] private int wins;
        [SerializeField] private int draws;
        [SerializeField] private int losses;
        [SerializeField] private int animals;
        [SerializeField] private int tackles;
#pragma warning restore IDE1006

        public string Batch { get => batch; set => batch = value; }
        public int Matches { get => matches; set => matches = value; }
        public int Wins { get => wins; set => wins = value; }
        public int Draws { get => draws; set => draws = value; }
        public int Losses { get => losses; set => losses = value; }
        public int Animals { get => animals; set => animals = value; }
        public int Tackles { get => tackles; set => tackles = value; }
    }
}
