using AlienFarms.Data;
using UnityEngine;

namespace AlienFarms.Match
{
    /// <summary>
    /// Exit options of a finished (or paused) match, wired to menu buttons: rematch or back to the title screen.
    /// What they do is delegated to an <see cref="IMatchExitHandler"/>: the local one by default, or one set with
    /// <see cref="SetHandler"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchExit : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Match whose results this exits from.")]
        private MatchManager _match;

        [SerializeField, Tooltip("Session picked in the MainMenu; cleared when returning to the title screen.")]
        private MatchSession _session;

        private IMatchExitHandler _handler;
        private bool _isLeaving;

        /// <summary>Replaces what the exit buttons do (e.g. the online handler). Null restores the local one.</summary>
        public void SetHandler(IMatchExitHandler handler)
        {
            _handler = handler;
        }

        /// <summary>Starts a new round with the same players (ignored until the results input delay has passed).</summary>
        public void Rematch()
        {
            if (!_isLeaving)
            {
                Handler.RequestRematch();
            }
        }

        /// <summary>Leaves for the title screen (locally: clears the session and loads the main menu).</summary>
        public void ReturnToTitle()
        {
            if (_isLeaving)
            {
                return;
            }

            _isLeaving = Handler.ReturnToTitle();
        }

        // Created on first use (no allocation unless an exit button is pressed).
        private IMatchExitHandler Handler => _handler ??= new LocalMatchExitHandler(_match, _session);

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
