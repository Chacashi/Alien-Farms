using AlienFarms.Core;
using AlienFarms.Player;
using UnityEngine;

namespace AlienFarms.Match
{
    /// <summary>
    /// Game pause: freezes time (<see cref="Time.timeScale"/> = 0) and audio, and routes the pause menu to the player who
    /// requested it; the other player is blocked. Only the pausing player navigates. Methods are wired to menu buttons.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Match whose phase gates pausing and that restarts on 'Restart'.")]
        private MatchManager _match;

        [SerializeField, Tooltip("Routes the pause menu to its owner and blocks the other player.")]
        private PlayerMenuRouter _menus;

        [SerializeField, Tooltip("Exit options (return to title).")]
        private MatchExit _exit;

        [SerializeField, Tooltip("Pause panel root the owner's event system is scoped to (PausePanelRoot).")]
        private GameObject _menuRoot;

        [SerializeField, Tooltip("Button focused when the pause menu opens (BTN_Resume).")]
        private GameObject _firstSelected;

        private bool _isPaused;
        private PlayerId _owner = PlayerId.None;

        public bool IsPaused => _isPaused;

        /// <summary>Player who opened the pause menu (None while not paused).</summary>
        public PlayerId Owner => _owner;

        private void Awake()
        {
            // Safety: never start a scene frozen (e.g. a previous scene unloaded while paused).
            RestoreTime();
        }

        private void OnEnable()
        {
            GameEvents.OnPauseRequested += HandlePauseRequested;
            GameEvents.OnMenuCancelPressed += HandleMenuCancelPressed;
            GameEvents.OnPlayerLeft += HandlePlayerLeft;
            GameEvents.OnPlayerDeviceLost += HandlePlayerDeviceLost;
            GameEvents.OnPlayerDeviceRegained += HandlePlayerDeviceRegained;
        }

        private void OnDisable()
        {
            GameEvents.OnPauseRequested -= HandlePauseRequested;
            GameEvents.OnMenuCancelPressed -= HandleMenuCancelPressed;
            GameEvents.OnPlayerLeft -= HandlePlayerLeft;
            GameEvents.OnPlayerDeviceLost -= HandlePlayerDeviceLost;
            GameEvents.OnPlayerDeviceRegained -= HandlePlayerDeviceRegained;

            // Disabled (or torn down) while paused: give input back to whoever is still alive and restore time.
            Resume();
            RestoreTime();
        }

        public void Pause(PlayerId owner)
        {
            if (_isPaused || owner == PlayerId.None || _match == null || _menus == null || !PauseRules.CanPause(_match.CurrentPhase))
            {
                return;
            }

            // Pause the rigs before the map switch so the Grab 'canceled' it causes is ignored.
            _menus.SetPlayersPaused(true);
            if (!_menus.RouteMenu(owner, _menuRoot, _firstSelected))
            {
                // Nobody could navigate the menu: do not freeze the game.
                _menus.SetPlayersPaused(false);
                return;
            }

            _isPaused = true;
            _owner = owner;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            // The panel is shown here; PlayerMenuInput selects the first button once it is active.
            GameEvents.RaiseGamePaused(owner);
        }

        /// <summary>Closes the pause menu and restores time. Wired to the Resume button.</summary>
        public void Resume()
        {
            if (!_isPaused)
            {
                return;
            }

            PlayerId owner = _owner;
            _isPaused = false;
            _owner = PlayerId.None;

            // Null-checked: Resume also runs from OnDisable during teardown.
            if (_menus != null)
            {
                _menus.ReleaseMenus();
                _menus.SetPlayersPaused(false);
            }

            GameEvents.RaiseGameResumed(owner);
            RestoreTime();
        }

        /// <summary>Resumes and restarts the round from the countdown. Wired to the Restart button.</summary>
        public void RestartMatch()
        {
            if (!_isPaused)
            {
                return;
            }

            Resume();
            if (_match != null)
            {
                _match.RestartRound();
            }
        }

        /// <summary>Resumes and goes back to the title screen. Wired to the Return To Title button.</summary>
        public void ReturnToTitle()
        {
            if (!_isPaused)
            {
                return;
            }

            Resume();
            if (_exit != null)
            {
                _exit.ReturnToTitle();
            }
        }

        private void HandlePauseRequested(PlayerId playerId) => Pause(playerId);

        private void HandleMenuCancelPressed(PlayerId playerId)
        {
            if (_isPaused && playerId == _owner)
            {
                Resume();
            }
        }

        private void HandlePlayerLeft(PlayerId playerId)
        {
            if (_isPaused)
            {
                Resume();
            }
        }

        /// <summary>The owner lost its device: hand the menu to the other player so the game is never stuck.</summary>
        private void HandlePlayerDeviceLost(PlayerId playerId)
        {
            if (!_isPaused || playerId != _owner || _menus == null)
            {
                return;
            }

            PlayerId other = playerId.Other();
            if (_menus.RouteMenu(other, _menuRoot, _firstSelected))
            {
                _owner = other;
            }
        }

        /// <summary>Re-routes to a player who regained its device while nobody else could take the menu.</summary>
        private void HandlePlayerDeviceRegained(PlayerId playerId)
        {
            if (_isPaused && playerId == _owner && _menus != null)
            {
                _menus.RouteMenu(playerId, _menuRoot, _firstSelected);
            }
        }

        private static void RestoreTime()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        private void OnValidate()
        {
            if (_match == null)
            {
                TryGetComponent(out _match);
            }

            if (_exit == null)
            {
                TryGetComponent(out _exit);
            }

            if (_match == null || _menus == null)
            {
                Debug.LogWarning($"[{nameof(PauseController)}] Missing MatchManager or PlayerMenuRouter on '{name}'.", this);
            }

            if (_menuRoot == null || _firstSelected == null)
            {
                Debug.LogWarning($"[{nameof(PauseController)}] Missing pause menu root or first selected button on '{name}'.", this);
            }
            else if (!_firstSelected.transform.IsChildOf(_menuRoot.transform))
            {
                Debug.LogWarning($"[{nameof(PauseController)}] First selected button on '{name}' is not under the pause menu root.", this);
            }
        }
    }
}
