using System;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Input;
using AlienFarms.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AlienFarms.Menu
{
    /// <summary>
    /// MainMenu composition root: Title or SubMenu -> CharacterSelect -> Loading. On the title the first joined player owns the
    /// buttons (the other is blocked); on character select every player moves its own cursor, confirms (locks) and
    /// cancels. When both have confirmed, their devices, scheme and character go to the <see cref="MatchSession"/> and the
    /// gameplay scene is loaded. Input state changes are always applied outside input callbacks (next frame on join,
    /// or from PlayerMenuInput's Update dispatch). Title sub-menus (<see cref="SubMenuPanel"/>) are navigated by the
    /// title owner; Cancel or the panel's Back button returns to the title. Online play hands the title owner to the
    /// <see cref="OnlineMenuController"/> (Online phase); there the match scene is loaded by the network, never here.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MenuFlowController : MonoBehaviour
    {
        private const int MaxPlayers = 2;
        private const int NoPendingFrame = -1;

        [Header("Config")]
        [SerializeField, Tooltip("Selectable characters (card order does not matter; cards match by character).")]
        private CharacterRoster _roster;

        [SerializeField, Tooltip("Delay between everyone confirming and loading the gameplay scene.")]
        private MenuSettings _settings;

        [Header("References")]
        [SerializeField, Tooltip("Joined menu players.")]
        private MenuLobby _lobby;

        [SerializeField, Tooltip("Runtime session handed to the gameplay scene (MatchSession_Runtime).")]
        private MatchSession _session;

        [SerializeField] private TitleScreenView _title;

        [SerializeField, Tooltip("Title intro for attract mode, camera transitions, and the menu column.")]
        private TitleIntroDirector _intro;

        [SerializeField] private CharacterSelectView _select;

        [SerializeField, Tooltip("Overlay shown while the gameplay scene is about to load.")]
        private GameObject _loadingOverlay;

        [Header("Online")]
        [SerializeField, Tooltip("Online panel controller (--- Managers ---/OnlineMenu).")]
        private OnlineMenuController _online;

        [SerializeField, Tooltip("Runtime online session channel. In a session the MatchSession is not cleared on load and the online panel reopens (R1).")]
        private OnlineSessionChannel _onlineSession;

        [SerializeField, Tooltip("Title button focused when returning from the online panel (BTN_Play).")]
        private GameObject _playOnlineButton;

        // Index = (int)PlayerId - 1. Handlers are created once (Awake) so they can be unsubscribed.
        private readonly PlayerMenuInput[] _players = new PlayerMenuInput[MaxPlayers];
        private readonly Action<int>[] _horizontalHandlers = new Action<int>[MaxPlayers];
        private readonly Action[] _submitHandlers = new Action[MaxPlayers];
        private readonly Action[] _cancelHandlers = new Action[MaxPlayers];
        private readonly int[] _applyFrame = { NoPendingFrame, NoPendingFrame };

        private CharacterSelection _selection;
        private SubMenuPanel _activeSubMenu;
        private GameObject _titleReturnSelection;
        private MenuPhase _phase = MenuPhase.Title;
        private PlayerId _onlineOwner = PlayerId.None;
        private float _loadAt;
        private bool _isLeaving;

        public MenuPhase Phase => _phase;

        private void Awake()
        {
            // R1: a MainMenu (re)loaded while in an online session keeps the session's MatchSession (online mode).
            bool inOnlineSession = _onlineSession != null && _onlineSession.Status == OnlineSessionStatus.InSession;

            // An online match ended (left, host gone): reopen the online hub instead of the title. Read before Clear.
            bool returnToOnline = _session != null && _session.ConsumeReturnToOnlineMenu();

            // Back on the title: whatever was picked before is gone.
            if (_session != null && !inOnlineSession)
            {
                _session.Clear();
            }

            _selection = new CharacterSelection(_roster != null ? _roster.Count : 0, MaxPlayers);
            for (int i = 0; i < MaxPlayers; i++)
            {
                PlayerId id = (PlayerId)(i + 1);
                _horizontalHandlers[i] = step => HandleHorizontalStep(id, step);
                _submitHandlers[i] = () => HandleSubmitted(id);
                _cancelHandlers[i] = () => HandleCanceled(id);
            }

            SetLoadingOverlay(false);
            if (_select != null)
            {
                _select.Hide();
            }

            if (_title != null)
            {
                _title.Show();
            }

            if (OnlineMenuRules.OpensOnlineOnLoad(inOnlineSession, returnToOnline) && _online != null)
            {
                // Straight back to the room / hub; the first player to join becomes its owner.
                _phase = MenuPhase.Online;
                if (_intro != null)
                {
                    _intro.SnapToMenu();
                }

                if (_title != null)
                {
                    _title.Hide();
                }

                _online.Enter(null);
            }
            else if (_intro != null)
            {
                _intro.SnapToAttract();
            }
        }

        private void OnEnable()
        {
            if (_intro != null)
            {
                _intro.MenuReady += HandleTitleMenuReady;
            }

            if (_online != null)
            {
                _online.ExitRequested += HandleOnlineExitRequested;
            }

            if (_lobby == null)
            {
                return;
            }

            _lobby.Joined += HandleJoined;
            _lobby.Left += HandleLeft;
        }

        private void OnDisable()
        {
            if (_intro != null)
            {
                _intro.MenuReady -= HandleTitleMenuReady;
            }

            if (_online != null)
            {
                _online.ExitRequested -= HandleOnlineExitRequested;
            }

            if (_lobby != null)
            {
                _lobby.Joined -= HandleJoined;
                _lobby.Left -= HandleLeft;
            }

            for (int i = 0; i < MaxPlayers; i++)
            {
                Unbind(i);
            }
        }

        private void Update()
        {
            int frame = Time.frameCount;
            for (int i = 0; i < MaxPlayers; i++)
            {
                if (_applyFrame[i] != NoPendingFrame && frame >= _applyFrame[i])
                {
                    _applyFrame[i] = NoPendingFrame;
                    ApplyPlayerState((PlayerId)(i + 1));
                }
            }

            if (_phase == MenuPhase.Online)
            {
                // The network loads the match scene: only block the menu while it does (back to normal if it fails).
                _isLeaving = _online != null && _online.IsLoadingMatch;
            }

            if (_phase == MenuPhase.Loading && !_isLeaving && Time.unscaledTime >= _loadAt)
            {
                _isLeaving = SceneFlow.LoadGameplay();
                if (!_isLeaving)
                {
                    CancelLoading();
                }
            }
        }

        /// <summary>Opens character select from the title or a sub-menu with everyone joined so far.</summary>
        public void Play()
        {
            if (_phase == MenuPhase.Title && _intro != null && !_intro.IsMenuInteractable)
            {
                return;
            }

            if ((_phase != MenuPhase.Title && _phase != MenuPhase.SubMenu) || _isLeaving)
            {
                return;
            }

            HideActiveSubMenu();
            _phase = MenuPhase.CharacterSelect;
            for (int i = 0; i < MaxPlayers; i++)
            {
                PlayerId id = (PlayerId)(i + 1);
                if (_players[i] != null)
                {
                    _selection.Join(id);
                }
            }

            if (_title != null)
            {
                _title.Hide();
            }

            if (_select != null)
            {
                _select.Show();
            }

            // Called from a button onClick (inside an event system update): input changes go to the next frame.
            RequestApplyAll();
            Render();
        }

        /// <summary>Opens the online panel from the title or a sub-menu for the title owner.</summary>
        public void PlayOnline()
        {
            if (_phase == MenuPhase.Title && _intro != null && !_intro.IsMenuInteractable)
            {
                return;
            }

            PlayerId owner = GetTitleOwner();
            if ((_phase != MenuPhase.Title && _phase != MenuPhase.SubMenu) || _isLeaving || _online == null || owner == PlayerId.None)
            {
                return;
            }

            HideActiveSubMenu();
            _phase = MenuPhase.Online;
            _onlineOwner = owner;
            if (_title != null)
            {
                _title.Hide();
            }

            if (_lobby != null)
            {
                _lobby.SetJoiningEnabled(false);
            }

            _online.Enter(_players[(int)owner - 1]);
            // Called from a button onClick: input routing goes to the next frame.
            RequestApplyAll();
        }

        /// <summary>Title sub-menu buttons (Options, How to play): opens <paramref name="panel"/> for the title owner.</summary>
        public void OpenSubMenu(SubMenuPanel panel)
        {
            if (_phase == MenuPhase.Title && _intro != null && !_intro.IsMenuInteractable)
            {
                return;
            }

            if (panel == null || _phase != MenuPhase.Title || _isLeaving)
            {
                return;
            }

            _phase = MenuPhase.SubMenu;
            _activeSubMenu = panel;
            if (_title != null)
            {
                _title.Hide();
            }

            panel.Show();
            // Called from a button onClick: input routing goes to the next frame.
            RequestApplyAll();
        }

        /// <summary>Sub-menu 'Back' button (also the owner's Cancel): back to the title buttons.</summary>
        public void CloseSubMenu()
        {
            if (_phase != MenuPhase.SubMenu || _isLeaving)
            {
                return;
            }

            if (_activeSubMenu != null)
            {
                _titleReturnSelection = _activeSubMenu.ReturnSelection;
            }

            HideActiveSubMenu();
            _phase = MenuPhase.Title;
            if (_title != null)
            {
                _title.Show();
            }

            RefreshTitleIntro();
            RequestApplyAll();
        }

        private void HandleTitleMenuReady()
        {
            if (_phase == MenuPhase.Title && !_isLeaving)
            {
                RequestApplyAll();
            }
        }

        private void RefreshTitleIntro()
        {
            if (_intro == null)
            {
                return;
            }

            if (_lobby != null && _lobby.JoinedCount > 0)
            {
                _intro.PlayToMenu();
            }
            else
            {
                _intro.PlayToAttract();
            }
        }

        /// <summary>Title 'Quit' button.</summary>
        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void HandleJoined(PlayerMenuInput menu)
        {
            int index = (int)menu.Id - 1;
            if (_isLeaving || index < 0 || index >= MaxPlayers)
            {
                return;
            }

            Unbind(index);
            _players[index] = menu;
            menu.HorizontalStep += _horizontalHandlers[index];
            menu.Submitted += _submitHandlers[index];
            menu.Canceled += _cancelHandlers[index];

            if (_phase == MenuPhase.CharacterSelect)
            {
                _selection.Join(menu.Id);
                Render();
            }
            else if (_phase == MenuPhase.Online && _onlineOwner == PlayerId.None && _online != null)
            {
                // MainMenu reloaded in a session (R1): the first player to join owns the online panel.
                _onlineOwner = menu.Id;
                _online.Enter(menu);
                if (_lobby != null)
                {
                    _lobby.SetJoiningEnabled(false);
                }
            }

            if (_phase == MenuPhase.Title)
            {
                RefreshTitleIntro();
            }

            // Next frame: never switch maps or (de)activate input inside the join callback, and the join press must not
            // also count as Submit. On the title (and its sub-menus) the newcomer may take ownership (lowest player
            // index), so refresh all.
            if (_phase == MenuPhase.Title || _phase == MenuPhase.SubMenu)
            {
                RequestApplyAll();
            }
            else
            {
                _applyFrame[index] = Time.frameCount + 1;
            }
        }

        private void HandleLeft(PlayerId id)
        {
            int index = (int)id - 1;
            if (_isLeaving || index < 0 || index >= MaxPlayers)
            {
                return;
            }

            Unbind(index);
            _applyFrame[index] = NoPendingFrame;
            _selection.Leave(id);

            switch (_phase)
            {
                case MenuPhase.Title:
                    RefreshTitleIntro();
                    // The other player may become the title owner.
                    RequestApplyAll();
                    break;
                case MenuPhase.SubMenu:
                    if (_lobby != null && _lobby.JoinedCount == 0)
                    {
                        EnterTitle();
                    }
                    else
                    {
                        // The other player may become the sub-menu owner.
                        RequestApplyAll();
                    }

                    break;
                case MenuPhase.Loading:
                    CancelLoading();
                    break;
                case MenuPhase.Online:
                    if (id == _onlineOwner)
                    {
                        // The owner's device is gone: close the panel (leaves the session) and go back to the title.
                        ExitOnline();
                        EnterTitle();
                    }

                    break;
                default:
                    if (_lobby != null && _lobby.JoinedCount == 0)
                    {
                        EnterTitle();
                    }
                    else
                    {
                        Render();
                    }

                    break;
            }
        }

        private void HandleHorizontalStep(PlayerId id, int step)
        {
            if (!_isLeaving && _phase == MenuPhase.CharacterSelect && _selection.Move(id, step))
            {
                Render();
            }
        }

        private void HandleSubmitted(PlayerId id)
        {
            if (!_isLeaving && _phase == MenuPhase.Title && _intro != null && _intro.IsTransitioning)
            {
                _intro.Skip();
                return;
            }

            if (_isLeaving || _phase != MenuPhase.CharacterSelect || !_selection.Confirm(id))
            {
                return;
            }

            Render();
            if (_selection.AllConfirmed)
            {
                BeginLoading();
            }
        }

        private void HandleCanceled(PlayerId id)
        {
            if (_isLeaving)
            {
                return;
            }

            switch (_phase)
            {
                case MenuPhase.Title:
                    // Any player backs out of the title: its device is freed; HandleLeft picks the next owner.
                    RemovePlayer(id);
                    break;
                case MenuPhase.SubMenu:
                    // The owner backs out to the title; the blocked player leaves (frees its device) as on the title.
                    if (id == GetTitleOwner())
                    {
                        CloseSubMenu();
                    }
                    else
                    {
                        RemovePlayer(id);
                    }

                    break;
                case MenuPhase.CharacterSelect:
                    if (_selection.Cancel(id))
                    {
                        Render();
                    }
                    else if (id == PlayerId.Player1)
                    {
                        EnterTitle();
                    }
                    else
                    {
                        // Player 2 backs out: remove it so its device is free and it can join again.
                        RemovePlayer(id);
                    }

                    break;
                case MenuPhase.Loading:
                    if (_selection.Cancel(id))
                    {
                        CancelLoading();
                    }

                    break;
                case MenuPhase.Online:
                    // The owner navigates the online panel; another local player backs out and frees its device.
                    if (id == _onlineOwner && _online != null)
                    {
                        _online.HandleCancel();
                    }
                    else
                    {
                        RemovePlayer(id);
                    }

                    break;
            }
        }

        /// <summary>Returns from the online hub to the title and focuses 'Play'.</summary>
        private void HandleOnlineExitRequested()
        {
            if (_phase != MenuPhase.Online || _isLeaving)
            {
                return;
            }

            ExitOnline();
            _phase = MenuPhase.Title;
            if (_title != null)
            {
                _title.Show();
            }

            RefreshTitleIntro();
            _titleReturnSelection = _playOnlineButton;
            // May come from a button onClick: input routing goes to the next frame.
            RequestApplyAll();
        }

        /// <summary>Closes the online panel and lets players join again.</summary>
        private void ExitOnline()
        {
            _onlineOwner = PlayerId.None;
            if (_online != null)
            {
                _online.Exit();
            }

            if (_lobby != null)
            {
                _lobby.SetJoiningEnabled(true);
            }
        }

        /// <summary>Destroys a menu player (PlayerInputManager unpairs its devices and raises onPlayerLeft).</summary>
        private void RemovePlayer(PlayerId id)
        {
            PlayerMenuInput menu = _players[(int)id - 1];
            if (menu != null)
            {
                Destroy(menu.gameObject);
            }
        }

        private void EnterTitle()
        {
            _phase = MenuPhase.Title;
            for (int i = 0; i < MaxPlayers; i++)
            {
                _selection.Leave((PlayerId)(i + 1));
            }

            if (_select != null)
            {
                _select.Hide();
            }

            HideActiveSubMenu();
            _titleReturnSelection = null;
            if (_title != null)
            {
                _title.Show();
            }

            RefreshTitleIntro();
            ApplyAllPlayerStates();
        }

        private void HideActiveSubMenu()
        {
            if (_activeSubMenu != null)
            {
                _activeSubMenu.Hide();
            }

            _activeSubMenu = null;
        }

        /// <summary>
        /// Players whose PlayerInput lost a required device (or has none) cannot be carried to the match: their pick is
        /// unlocked so their slot shows "choosing" again. Returns true if every joined player has its devices.
        /// </summary>
        private bool ReleasePlayersWithoutDevices()
        {
            bool allPresent = true;
            for (int i = 0; i < MaxPlayers; i++)
            {
                PlayerId id = (PlayerId)(i + 1);
                if (!_lobby.TryGetInput(id, out PlayerInput input))
                {
                    continue;
                }

                if (input.hasMissingRequiredDevices || input.devices.Count == 0)
                {
                    allPresent = false;
                    _selection.Cancel(id);
                    Debug.LogWarning($"[{nameof(MenuFlowController)}] {id} has no device paired; it must reconnect and confirm again.", this);
                }
            }

            return allPresent;
        }

        private void BeginLoading()
        {
            if (_session == null || _lobby == null || _roster == null)
            {
                Debug.LogError($"[{nameof(MenuFlowController)}] Cannot start the match: missing session, lobby or roster on '{name}'.", this);
                return;
            }

            if (!ReleasePlayersWithoutDevices())
            {
                Render();
                return;
            }

            _session.Clear();
            for (int i = 0; i < MaxPlayers; i++)
            {
                PlayerId id = (PlayerId)(i + 1);
                if (!_lobby.TryGetInput(id, out PlayerInput input))
                {
                    continue;
                }

                CharacterDefinition character = _roster.Get(_selection.GetConfirmedIndex(id));
                // One allocation per player per match: the session must outlive this scene's PlayerInputs.
                _session.SetSlot(new SessionSlot(id, input.currentControlScheme, input.devices.ToArray(), character));
            }

            _phase = MenuPhase.Loading;
            _lobby.SetJoiningEnabled(false);
            _loadAt = Time.unscaledTime + (_settings != null ? _settings.LoadDelayAfterConfirm : 0f);
            SetLoadingOverlay(true);
        }

        private void CancelLoading()
        {
            // Once the scene load is queued the session belongs to the gameplay scene: never clear it.
            if (_phase != MenuPhase.Loading || _isLeaving)
            {
                return;
            }

            _phase = MenuPhase.CharacterSelect;
            if (_session != null)
            {
                _session.Clear();
            }

            if (_lobby != null)
            {
                _lobby.SetJoiningEnabled(true);
            }

            SetLoadingOverlay(false);
            Render();
        }

        private PlayerId GetTitleOwner()
        {
            for (int i = 0; i < MaxPlayers; i++)
            {
                if (_players[i] != null)
                {
                    return (PlayerId)(i + 1);
                }
            }

            return PlayerId.None;
        }

        private void RequestApplyAll()
        {
            int next = Time.frameCount + 1;
            for (int i = 0; i < MaxPlayers; i++)
            {
                if (_players[i] != null)
                {
                    _applyFrame[i] = next;
                }
            }
        }

        private void ApplyAllPlayerStates()
        {
            for (int i = 0; i < MaxPlayers; i++)
            {
                _applyFrame[i] = NoPendingFrame;
                ApplyPlayerState((PlayerId)(i + 1));
            }
        }

        /// <summary>Puts a player's input in the state the current phase expects. Never call from an input callback.</summary>
        private void ApplyPlayerState(PlayerId id)
        {
            PlayerMenuInput menu = _players[(int)id - 1];
            if (menu == null)
            {
                return;
            }

            if (_phase == MenuPhase.Title || _phase == MenuPhase.SubMenu)
            {
                bool isOwner = id == GetTitleOwner();
                if (isOwner && _phase == MenuPhase.SubMenu && _activeSubMenu != null)
                {
                    menu.EnterMenu(_activeSubMenu.MenuRoot, _activeSubMenu.FirstSelected);
                }
                else if (isOwner && _phase == MenuPhase.Title && _title != null)
                {
                    if (_intro != null && !_intro.IsMenuInteractable)
                    {
                        // Keep Submit active for skipping while MenuReady is pending.
                        menu.EnterMenu(_title.MenuRoot, null);
                    }
                    else
                    {
                        // Back from a sub-menu: focus the button that opened it.
                        GameObject selection = _titleReturnSelection != null ? _titleReturnSelection : _title.FirstSelected;
                        _titleReturnSelection = null;
                        menu.EnterMenu(_title.MenuRoot, selection);
                    }
                }
                else
                {
                    // Non-owner: its event system is scoped to the select panel, which is inactive on the title, so it
                    // cannot click the owner's buttons, but its Cancel still arrives (to leave and free its device).
                    menu.EnterMenu(_select != null ? _select.MenuRoot : null, null);
                }

                return;
            }

            if (_phase == MenuPhase.Online)
            {
                if (id == _onlineOwner && _online != null)
                {
                    // The online controller owns this player's scope and focus (applied on its next frame).
                    _online.RefreshFocus();
                }
                else
                {
                    // Not the owner: scoped to the (inactive) select panel, so only its Cancel arrives (to leave).
                    menu.EnterMenu(_select != null ? _select.MenuRoot : null, null);
                }

                return;
            }

            // Character select and loading: code-driven (no focused Selectable), each player with its own cursor.
            menu.EnterMenu(_select != null ? _select.MenuRoot : null, null);
        }

        private void Render()
        {
            if (_select == null || _roster == null)
            {
                return;
            }

            int hoverP1 = GetCursor(PlayerId.Player1);
            int hoverP2 = GetCursor(PlayerId.Player2);
            for (int i = 0; i < _roster.Count; i++)
            {
                _select.RenderCard(_roster.Get(i), hoverP1 == i, hoverP2 == i, _selection.LockedBy(i));
            }

            RenderSlot(PlayerId.Player1);
            RenderSlot(PlayerId.Player2);
        }

        /// <summary>Hovered character of an unconfirmed player (a confirmed one shows the lock instead of a cursor).</summary>
        private int GetCursor(PlayerId id)
        {
            return _selection.IsJoined(id) && !_selection.IsConfirmed(id) ? _selection.GetHover(id) : CharacterSelection.NoCharacter;
        }

        private void RenderSlot(PlayerId id)
        {
            PlayerSlotState state = !_selection.IsJoined(id)
                ? PlayerSlotState.Empty
                : _selection.IsConfirmed(id) ? PlayerSlotState.Ready : PlayerSlotState.Choosing;
            CharacterDefinition character = _roster.Get(_selection.GetHover(id));
            _select.RenderSlot(id, state, character);
        }

        private void Unbind(int index)
        {
            PlayerMenuInput menu = _players[index];
            _players[index] = null;
            if (menu == null)
            {
                return;
            }

            menu.HorizontalStep -= _horizontalHandlers[index];
            menu.Submitted -= _submitHandlers[index];
            menu.Canceled -= _cancelHandlers[index];
        }

        private void SetLoadingOverlay(bool visible)
        {
            if (_loadingOverlay != null && _loadingOverlay.activeSelf != visible)
            {
                _loadingOverlay.SetActive(visible);
            }
        }

        private void OnValidate()
        {
            if (_roster == null || _settings == null)
            {
                Debug.LogWarning($"[{nameof(MenuFlowController)}] Missing CharacterRoster or MenuSettings on '{name}'.", this);
            }

            if (_lobby == null || _session == null)
            {
                Debug.LogWarning($"[{nameof(MenuFlowController)}] Missing MenuLobby or MatchSession on '{name}'.", this);
            }

            if (_title == null || _select == null || _intro == null)
            {
                Debug.LogWarning($"[{nameof(MenuFlowController)}] Missing TitleScreenView, CharacterSelectView or TitleIntroDirector on '{name}'.", this);
            }

            if (_online == null || _onlineSession == null || _playOnlineButton == null)
            {
                Debug.LogWarning($"[{nameof(MenuFlowController)}] Missing OnlineMenuController, OnlineSessionChannel or Play button on '{name}'.", this);
            }
        }
    }
}
