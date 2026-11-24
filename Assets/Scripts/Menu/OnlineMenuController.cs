using System;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Input;
using AlienFarms.UI;
using UnityEngine;
using UnityEngine.UI;

namespace AlienFarms.Menu
{
    /// <summary>
    /// Online phase of the MainMenu (hub, join, room) for the title owner. Talks to the network only through the
    /// <see cref="OnlineSessionChannel"/> and <see cref="OnlineLobbyChannel"/>: buttons call their <c>Request*</c> methods
    /// and every channel change only marks the panel dirty. The panel is rebuilt from the channels in the next frame's
    /// Update (never inside a button click), so the owner's input scope changes outside the event system and nothing is
    /// redrawn per frame. A full rebuild also covers a MainMenu reloaded while in a session (R1).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OnlineMenuController : MonoBehaviour
    {
        private const int NoHover = -1;

        [Header("Config")]
        [SerializeField, Tooltip("Runtime online session channel (OnlineSessionChannel.asset).")]
        private OnlineSessionChannel _session;

        [SerializeField, Tooltip("Runtime online lobby channel (OnlineLobbyChannel.asset).")]
        private OnlineLobbyChannel _lobby;

        [SerializeField, Tooltip("Selectable characters; lobby character indices are roster indices.")]
        private CharacterRoster _roster;

        [SerializeField, Tooltip("Optional: runtime progress channel (PlayerProgressChannel.asset). A 'host left' error becomes 'host left: win recorded' when a win was recorded for it.")]
        private PlayerProgressChannel _progress;

        [Header("References")]
        [SerializeField, Tooltip("Online panel view (UI_OnlinePanel).")]
        private OnlinePanelView _view;

        private PlayerMenuInput _owner;
        private GameObject _focusOverride;
        private OnlineScreen _shownScreen;
        private int _dirtyFrame;
        private int _hoverIndex = NoHover;
        private bool _isActive;
        private bool _wantsJoin;
        private bool _dirty;
        private bool _focusPending;
        private bool _hasShownScreen;
        private bool _shownReadOnly;
        private bool _shownLocked;
        private bool _shownCanStart;
        private bool _shownRejoin;
        private int _shownRejoinSeconds = -1;
        // The host-left win notice was consumed for the 'host left' error on screen: keep showing it until that error goes.
        private bool _hostLeftWinShown;

        /// <summary>The player backed out of the hub (Back / Cancel): the menu flow returns to the title.</summary>
        public event Action ExitRequested;

        /// <summary>True while the online panel is entered.</summary>
        public bool IsActive => _isActive;

        /// <summary>True while the host is loading the match (lobby Starting): the scene is loaded by the network.</summary>
        public bool IsLoadingMatch => _isActive
            && _session != null && _session.Status == OnlineSessionStatus.InSession
            && _lobby != null && _lobby.Phase == OnlineLobbyPhase.Starting;

        private OnlineSessionStatus Status => _session != null ? _session.Status : OnlineSessionStatus.SignedOut;

        private OnlineLobbyPhase LobbyPhase => _lobby != null ? _lobby.Phase : OnlineLobbyPhase.Unavailable;

        private OnlineScreen CurrentScreen => OnlineMenuRules.Resolve(Status, LobbyPhase, _wantsJoin);

        private void OnEnable()
        {
            if (_session != null)
            {
                _session.Changed += MarkDirty;
            }

            if (_lobby != null)
            {
                _lobby.Changed += MarkDirty;
            }

            if (_progress != null)
            {
                _progress.Changed += MarkDirty;
            }

            if (_view != null)
            {
                _view.CardClicked += HandleCardClicked;
                _view.CardSelected += HandleCardSelected;
                _view.CodeSubmitted += HandleCodeSubmitted;
                _view.NameEdited += HandleNameEdited;
            }

            // Changes may have happened while disabled (or before this scene loaded): rebuild everything.
            _hasShownScreen = false;
            MarkDirty();
        }

        private void OnDisable()
        {
            if (_session != null)
            {
                _session.Changed -= MarkDirty;
            }

            if (_lobby != null)
            {
                _lobby.Changed -= MarkDirty;
            }

            if (_progress != null)
            {
                _progress.Changed -= MarkDirty;
            }

            if (_view != null)
            {
                _view.CardClicked -= HandleCardClicked;
                _view.CardSelected -= HandleCardSelected;
                _view.CodeSubmitted -= HandleCodeSubmitted;
                _view.NameEdited -= HandleNameEdited;
            }
        }

        private void Update()
        {
            TickRejoin();

            // Strictly after the frame that marked it: changes may come from a button click inside the event system.
            if (!_isActive || !_dirty || Time.frameCount <= _dirtyFrame)
            {
                return;
            }

            _dirty = false;
            Refresh();
        }

        /// <summary>
        /// Opens the online panel for <paramref name="owner"/> and asks to sign in. <paramref name="owner"/> may be null
        /// (MainMenu reloaded in a session before any player joined): the panel renders and focus waits for an owner.
        /// </summary>
        public void Enter(PlayerMenuInput owner)
        {
            if (!_isActive)
            {
                _isActive = true;
                _wantsJoin = false;
                _hoverIndex = NoHover;
                if (_view != null)
                {
                    _view.Show();
                }

                if (_session != null)
                {
                    _session.RequestSignIn();
                }
            }

            _owner = owner;
            _hasShownScreen = false;
            MarkDirty();
        }

        /// <summary>Closes the panel. Leaves a session in progress (unless the match is loading) and declines a rejoin offer.</summary>
        public void Exit()
        {
            if (!_isActive)
            {
                return;
            }

            if (_session != null && LobbyPhase != OnlineLobbyPhase.Starting && OnlineSessionStatusRules.CanLeave(Status))
            {
                _session.RequestLeave();
            }

            // Leaving the hub gives up the seat held after a drop: the automatic rejoin must not bring the player back.
            if (_session != null)
            {
                _session.RequestDeclineRejoin();
            }

            _isActive = false;
            _owner = null;
            _dirty = false;
            _shownRejoin = false;
            _focusPending = false;
            _focusOverride = null;
            if (_view != null)
            {
                _view.Hide();
            }
        }

        /// <summary>Re-applies the owner's input scope and focus on the next frame (menu flow ApplyPlayerState).</summary>
        public void RefreshFocus()
        {
            _focusPending = true;
            MarkDirty();
        }

        /// <summary>
        /// Owner's Cancel: Join -> hub; Room -> release the pick, or focus Leave without one; Hub -> exit to the title;
        /// Busy -> cancel the pending create / join (or leave the panel while signing in). False when ignored.
        /// </summary>
        public bool HandleCancel()
        {
            if (!_isActive)
            {
                return false;
            }

            switch (CurrentScreen)
            {
                case OnlineScreen.Join:
                    Back();
                    return true;
                case OnlineScreen.Room:
                    if (HasLocalCharacter())
                    {
                        _lobby.RequestCharacter(OnlineLobbyChannel.ReleaseCharacter);
                    }
                    else
                    {
                        FocusLeave();
                    }

                    return true;
                case OnlineScreen.Busy:
                    return CancelBusyInternal();
                default:
                    // Cancel while typing the name only ends the edit (it is saved); it never leaves the hub.
                    if (_view != null && _view.IsEditingName)
                    {
                        _view.EndNameEdit();
                        return true;
                    }

                    ExitRequested?.Invoke();
                    return true;
            }
        }

        // ---- Buttons (OnClick) ----

        /// <summary>Hub 'Create room'. Signed out (sign-in failed): retries the sign-in instead.</summary>
        public void CreateRoom()
        {
            if (!_isActive || _session == null)
            {
                return;
            }

            // A stale error (e.g. host left) must not stay on screen after a new action.
            _session.ClearError();
            if (Status == OnlineSessionStatus.SignedOut)
            {
                _session.RequestSignIn();
                return;
            }

            _session.RequestCreate();
        }

        /// <summary>
        /// Hub 'Reconnect (n s)': rejoins the session this machine dropped from (the channel checks the offer and reports
        /// <c>rejoin_expired</c> when it ran out). Signed out: retries the sign-in instead.
        /// </summary>
        public void RejoinRoom()
        {
            if (!_isActive || _session == null || CurrentScreen != OnlineScreen.Hub)
            {
                return;
            }

            _session.ClearError();
            if (Status == OnlineSessionStatus.SignedOut)
            {
                _session.RequestSignIn();
                return;
            }

            _session.RequestRejoin();
            MarkDirty();
        }

        /// <summary>Hub 'Join room': opens the code screen.</summary>
        public void OpenJoin()
        {
            if (!_isActive || CurrentScreen != OnlineScreen.Hub)
            {
                return;
            }

            _wantsJoin = true;
            if (_session != null)
            {
                _session.ClearError();
            }

            MarkDirty();
        }

        /// <summary>Join screen 'Join': joins with the typed code (validated by the channel).</summary>
        public void ConfirmJoin()
        {
            if (!_isActive || _session == null || _view == null)
            {
                return;
            }

            if (Status == OnlineSessionStatus.SignedOut)
            {
                _session.RequestSignIn();
                return;
            }

            _session.RequestJoin(_view.CodeText);
        }

        /// <summary>Room 'Leave'.</summary>
        public void LeaveRoom()
        {
            if (_isActive && _session != null)
            {
                _session.RequestLeave();
            }
        }

        /// <summary>Room 'Start' (host): the lobby validates and loads the match.</summary>
        public void StartMatch()
        {
            if (_isActive && _session != null && _session.IsHost && _lobby != null)
            {
                _lobby.RequestStart();
            }
        }

        /// <summary>'Back' on the hub (exit to the title) or on the join screen (back to the hub).</summary>
        public void Back()
        {
            if (!_isActive)
            {
                return;
            }

            OnlineScreen screen = CurrentScreen;
            if ((screen == OnlineScreen.Join || screen == OnlineScreen.Hub) && _session != null)
            {
                _session.ClearError();
            }

            if (screen == OnlineScreen.Join)
            {
                _wantsJoin = false;
                MarkDirty();
            }
            else if (screen == OnlineScreen.Hub)
            {
                ExitRequested?.Invoke();
            }
        }

        /// <summary>Busy 'Cancel'.</summary>
        public void CancelBusy()
        {
            if (_isActive && CurrentScreen == OnlineScreen.Busy)
            {
                CancelBusyInternal();
            }
        }

        // ---- Internals ----

        private bool CancelBusyInternal()
        {
            OnlineSessionStatus status = Status;
            if (!OnlineMenuRules.CanCancelBusy(status))
            {
                return false;
            }

            if (status == OnlineSessionStatus.SigningIn)
            {
                // Sign-in cannot be aborted; it keeps going in the background.
                ExitRequested?.Invoke();
            }
            else if (_session != null)
            {
                _session.RequestLeave();
            }

            // A cancelled join lands on the hub, not back on the code screen.
            _wantsJoin = OnlineMenuRules.WantsJoinAfterCancelBusy(status, _wantsJoin);
            MarkDirty();
            return true;
        }

        // 1 Hz redraw of 'Reconnect (n s)' while it shows: only compares the whole second (no allocation per frame).
        private void TickRejoin()
        {
            if (!_isActive || !_shownRejoin || _session == null)
            {
                return;
            }

            double now = Time.realtimeSinceStartupAsDouble;
            double expiresAt = _session.RejoinExpiresAt;
            if (!OnlineMenuRules.ShowsRejoin(_session.HasRejoinOffer, expiresAt, now))
            {
                // Expired: a full refresh hides it and moves the focus off it.
                if (!_dirty)
                {
                    MarkDirty();
                }

                return;
            }

            int seconds = OnlineMenuRules.RejoinSecondsLeft(expiresAt, now);
            if (seconds != _shownRejoinSeconds)
            {
                _shownRejoinSeconds = seconds;
                _view.SetRejoin(true, seconds);
            }
        }

        /// <summary>Renders BTN_Rejoin on the hub; asks to move the focus off it when it just hid under the owner's cursor.</summary>
        private void RenderRejoin()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            bool hasOffer = _session != null && _session.HasRejoinOffer;
            double expiresAt = hasOffer ? _session.RejoinExpiresAt : 0d;
            bool visible = OnlineMenuRules.ShowsRejoin(hasOffer, expiresAt, now);
            int seconds = visible ? OnlineMenuRules.RejoinSecondsLeft(expiresAt, now) : 0;
            if (_shownRejoin && !visible && _owner != null && _owner.CurrentSelection == _view.RejoinSelectable)
            {
                _focusPending = true;
            }

            _shownRejoin = visible;
            _shownRejoinSeconds = seconds;
            _view.SetRejoin(visible, seconds);
        }

        private void HandleNameEdited(string text)
        {
            if (_session != null)
            {
                // Sanitized and persisted by the channel; its Changed redraws the field with the clean name.
                _session.SetDisplayName(text);
            }
        }

        private void MarkDirty()
        {
            _dirty = true;
            _dirtyFrame = Time.frameCount;
        }

        private void FocusLeave()
        {
            if (_view == null)
            {
                return;
            }

            _focusOverride = _view.LeaveSelectable;
            RefreshFocus();
        }

        // Enter / Submit inside the code input confirms the join (with a pad the field would otherwise keep the focus).
        private void HandleCodeSubmitted()
        {
            if (_isActive && CurrentScreen == OnlineScreen.Join)
            {
                ConfirmJoin();
            }
        }

        private void HandleCardClicked(CharacterDefinition character)
        {
            if (!_isActive || _lobby == null || _roster == null || CurrentScreen != OnlineScreen.Room)
            {
                return;
            }

            int index = _roster.IndexOf(character);
            if (index >= 0)
            {
                _hoverIndex = index;
                _lobby.RequestCharacter(index);
            }
        }

        private void HandleCardSelected(CharacterDefinition character)
        {
            if (!_isActive || _roster == null)
            {
                return;
            }

            int index = _roster.IndexOf(character);
            if (index != _hoverIndex)
            {
                _hoverIndex = index;
                MarkDirty();
            }
        }

        /// <summary>Rebuilds the whole panel from the channels and moves the owner's focus when the screen changed.</summary>
        private void Refresh()
        {
            if (_view == null)
            {
                return;
            }

            OnlineSessionStatus status = Status;
            OnlineLobbyPhase phase = LobbyPhase;
            if (status == OnlineSessionStatus.InSession)
            {
                // Reached the room: the next time the hub shows, it is the hub (not the code screen).
                _wantsJoin = false;
            }

            OnlineScreen screen = OnlineMenuRules.Resolve(status, phase, _wantsJoin);
            _view.ShowScreen(screen);
            _view.ShowError(GetErrorKey(screen));
            if (screen != OnlineScreen.Hub)
            {
                // BTN_Rejoin lives in the hub: no countdown to tick elsewhere.
                _shownRejoin = false;
            }

            bool readOnly = false;
            bool locked = false;
            bool canStart = false;
            switch (screen)
            {
                case OnlineScreen.Hub:
                    _view.SetSignedInAs(GetLocalDisplayName());
                    _view.SetNameText(_session != null ? _session.DisplayName : null);
                    RenderRejoin();
                    break;
                case OnlineScreen.Busy:
                    _view.SetBusy(OnlineMenuRules.BusyKey(status, phase), OnlineMenuRules.CanCancelBusy(status));
                    break;
                case OnlineScreen.Room:
                    readOnly = RenderRoom(phase, out locked, out canStart);
                    break;
            }

            bool screenChanged = !_hasShownScreen || screen != _shownScreen;
            bool roleChanged = screen == OnlineScreen.Room && (readOnly != _shownReadOnly || locked != _shownLocked);
            if (screenChanged || roleChanged || _focusPending)
            {
                ApplyFocus(screen, readOnly, locked, canStart, screenChanged || roleChanged);
            }
            else if (screen == OnlineScreen.Room && ShouldMoveRoomFocus(locked, canStart))
            {
                // The focused button / card stopped being usable (Start disabled, card taken): pick the default again.
                ApplyFocus(screen, readOnly, locked, canStart, true);
            }

            _shownCanStart = canStart;
        }

        /// <summary>
        /// True when the owner's focus sits on a Selectable that is no longer interactable, or when Start just became
        /// available for a locked host still waiting on Leave. Only runs on channel changes (never per frame).
        /// </summary>
        private bool ShouldMoveRoomFocus(bool locked, bool canStart)
        {
            GameObject selected = _owner != null ? _owner.CurrentSelection : null;
            if (selected == null)
            {
                return false;
            }

            if (selected.TryGetComponent(out Selectable selectable) && !selectable.IsInteractable())
            {
                return true;
            }

            return locked && canStart && !_shownCanStart && IsHost && selected == _view.LeaveSelectable;
        }

        private bool IsHost => _session != null && _session.IsHost;

        private void ApplyFocus(OnlineScreen screen, bool readOnly, bool locked, bool canStart, bool screenChanged)
        {
            if (_owner == null)
            {
                // No owner yet: keep the request until one enters.
                return;
            }

            GameObject target;
            if (_focusOverride != null && !screenChanged)
            {
                target = _focusOverride;
            }
            else if (screen == OnlineScreen.Room && readOnly)
            {
                target = _view.LeaveSelectable;
            }
            else if (screen == OnlineScreen.Room && locked)
            {
                // Picked = ready: the cards are locked, the host waits on Start (once usable) and the guest on Leave.
                GameObject start = IsHost && canStart ? _view.StartSelectable : null;
                target = start != null ? start : _view.LeaveSelectable;
            }
            else if (screen == OnlineScreen.Room)
            {
                // First card the other player has not taken; all taken -> Leave.
                GameObject card = _view.GetFirstFreeCard();
                target = card != null ? card : _view.LeaveSelectable;
            }
            else if (screen == OnlineScreen.Hub && _shownRejoin && _view.RejoinSelectable != null)
            {
                // Back from a drop: reconnecting is the likely next step.
                target = _view.RejoinSelectable;
            }
            else
            {
                target = _view.GetFirstSelected(screen);
            }

            _owner.EnterMenu(_view.GetRoot(screen), target);
            _hasShownScreen = true;
            _shownScreen = screen;
            _shownReadOnly = readOnly;
            _shownLocked = locked;
            _focusPending = false;
            _focusOverride = null;
        }

        /// <summary>
        /// Renders cards, seats, code, start and hint. Returns true when the room is read-only here;
        /// <paramref name="locked"/> is true when the local seat holds a pick (cards locked until it is released).
        /// </summary>
        private bool RenderRoom(OnlineLobbyPhase phase, out bool locked, out bool canStart)
        {
            PlayerId localId = _lobby != null ? _lobby.LocalPlayerId : PlayerId.None;
            OnlineLobbySeat localSeat = default;
            bool seated = _lobby != null && _lobby.TryGetSeat(localId, out localSeat);
            bool localHasCharacter = seated && localSeat.HasCharacter;
            bool open = phase == OnlineLobbyPhase.Open;

            // Picking is being ready: the cards stay locked until Cancel releases the pick (as in the local selection).
            bool cardsOpen = seated && open && !localHasCharacter;
            int count = _roster != null ? _roster.Count : 0;
            for (int i = 0; i < count; i++)
            {
                // Only the local cursor is known; a picked player shows its lock instead of a cursor (as locally).
                bool hover = seated && !localHasCharacter && _hoverIndex == i;
                PlayerId lockedBy = LockedBy(i);
                CharacterDefinition character = _roster.Get(i);
                _view.RenderCard(character, hover && localId == PlayerId.Player1, hover && localId == PlayerId.Player2, lockedBy);
                // A card taken by the other player is skipped by navigation; its lock still renders.
                _view.SetCardInteractable(character, cardsOpen && lockedBy == PlayerId.None);
            }

            bool p1Picked = RenderSeat(PlayerId.Player1);
            bool p2Picked = RenderSeat(PlayerId.Player2);
            bool allPicked = p1Picked && p2Picked;
            _view.SetCode(_session != null ? _session.Code : null);

            bool isHost = IsHost;
            canStart = open && _lobby != null && _lobby.CanStart;
            _view.SetStart(isHost, canStart);
            int seatCount = _lobby != null ? _lobby.SeatCount : 0;
            _view.SetHint(OnlineMenuRules.HintKey(phase, seatCount, allPicked, isHost));

            // Not seated yet: nothing to pick, focus goes to Leave.
            locked = localHasCharacter;
            return !seated;
        }

        /// <summary>Renders one seat; true when it is occupied and has a pick.</summary>
        private bool RenderSeat(PlayerId id)
        {
            OnlineLobbySeat seat = default;
            bool occupied = _lobby != null && _lobby.TryGetSeat(id, out seat);
            CharacterDefinition character = null;
            if (occupied && _roster != null)
            {
                int index = seat.HasCharacter ? seat.CharacterIndex : seat.IsLocal ? _hoverIndex : NoHover;
                character = _roster.Get(index);
            }

            _view.RenderSeat(id, seat, occupied, character);
            return occupied && seat.HasCharacter;
        }

        private PlayerId LockedBy(int characterIndex)
        {
            int count = _lobby != null ? _lobby.SeatCount : 0;
            for (int i = 0; i < count; i++)
            {
                OnlineLobbySeat seat = _lobby.GetSeat(i);
                if (seat.CharacterIndex == characterIndex)
                {
                    return seat.Id;
                }
            }

            return PlayerId.None;
        }

        private bool HasLocalCharacter()
        {
            return _lobby != null
                && _lobby.TryGetSeat(_lobby.LocalPlayerId, out OnlineLobbySeat seat)
                && seat.HasCharacter;
        }

        private string GetErrorKey(OnlineScreen screen)
        {
            // Room refusals only belong to the room: elsewhere a stale one would hide the session error.
            string reject = screen == OnlineScreen.Room && _lobby != null ? _lobby.LastRejectKey : null;
            if (!string.IsNullOrEmpty(reject))
            {
                return reject;
            }

            string key = _session != null ? _session.LastErrorKey : null;
            if (!string.Equals(key, OnlineSessionErrorKeys.HostLeft, StringComparison.Ordinal))
            {
                _hostLeftWinShown = false;
                return key;
            }

            // The only reader of the notice: shown in place of the plain 'host left', then consumed.
            if (!_hostLeftWinShown && _progress != null && _progress.HasHostLeftWinNotice)
            {
                _hostLeftWinShown = true;
                _progress.RequestConsumeHostLeftWinNotice();
            }

            return _hostLeftWinShown ? LocalizationKeys.StatsHostLeftWin : key;
        }

        /// <summary>
        /// Local participant's name when known, else the typed name, else the name published after the sign-in (UGS
        /// name); the raw player id only as a last resort (null when signed out).
        /// </summary>
        private string GetLocalDisplayName()
        {
            if (_session == null)
            {
                return null;
            }

            int count = _session.ParticipantCount;
            for (int i = 0; i < count; i++)
            {
                OnlineParticipant participant = _session.GetParticipant(i);
                if (participant.IsLocal && !string.IsNullOrEmpty(participant.DisplayName))
                {
                    return participant.DisplayName;
                }
            }

            // Signed out: no "signed in as" line at all.
            if (string.IsNullOrEmpty(_session.LocalPlayerId))
            {
                return null;
            }

            string typed = _session.DisplayName;
            if (!string.IsNullOrEmpty(typed))
            {
                // As the others will see it (the payload holds 29 bytes).
                return DisplayNameSanitizer.FitToPayload(DisplayNameSanitizer.Sanitize(typed));
            }

            string published = _session.LocalPlayerName;
            return !string.IsNullOrEmpty(published) ? published : _session.LocalPlayerId;
        }

        private void OnValidate()
        {
            if (_session == null || _lobby == null)
            {
                Debug.LogWarning($"[{nameof(OnlineMenuController)}] Missing OnlineSessionChannel or OnlineLobbyChannel on '{name}'.", this);
            }

            if (_roster == null || _view == null)
            {
                Debug.LogWarning($"[{nameof(OnlineMenuController)}] Missing CharacterRoster or OnlinePanelView on '{name}'.", this);
            }
        }
    }
}
