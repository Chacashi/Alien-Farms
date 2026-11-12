using System;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace AlienFarms.UI
{
    /// <summary>
    /// Online panel (hub, join, room, busy) of the MainMenu. Pure view: everything lives in the scene and is referenced
    /// here; <see cref="OnlineMenuController"/> drives it on channel changes (never per frame). Lives on an always-active
    /// object and toggles <see cref="_panelRoot"/>. Dynamic texts (error, busy label, hint) swap the entry of a
    /// <see cref="LocalizedString"/>; their text arrives through StringChanged, so strings are only built on changes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OnlinePanelView : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Error banner entry; the key is replaced at runtime (default only resolves the table).")]
        private LocalizedString _errorEntry = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.OnlineErrorUnknown);

        [SerializeField, Tooltip("Busy label entry; the key is replaced at runtime.")]
        private LocalizedString _busyEntry = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.OnlineBusySigningIn);

        [SerializeField, Tooltip("'Signed in as {0}' format (non-Smart).")]
        private LocalizedString _signedInEntry = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.OnlineHubSignedInAs);

        [SerializeField, Tooltip("'Reconnect ({0} s)' format (non-Smart): used as a TextMeshPro SetText template, so only {0} may hold braces.")]
        private LocalizedString _rejoinEntry = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.OnlineHubRejoin);

        [SerializeField, Tooltip("Room hint entry; the key is replaced at runtime.")]
        private LocalizedString _hintEntry = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.OnlineRoomWaitingPlayer);

        [Header("Roots")]
        [SerializeField, Tooltip("Whole panel. Must not contain this component.")]
        private GameObject _panelRoot;

        [SerializeField] private GameObject _hubRoot;
        [SerializeField] private GameObject _joinRoot;
        [SerializeField] private GameObject _roomRoot;

        [SerializeField, Tooltip("Busy overlay (blocks raycasts).")]
        private GameObject _busyRoot;

        [SerializeField, Tooltip("Error banner, shown while there is an error.")]
        private GameObject _errorRoot;

        [Header("First selected per screen")]
        [SerializeField, Tooltip("BTN_Create.")]
        private GameObject _hubFirst;

        [SerializeField, Tooltip("INP_Code.")]
        private GameObject _joinFirst;

        [SerializeField, Tooltip("First character card.")]
        private GameObject _roomFirst;

        [SerializeField, Tooltip("BTN_CancelBusy (hidden while the busy state cannot be cancelled).")]
        private GameObject _busyFirst;

        [SerializeField, Tooltip("BTN_Leave: focused on Cancel without a pick and for spectators.")]
        private GameObject _roomLeave;

        [Header("Texts")]
        [SerializeField] private TMP_Text _signedInAs;

        [SerializeField, Tooltip("Room code (large).")]
        private TMP_Text _code;

        [SerializeField, Tooltip("Room hint (waiting for a player / the host / both picks, syncing).")]
        private TMP_Text _hint;

        [SerializeField] private TMP_Text _busyLabel;
        [SerializeField] private TMP_Text _error;

        [SerializeField, Tooltip("Join code input (max 6 chars, upper case).")]
        private TMP_InputField _codeInput;

        [Header("Hub")]
        [SerializeField, Tooltip("BTN_Rejoin: shown while a reconnection offer is valid.")]
        private UnityEngine.UI.Button _rejoinButton;

        [SerializeField, Tooltip("Label of BTN_Rejoin ('Reconnect (n s)'), written from code.")]
        private TMP_Text _rejoinLabel;

        [SerializeField, Tooltip("Hub BTN_Join (Explicit navigation): its Down goes to BTN_Rejoin while shown, else to the hub Back button.")]
        private UnityEngine.UI.Selectable _hubJoinButton;

        [SerializeField, Tooltip("Hub BTN_Back (Explicit navigation): its Up goes to BTN_Rejoin while shown, else to the hub Join button.")]
        private UnityEngine.UI.Selectable _hubBackButton;

        [SerializeField, Tooltip("INP_Name: name typed by the player (saved on end edit). Its character limit is set from code.")]
        private TMP_InputField _nameInput;

        [Header("Room")]
        [SerializeField, Tooltip("One card per selectable character (matched by character, any order).")]
        private CharacterCardView[] _cards = new CharacterCardView[0];

        [SerializeField, Tooltip("Button of each card (UI_CharacterCard_Online).")]
        private CharacterCardButton[] _cardButtons = new CharacterCardButton[0];

        [SerializeField, Tooltip("One view per player seat (matched by player).")]
        private OnlineSeatView[] _seats = new OnlineSeatView[0];

        [SerializeField, Tooltip("'Spectator: name' row, shown while someone watches.")]
        private GameObject _spectatorRow;

        [SerializeField] private TMP_Text _spectatorName;

        [SerializeField, Tooltip("'You are watching' banner, shown when this machine spectates.")]
        private GameObject _spectatorBanner;

        [SerializeField, Tooltip("Start button (host only).")]
        private UnityEngine.UI.Button _startButton;

        [SerializeField, Tooltip("Object toggled to show / hide Start (the button itself or its container).")]
        private GameObject _startRoot;

        private LocalizedStringBinding _errorBinding;
        private LocalizedStringBinding _busyBinding;
        private LocalizedStringBinding _signedInBinding;
        private LocalizedStringBinding _hintBinding;
        private LocalizedStringBinding _rejoinBinding;
        private Action<CharacterDefinition> _cardClickedHandler;
        private Action<CharacterDefinition> _cardSelectedHandler;
        private UnityEngine.Events.UnityAction<string> _codeSubmitHandler;
        private UnityEngine.Events.UnityAction<string> _nameEndEditHandler;
        private int _rejoinSeconds = -1;
        private int _nameEndEditFrame = -1;
        private bool _rejoinVisible;
        private bool _hasRejoinVisibility;
        private string _signedInName;
        private string _codeText;
        private string _spectatorText;
        private bool _hasSignedIn;
        private bool _hasCode;
        private bool _hasSpectator;

        /// <summary>A card was clicked / submitted (pick request).</summary>
        public event Action<CharacterDefinition> CardClicked;

        /// <summary>A card received the local player's focus (cursor).</summary>
        public event Action<CharacterDefinition> CardSelected;

        /// <summary>The join code input was submitted (Enter / pad Submit while editing).</summary>
        public event Action CodeSubmitted;

        /// <summary>The name input finished an edit (Enter, pad Submit, click outside, Cancel); carries the raw text.</summary>
        public event Action<string> NameEdited;

        /// <summary>Text typed in the join code input (null without input).</summary>
        public string CodeText => _codeInput != null ? _codeInput.text : null;

        /// <summary>Hub 'Reconnect' button (null when not assigned).</summary>
        public GameObject RejoinSelectable => _rejoinButton != null ? _rejoinButton.gameObject : null;

        /// <summary>
        /// True while the name input is being edited, and on the frame its edit ended: the field's own Cancel (end edit)
        /// and the owner's latched Cancel arrive in different updates, and the latter must not leave the hub.
        /// </summary>
        public bool IsEditingName => _nameInput != null && (_nameInput.isFocused || Time.frameCount - _nameEndEditFrame <= 1);

        /// <summary>Room 'Leave' button.</summary>
        public GameObject LeaveSelectable => _roomLeave;

        /// <summary>Host 'Start' button (null when not assigned).</summary>
        public GameObject StartSelectable => _startButton != null ? _startButton.gameObject : null;

        private void Awake()
        {
            _errorBinding = new LocalizedStringBinding(_errorEntry, RefreshError);
            _busyBinding = new LocalizedStringBinding(_busyEntry, RefreshBusy);
            _signedInBinding = new LocalizedStringBinding(_signedInEntry, RefreshSignedIn);
            _hintBinding = new LocalizedStringBinding(_hintEntry, RefreshHint);
            _rejoinBinding = new LocalizedStringBinding(_rejoinEntry, RefreshRejoin);
            _cardClickedHandler = HandleCardClicked;
            _cardSelectedHandler = HandleCardSelected;
            _codeSubmitHandler = HandleCodeSubmitted;
            _nameEndEditHandler = HandleNameEndEdit;
            if (_nameInput != null)
            {
                _nameInput.characterLimit = DisplayNameSanitizer.MaxTypedLength;
                // Pending from 2.2 (INP_Code): an active field swallows the D-pad. This one only starts editing on
                // Submit / click, so pad navigation passes over it; Submit or Cancel end the edit.
                _nameInput.shouldActivateOnSelect = false;
            }
        }

        private void OnEnable()
        {
            _errorBinding.Enable();
            _busyBinding.Enable();
            _signedInBinding.Enable();
            _hintBinding.Enable();
            _rejoinBinding.Enable();
            if (_codeInput != null)
            {
                _codeInput.onSubmit.AddListener(_codeSubmitHandler);
            }

            if (_nameInput != null)
            {
                _nameInput.onEndEdit.AddListener(_nameEndEditHandler);
            }

            for (int i = 0; i < _cardButtons.Length; i++)
            {
                if (_cardButtons[i] != null)
                {
                    _cardButtons[i].Clicked += _cardClickedHandler;
                    _cardButtons[i].Selected += _cardSelectedHandler;
                }
            }
        }

        private void OnDisable()
        {
            _errorBinding.Disable();
            _busyBinding.Disable();
            _signedInBinding.Disable();
            _hintBinding.Disable();
            _rejoinBinding.Disable();
            if (_codeInput != null)
            {
                _codeInput.onSubmit.RemoveListener(_codeSubmitHandler);
            }

            if (_nameInput != null)
            {
                _nameInput.onEndEdit.RemoveListener(_nameEndEditHandler);
            }

            for (int i = 0; i < _cardButtons.Length; i++)
            {
                if (_cardButtons[i] != null)
                {
                    _cardButtons[i].Clicked -= _cardClickedHandler;
                    _cardButtons[i].Selected -= _cardSelectedHandler;
                }
            }
        }

        // ---- Navigation ----

        public void Show() => SetActive(_panelRoot, true);

        public void Hide() => SetActive(_panelRoot, false);

        /// <summary>Shows only the root of <paramref name="screen"/>.</summary>
        public void ShowScreen(OnlineScreen screen)
        {
            SetActive(_hubRoot, screen == OnlineScreen.Hub);
            SetActive(_joinRoot, screen == OnlineScreen.Join);
            SetActive(_roomRoot, screen == OnlineScreen.Room);
            SetActive(_busyRoot, screen == OnlineScreen.Busy);
        }

        /// <summary>Root the owner's event system is scoped to on <paramref name="screen"/>.</summary>
        public GameObject GetRoot(OnlineScreen screen)
        {
            switch (screen)
            {
                case OnlineScreen.Join:
                    return _joinRoot;
                case OnlineScreen.Room:
                    return _roomRoot;
                case OnlineScreen.Busy:
                    return _busyRoot;
                default:
                    return _hubRoot;
            }
        }

        /// <summary>Selectable focused when <paramref name="screen"/> opens.</summary>
        public GameObject GetFirstSelected(OnlineScreen screen)
        {
            switch (screen)
            {
                case OnlineScreen.Join:
                    return _joinFirst;
                case OnlineScreen.Room:
                    return _roomFirst;
                case OnlineScreen.Busy:
                    return _busyFirst;
                default:
                    return _hubFirst;
            }
        }

        // ---- Texts ----

        /// <summary>Hub 'Signed in as {0}' line; null hides it.</summary>
        public void SetSignedInAs(string displayName)
        {
            if (_hasSignedIn && string.Equals(_signedInName, displayName, StringComparison.Ordinal))
            {
                return;
            }

            _hasSignedIn = true;
            _signedInName = displayName;
            if (_signedInAs != null)
            {
                SetActive(_signedInAs.gameObject, !string.IsNullOrEmpty(displayName));
            }

            RefreshSignedIn();
        }

        /// <summary>
        /// Hub 'Reconnect (n s)' button: shown with <paramref name="seconds"/> while <paramref name="visible"/>. The label
        /// is rewritten only when the second changes (SetText with the localized template, no allocation).
        /// </summary>
        public void SetRejoin(bool visible, int seconds)
        {
            if (!_hasRejoinVisibility || visible != _rejoinVisible)
            {
                _hasRejoinVisibility = true;
                RewireHubNavigation(visible);
            }

            _rejoinVisible = visible;
            SetActive(RejoinSelectable, visible);
            if (visible && seconds != _rejoinSeconds)
            {
                _rejoinSeconds = seconds;
                RefreshRejoin();
            }
        }

        /// <summary>Shows <paramref name="displayName"/> in the name input, unless the player is editing it.</summary>
        public void SetNameText(string displayName)
        {
            if (_nameInput == null || _nameInput.isFocused)
            {
                return;
            }

            displayName ??= string.Empty;
            if (!string.Equals(_nameInput.text, displayName, StringComparison.Ordinal))
            {
                _nameInput.SetTextWithoutNotify(displayName);
            }
        }

        /// <summary>Ends an edit in progress of the name input (its end edit saves the name).</summary>
        public void EndNameEdit()
        {
            if (_nameInput != null && _nameInput.isFocused)
            {
                _nameInput.DeactivateInputField();
            }
        }

        /// <summary>Room code (null clears it).</summary>
        public void SetCode(string code)
        {
            if (_hasCode && string.Equals(_codeText, code, StringComparison.Ordinal))
            {
                return;
            }

            _hasCode = true;
            _codeText = code;
            if (_code != null)
            {
                _code.SetText(code ?? string.Empty);
            }
        }

        /// <summary>Shows the error banner with the text of <paramref name="key"/>; null hides it.</summary>
        public void ShowError(string key)
        {
            bool visible = !string.IsNullOrEmpty(key);
            if (visible)
            {
                SetEntry(_errorEntry, key);
            }

            SetActive(_errorRoot, visible);
        }

        /// <summary>Busy label from <paramref name="key"/>; the Cancel button is shown only when <paramref name="cancelable"/>.</summary>
        public void SetBusy(string key, bool cancelable)
        {
            if (!string.IsNullOrEmpty(key))
            {
                SetEntry(_busyEntry, key);
            }

            SetActive(_busyFirst, cancelable);
        }

        /// <summary>Room hint from <paramref name="key"/>; null hides it.</summary>
        public void SetHint(string key)
        {
            bool visible = !string.IsNullOrEmpty(key);
            if (visible)
            {
                SetEntry(_hintEntry, key);
            }

            if (_hint != null)
            {
                SetActive(_hint.gameObject, visible);
            }
        }

        // ---- Room ----

        /// <summary>Cursor badges and lock of the card showing <paramref name="character"/>.</summary>
        public void RenderCard(CharacterDefinition character, bool hoverP1, bool hoverP2, PlayerId lockedBy)
        {
            for (int i = 0; i < _cards.Length; i++)
            {
                if (_cards[i] != null && _cards[i].Character == character)
                {
                    _cards[i].Render(hoverP1, hoverP2, lockedBy);
                }
            }
        }

        public void RenderSeat(PlayerId player, in OnlineLobbySeat seat, bool occupied, CharacterDefinition character)
        {
            for (int i = 0; i < _seats.Length; i++)
            {
                if (_seats[i] != null && _seats[i].Player == player)
                {
                    _seats[i].Render(seat, occupied, character);
                }
            }
        }

        /// <summary>Spectator row (shown when <paramref name="displayName"/> is not null) and the local 'watching' banner.</summary>
        public void SetSpectator(bool isLocal, string displayName)
        {
            bool hasSpectator = displayName != null;
            SetActive(_spectatorRow, hasSpectator);
            SetActive(_spectatorBanner, isLocal);
            if (_spectatorName != null && hasSpectator
                && (!_hasSpectator || !string.Equals(_spectatorText, displayName, StringComparison.Ordinal)))
            {
                _hasSpectator = true;
                _spectatorText = displayName;
                _spectatorName.SetText(displayName);
            }
        }

        public void SetStart(bool visible, bool interactable)
        {
            SetActive(_startRoot != null ? _startRoot : _startButton != null ? _startButton.gameObject : null, visible);
            if (_startButton != null && _startButton.interactable != interactable)
            {
                _startButton.interactable = interactable;
            }
        }

        public void SetCardsInteractable(bool interactable)
        {
            for (int i = 0; i < _cardButtons.Length; i++)
            {
                if (_cardButtons[i] != null)
                {
                    _cardButtons[i].Interactable = interactable;
                }
            }
        }

        /// <summary>Enables or disables the button of the card showing <paramref name="character"/> (its render is untouched).</summary>
        public void SetCardInteractable(CharacterDefinition character, bool interactable)
        {
            for (int i = 0; i < _cardButtons.Length; i++)
            {
                if (_cardButtons[i] != null && _cardButtons[i].Character == character)
                {
                    _cardButtons[i].Interactable = interactable;
                }
            }
        }

        /// <summary>First interactable card button in panel order (null when none): the room's default focus.</summary>
        public GameObject GetFirstFreeCard()
        {
            for (int i = 0; i < _cardButtons.Length; i++)
            {
                CharacterCardButton button = _cardButtons[i];
                if (button != null && button.Interactable)
                {
                    return button.gameObject;
                }
            }

            return null;
        }

        // ---- Internals ----

        private void HandleCodeSubmitted(string text) => CodeSubmitted?.Invoke();

        private void HandleNameEndEdit(string text)
        {
            _nameEndEditFrame = Time.frameCount;
            NameEdited?.Invoke(text);
        }

        // On a visibility change of BTN_Rejoin only: Join and Back go through it while shown and skip it while hidden
        // (Navigation is a struct: no allocation). BTN_Rejoin's own Up / Down (Join / Back) live in the prefab.
        private void RewireHubNavigation(bool rejoinVisible)
        {
            UnityEngine.UI.Selectable rejoin = rejoinVisible ? _rejoinButton : null;
            if (_hubJoinButton != null)
            {
                UnityEngine.UI.Navigation navigation = _hubJoinButton.navigation;
                if (navigation.mode == UnityEngine.UI.Navigation.Mode.Explicit)
                {
                    navigation.selectOnDown = rejoin != null ? rejoin : _hubBackButton;
                    _hubJoinButton.navigation = navigation;
                }
            }

            if (_hubBackButton != null)
            {
                UnityEngine.UI.Navigation navigation = _hubBackButton.navigation;
                if (navigation.mode == UnityEngine.UI.Navigation.Mode.Explicit)
                {
                    navigation.selectOnUp = rejoin != null ? rejoin : _hubJoinButton;
                    _hubBackButton.navigation = navigation;
                }
            }
        }

        // On second or language change only. The localized format is the TMP template itself ({0} = seconds).
        private void RefreshRejoin()
        {
            string format = _rejoinBinding?.Value;
            if (_rejoinLabel == null || format == null || !_rejoinVisible || _rejoinSeconds < 0)
            {
                return;
            }

            _rejoinLabel.SetText(format, _rejoinSeconds);
        }

        private void HandleCardClicked(CharacterDefinition character) => CardClicked?.Invoke(character);

        private void HandleCardSelected(CharacterDefinition character) => CardSelected?.Invoke(character);

        // Swapping the entry refreshes the bound string (LocalizedReference.ForceUpdate) when it differs.
        private static void SetEntry(LocalizedString entry, string key)
        {
            if (entry != null && !string.Equals(entry.TableEntryReference.Key, key, StringComparison.Ordinal))
            {
                entry.TableEntryReference = key;
            }
        }

        private void RefreshError() => Write(_error, _errorBinding);

        private void RefreshBusy() => Write(_busyLabel, _busyBinding);

        private void RefreshHint() => Write(_hint, _hintBinding);

        // On name or language change only.
        private void RefreshSignedIn()
        {
            string format = _signedInBinding?.Value;
            if (_signedInAs == null || format == null || string.IsNullOrEmpty(_signedInName))
            {
                return;
            }

            _signedInAs.SetText(string.Format(format, _signedInName));
        }

        private static void Write(TMP_Text target, LocalizedStringBinding binding)
        {
            string value = binding?.Value;
            if (target != null && value != null)
            {
                target.SetText(value);
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }

        private void OnValidate()
        {
            if (_errorEntry == null || _errorEntry.IsEmpty || _busyEntry == null || _busyEntry.IsEmpty
                || _signedInEntry == null || _signedInEntry.IsEmpty || _hintEntry == null || _hintEntry.IsEmpty
                || _rejoinEntry == null || _rejoinEntry.IsEmpty)
            {
                Debug.LogWarning($"[{nameof(OnlinePanelView)}] Empty localized string on '{name}'.", this);
            }

            if (_panelRoot == null)
            {
                Debug.LogWarning($"[{nameof(OnlinePanelView)}] Missing panel root on '{name}'.", this);
            }
            else if (_panelRoot == gameObject || transform.IsChildOf(_panelRoot.transform))
            {
                Debug.LogWarning($"[{nameof(OnlinePanelView)}] Panel root on '{name}' contains this view.", this);
            }

            if (_hubRoot == null || _joinRoot == null || _roomRoot == null || _busyRoot == null || _errorRoot == null)
            {
                Debug.LogWarning($"[{nameof(OnlinePanelView)}] Missing a screen root or the error root on '{name}'.", this);
            }

            if (_hubFirst == null || _joinFirst == null || _roomFirst == null || _busyFirst == null || _roomLeave == null)
            {
                Debug.LogWarning($"[{nameof(OnlinePanelView)}] Missing a first selected or the leave button on '{name}'.", this);
            }

            if (_signedInAs == null || _code == null || _hint == null || _busyLabel == null || _error == null || _codeInput == null)
            {
                Debug.LogWarning($"[{nameof(OnlinePanelView)}] Missing a text or the code input on '{name}'.", this);
            }

            if (_cards == null || _cards.Length == 0 || _cardButtons == null || _cardButtons.Length != _cards.Length
                || _seats == null || _seats.Length != OnlineLobbyChannel.MaxSeats)
            {
                Debug.LogWarning($"[{nameof(OnlinePanelView)}] '{name}' needs one button per card and {OnlineLobbyChannel.MaxSeats} seats.", this);
            }

            if (_rejoinButton == null || _rejoinLabel == null || _nameInput == null)
            {
                Debug.LogWarning($"[{nameof(OnlinePanelView)}] Missing BTN_Rejoin, its label or INP_Name on '{name}'.", this);
            }
            else if (_rejoinLabel.TryGetComponent(out LocalizedText _))
            {
                Debug.LogWarning($"[{nameof(OnlinePanelView)}] The rejoin label on '{name}' is written from code: remove its LocalizedText.", this);
            }

            if (_hubJoinButton == null || _hubBackButton == null)
            {
                Debug.LogWarning($"[{nameof(OnlinePanelView)}] Missing the hub Join or Back button on '{name}': their navigation does not follow BTN_Rejoin.", this);
            }

            if (_startButton == null || _spectatorRow == null || _spectatorName == null || _spectatorBanner == null)
            {
                Debug.LogWarning($"[{nameof(OnlinePanelView)}] Missing start button or spectator objects on '{name}'.", this);
            }
        }
    }
}
