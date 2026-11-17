using System;
using UnityEngine;

namespace AlienFarms.Networking.Persistence
{
    /// <summary>
    /// JsonUtility shape of the local pending stats (<see cref="PendingStatsCodec"/>): the head at the top level (what a
    /// v1 entry holds), plus <c>sent</c> and the <c>tail</c> segment (v2). Field names are the JSON names, so they
    /// deliberately break the <c>_camelCase</c> convention.
    /// </summary>
    [Serializable]
    internal sealed class PendingStatsJson
    {
#pragma warning disable IDE1006 // Naming: the JSON names.
        [SerializeField] private int v;
        [SerializeField] private string batch;
        [SerializeField] private int matches;
        [SerializeField] private int wins;
        [SerializeField] private int draws;
        [SerializeField] private int losses;
        [SerializeField] private int animals;
        [SerializeField] private int tackles;
        [SerializeField] private bool sent;
        [SerializeField] private PendingSegmentJson tail = new PendingSegmentJson();
#pragma warning restore IDE1006

        public int Version { get => v; set => v = value; }
        public string Batch { get => batch; set => batch = value; }
        public int Matches { get => matches; set => matches = value; }
        public int Wins { get => wins; set => wins = value; }
        public int Draws { get => draws; set => draws = value; }
        public int Losses { get => losses; set => losses = value; }
        public int Animals { get => animals; set => animals = value; }
        public int Tackles { get => tackles; set => tackles = value; }
        public bool Sent { get => sent; set => sent = value; }
        public PendingSegmentJson Tail { get => tail; set => tail = value; }
    }
}
