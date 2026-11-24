using AlienFarms.Core;
using AlienFarms.Player;
using AlienFarms.UI;
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
        private const int NoPendingRoute = -1;

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

        [SerializeField, Tooltip("Main pause panel (buttons) under the pause root; hidden while a sub-menu is open.")]
        private GameObject _mainContent;

        private bool _isPaused;
        private PlayerId _owner = PlayerId.None;
        private SubMenuPanel _activeSubMenu;
        private GameObject _pendingRoot;
        private GameObject _pendingSelection;
        private int _pendingRouteFrame = NoPendingRoute;
        private bool _freezesTime = true;

        // Last phase announced by GameEvents: an online client mirrors the server's match (its MatchManager is disabled).
        private MatchPhase _announcedPhase = MatchPhase.None;

        public bool IsPaused => _isPaused;

        /// <summary>True (default) when pausing sets <see cref="Time.timeScale"/> to 0 and pauses audio.</summary>
        public bool FreezesTime => _freezesTime;

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
            GameEvents.OnMatchPhaseChanged += HandleMatchPhaseChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnPauseRequested -= HandlePauseRequested;
            GameEvents.OnMenuCancelPressed -= HandleMenuCancelPressed;
            GameEvents.OnPlayerLeft -= HandlePlayerLeft;
            GameEvents.OnPlayerDeviceLost -= HandlePlayerDeviceLost;
            GameEvents.OnPlayerDeviceRegained -= HandlePlayerDeviceRegained;
            GameEvents.OnMatchPhaseChanged -= HandleMatchPhaseChanged;

            // Disabled (or torn down) while paused: give input back to whoever is still alive and restore time.
            CloseMenu();
            ResetSubMenu();
            RestoreTime();
        }

        private void Update()
        {
            // Sub-menu routing requested from a button onClick (inside the event system update) is applied here, a
            // frame later, so playerRoot/selection never change while the event system is processing.
            if (_pendingRouteFrame == NoPendingRoute || Time.frameCount < _pendingRouteFrame)
            {
                return;
            }

            GameObject root = _pendingRoot;
            GameObject selection = _pendingSelection;
            ClearPendingRoute();
            if (_isPaused && _menus != null)
            {
                _menus.RouteMenu(_owner, root, selection);
            }
        }

        public void Pause(PlayerId owner)
        {
            if (_isPaused || owner == PlayerId.None || _match == null || _menus == null || !PauseRules.CanPause(CurrentPhase))
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
            if (_freezesTime)
            {
                Time.timeScale = 0f;
                AudioListener.pause = true;
            }

            // The panel is shown here; PlayerMenuInput selects the first button once it is active.
            GameEvents.RaiseGamePaused(owner);
        }

        /// <summary>
        /// True (default): pausing freezes time and audio. False (online): the pause only opens the menu; the shared
        /// simulation keeps running. Turning it off while paused restores time right away.
        /// </summary>
        public void SetFreezesTime(bool freezesTime)
        {
            _freezesTime = freezesTime;
            if (!freezesTime && _isPaused)
            {
                RestoreTime();
            }
        }

        /// <summary>Closes the pause menu and restores time. Wired to the Resume button.</summary>
        public void Resume()
        {
            PlayerId owner = CloseMenu();
            if (owner != PlayerId.None)
            {
                GameEvents.RaisePauseMenuResumed(owner);
            }
        }

        private PlayerId CloseMenu()
        {
            if (!_isPaused)
            {
                return PlayerId.None;
            }

            PlayerId owner = _owner;
            _isPaused = false;
            _owner = PlayerId.None;

            // Null-checked: the menu also closes during teardown.
            if (_menus != null)
            {
                _menus.ReleaseMenus();
                _menus.SetPlayersPaused(false);
            }

            // The next pause opens on the main panel.
            ResetSubMenu();
            // Before the event: listeners must already see time running.
            RestoreTime();
            GameEvents.RaiseGameResumed(owner);
            return owner;
        }

        /// <summary>Resumes and restarts the round from the countdown. Wired to the Restart button.</summary>
        public void RestartMatch()
        {
            if (!_isPaused)
            {
                return;
            }

            CloseMenu();
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

            CloseMenu();
            if (_exit != null)
            {
                _exit.ReturnToTitle();
            }
        }

        /// <summary>Pause sub-menu buttons (Options, How to play): opens <paramref name="panel"/> over the main panel.</summary>
        public void OpenSubMenu(SubMenuPanel panel)
        {
            if (!_isPaused || panel == null || panel == _activeSubMenu)
            {
                return;
            }

            if (_activeSubMenu != null)
            {
                _activeSubMenu.Hide();
            }

            _activeSubMenu = panel;
            SetActive(_mainContent, false);
            panel.Show();
            RequestRoute(panel.MenuRoot, panel.FirstSelected);
        }

        /// <summary>Sub-menu 'Back' button (also the owner's Cancel): back to the main pause panel.</summary>
        public void CloseSubMenu()
        {
            if (_activeSubMenu == null)
            {
                return;
            }

            GameObject returnSelection = _activeSubMenu.ReturnSelection;
            _activeSubMenu.Hide();
            _activeSubMenu = null;
            SetActive(_mainContent, true);
            if (_isPaused)
            {
                RequestRoute(_menuRoot, returnSelection != null ? returnSelection : _firstSelected);
            }
        }

        private void HandlePauseRequested(PlayerId playerId) => Pause(playerId);

        private void HandleMatchPhaseChanged(MatchPhase previous, MatchPhase current) => _announcedPhase = current;

        /// <summary>The running match's phase; the announced one when the match does not run here (online client).</summary>
        private MatchPhase CurrentPhase =>
            _match != null && _match.isActiveAndEnabled && _match.CurrentPhase != MatchPhase.None ? _match.CurrentPhase : _announcedPhase;

        private void HandleMenuCancelPressed(PlayerId playerId)
        {
            if (!_isPaused || playerId != _owner)
            {
                return;
            }

            // Cancel inside a sub-menu goes back one level instead of resuming.
            if (_activeSubMenu != null)
            {
                CloseSubMenu();
            }
            else
            {
                Resume();
            }
        }

        private void HandlePlayerLeft(PlayerId playerId) => CloseMenu();

        /// <summary>The owner lost its device: hand the menu to the other player so the game is never stuck.</summary>
        private void HandlePlayerDeviceLost(PlayerId playerId)
        {
            if (!_isPaused || playerId != _owner || _menus == null)
            {
                return;
            }

            PlayerId other = playerId.Other();
            GetCurrentRoute(out GameObject root, out GameObject selection);
            if (_menus.RouteMenu(other, root, selection))
            {
                _owner = other;
            }
        }

        /// <summary>Re-routes to a player who regained its device while nobody else could take the menu.</summary>
        private void HandlePlayerDeviceRegained(PlayerId playerId)
        {
            if (_isPaused && playerId == _owner && _menus != null)
            {
                GetCurrentRoute(out GameObject root, out GameObject selection);
                _menus.RouteMenu(playerId, root, selection);
            }
        }

        /// <summary>Menu root and focus of whatever is showing now (open sub-menu or main pause panel).</summary>
        private void GetCurrentRoute(out GameObject root, out GameObject selection)
        {
            if (_activeSubMenu != null)
            {
                root = _activeSubMenu.MenuRoot;
                selection = _activeSubMenu.FirstSelected;
            }
            else
            {
                root = _menuRoot;
                selection = _firstSelected;
            }
        }

        private void RequestRoute(GameObject root, GameObject selection)
        {
            _pendingRoot = root;
            _pendingSelection = selection;
            _pendingRouteFrame = Time.frameCount + 1;
        }

        private void ClearPendingRoute()
        {
            _pendingRoot = null;
            _pendingSelection = null;
            _pendingRouteFrame = NoPendingRoute;
        }

        /// <summary>Closes any sub-menu without routing and restores the main panel (on resume or teardown).</summary>
        private void ResetSubMenu()
        {
            ClearPendingRoute();
            if (_activeSubMenu != null)
            {
                _activeSubMenu.Hide();
                _activeSubMenu = null;
            }

            SetActive(_mainContent, true);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
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

            if (_mainContent == null)
            {
                Debug.LogWarning($"[{nameof(PauseController)}] Missing main pause panel (_mainContent) on '{name}'; sub-menus cannot hide it.", this);
            }
            else if (_menuRoot != null && (_mainContent == _menuRoot || !_mainContent.transform.IsChildOf(_menuRoot.transform)))
            {
                Debug.LogWarning($"[{nameof(PauseController)}] Main pause panel on '{name}' must be a child of the pause menu root.", this);
            }
        }
    }
}
