using AlienFarms.Core;
using AlienFarms.Data;
using UnityEngine;

namespace AlienFarms.Match
{
    /// <summary>Exit options of a finished (or paused) match, wired to menu buttons: rematch or back to the title screen.</summary>
    [DisallowMultipleComponent]
    public sealed class MatchExit : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Match whose results this exits from.")]
        private MatchManager _match;

        [SerializeField, Tooltip("Session picked in the MainMenu; cleared when returning to the title screen.")]
        private MatchSession _session;

        private bool _isLeaving;

        /// <summary>Starts a new round with the same players (ignored until the results input delay has passed).</summary>
        public void Rematch()
        {
            if (_match != null && !_isLeaving)
            {
                _match.RequestRematch();
            }
        }

        /// <summary>Clears the session and loads the title screen (time and audio are restored by <see cref="SceneFlow"/>).</summary>
        public void ReturnToTitle()
        {
            if (_isLeaving)
            {
                return;
            }

            if (_session != null)
            {
                _session.Clear();
            }

            _isLeaving = SceneFlow.LoadMainMenu();
        }

        private void OnValidate()
        {
            if (_match == null)
            {
                TryGetComponent(out _match);
            }

            if (_match == null)
            {
                Debug.LogWarning($"[{nameof(MatchExit)}] Missing MatchManager on '{name}'.", this);
            }

            if (_session == null)
            {
                Debug.LogWarning($"[{nameof(MatchExit)}] Missing MatchSession on '{name}'.", this);
            }
        }
    }
}
