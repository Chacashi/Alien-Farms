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

        /// <summary>Starts a new round with the same players (ignored until the results input delay has passed).</summary>
        public void Rematch()
        {
            if (_match != null)
            {
                _match.RequestRematch();
            }
        }

        /// <summary>Back to the title screen. Not available until the MainMenu scene and match session exist (Hito 8B).</summary>
        public void ReturnToTitle()
        {
            // TODO 8B: clear the MatchSession and call SceneFlow.LoadMainMenu(). Buttons stay non-interactable meanwhile.
            Debug.LogWarning($"[{nameof(MatchExit)}] Return to title is not available yet (Hito 8B).", this);
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
        }
    }
}
