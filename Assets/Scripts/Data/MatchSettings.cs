using UnityEngine;

namespace AlienFarms.Data
{
    /// <summary>Timing rules of a match round.</summary>
    [CreateAssetMenu(fileName = "MatchSettings", menuName = "Alien Farms/Match/Match Settings")]
    public sealed class MatchSettings : ScriptableObject
    {
        [Header("Timing")]
        [SerializeField, Min(5f), Tooltip("Round duration (s).")]
        private float _matchDuration = 300f;

        [SerializeField, Min(0), Tooltip("Whole seconds of the pre-round countdown (0 = start immediately).")]
        private int _countdownSeconds = 3;

        [SerializeField, Min(0f), Tooltip("How long the 'GO' message stays on screen after the countdown (s).")]
        private float _goMessageDuration = 1f;

        [SerializeField, Min(0f), Tooltip("Confirm is ignored this long after results appear, so a held button does not restart instantly (s).")]
        private float _resultsInputDelay = 1.5f;

        public float MatchDuration => _matchDuration;
        public int CountdownSeconds => _countdownSeconds;
        public float GoMessageDuration => _goMessageDuration;
        public float ResultsInputDelay => _resultsInputDelay;
    }
}
