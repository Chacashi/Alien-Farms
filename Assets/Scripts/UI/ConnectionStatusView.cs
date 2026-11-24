using AlienFarms.Core;
using AlienFarms.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace AlienFarms.UI
{
    /// <summary>
    /// HUD connection banner (online). Listens to <see cref="GameEvents.OnPlayerConnectionChanged"/>:
    /// <list type="bullet">
    /// <item>Reconnecting: "Waiting for P2 · {character}: n s", counted down locally on unscaled time. The template is
    /// built once per state change and the number is written with <c>SetText(format, float)</c> only when the whole
    /// second changes (no allocation per frame).</item>
    /// <item>Left / TimedOut / back after a drop: a toast that hides after <see cref="_toastSeconds"/>, then the banner
    /// returns to any seat that is still reserved.</item>
    /// </list>
    /// Character names come from <see cref="GameEvents.OnPlayerCharacterAssigned"/> (kept after the avatar despawns).
    /// Lives on an always-active object and toggles <see cref="_root"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ConnectionStatusView : MonoBehaviour
    {
        private const int MaxPlayers = 2;
        private const string SecondsPlaceholder = "{0}";
        private const string CharacterSeparator = " · ";

        [Header("Texts")]
        [SerializeField, Tooltip("'Waiting for {0}: {1} s' ({0} player, {1} seconds; non-Smart).")]
        private LocalizedString _reconnecting = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.OnlineStatusReconnecting);

        [SerializeField, Tooltip("'{0} is back' toast.")]
        private LocalizedString _returned = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.OnlineStatusReturned);

        [SerializeField, Tooltip("'{0} left' toast.")]
        private LocalizedString _left = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.OnlineStatusLeft);

        [SerializeField, Tooltip("'{0} did not come back' toast.")]
        private LocalizedString _timedOut = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.OnlineStatusTimedOut);

        [SerializeField, Tooltip("Player 1 label.")]
        private LocalizedString _player1Label = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.CommonP1);

        [SerializeField, Tooltip("Player 2 label.")]
        private LocalizedString _player2Label = new LocalizedString(LocalizationKeys.UiTable, LocalizationKeys.CommonP2);

        [Header("Look")]
        [SerializeField, Min(0.5f), Tooltip("Seconds a toast (back / left / did not come back) stays on screen.")]
        private float _toastSeconds = 3f;

        [Header("References")]
        [SerializeField, Tooltip("Banner root, hidden while there is nothing to show. Must not contain this component.")]
        private GameObject _root;

        [SerializeField, Tooltip("Banner text.")]
        private TMP_Text _label;

        private readonly PlayerCharacterCache _characters = new PlayerCharacterCache();
        private readonly bool[] _awaiting = new bool[MaxPlayers];
        private readonly float[] _graceEnd = new float[MaxPlayers];

        private LocalizedStringBinding _reconnectingBinding;
        private LocalizedStringBinding _returnedBinding;
        private LocalizedStringBinding _leftBinding;
        private LocalizedStringBinding _timedOutBinding;
        private LocalizedStringBinding _player1Binding;
        private LocalizedStringBinding _player2Binding;

        private ConnectionBanner _shown;
        private PlayerId _shownPlayer = PlayerId.None;
        private string _template;
        private int _shownSeconds = -1;
        private float _toastHideAt;

        /// <summary>Banner currently on screen (None while hidden).</summary>
        public ConnectionBanner Shown => _shown;

        private void Awake()
        {
            System.Action refresh = RefreshTemplate;
            _reconnectingBinding = new LocalizedStringBinding(_reconnecting, refresh);
            _returnedBinding = new LocalizedStringBinding(_returned, refresh);
            _leftBinding = new LocalizedStringBinding(_left, refresh);
            _timedOutBinding = new LocalizedStringBinding(_timedOut, refresh);
            _player1Binding = new LocalizedStringBinding(_player1Label, refresh);
            _player2Binding = new LocalizedStringBinding(_player2Label, refresh);
            SetRootActive(false);
        }

        private void OnEnable()
        {
            _reconnectingBinding.Enable();
            _returnedBinding.Enable();
            _leftBinding.Enable();
            _timedOutBinding.Enable();
            _player1Binding.Enable();
            _player2Binding.Enable();
            GameEvents.OnPlayerConnectionChanged += HandleConnectionChanged;
            GameEvents.OnPlayerCharacterAssigned += HandleCharacterAssigned;
        }

        private void OnDisable()
        {
            _reconnectingBinding.Disable();
            _returnedBinding.Disable();
            _leftBinding.Disable();
            _timedOutBinding.Disable();
            _player1Binding.Disable();
            _player2Binding.Disable();
            GameEvents.OnPlayerConnectionChanged -= HandleConnectionChanged;
            GameEvents.OnPlayerCharacterAssigned -= HandleCharacterAssigned;
        }

        private void Update()
        {
            if (_shown == ConnectionBanner.None)
            {
                return;
            }

            if (_shown == ConnectionBanner.Reconnecting)
            {
                WriteText(false);
                return;
            }

            if (Time.unscaledTime >= _toastHideAt)
            {
                ShowNextAwaitingOrHide();
            }
        }

        private void HandleConnectionChanged(PlayerId id, PlayerConnectionStatus status, float graceLeft, bool isLocalSeat)
        {
            int index = ToIndex(id);
            if (index < 0)
            {
                return;
            }

            ConnectionBanner banner = ConnectionStatusRules.Resolve(status, graceLeft, _awaiting[index]);
            _awaiting[index] = ConnectionStatusRules.IsAwaiting(status);
            if (_awaiting[index])
            {
                _graceEnd[index] = Time.unscaledTime + Mathf.Max(0f, graceLeft);
            }

            if (banner != ConnectionBanner.None)
            {
                Show(banner, id);
            }
        }

        private void HandleCharacterAssigned(PlayerId id, CharacterDefinition character)
        {
            _characters.Set(id, character);
            if (_shown != ConnectionBanner.None && id == _shownPlayer)
            {
                RefreshTemplate();
            }
        }

        private void Show(ConnectionBanner banner, PlayerId id)
        {
            _shown = banner;
            _shownPlayer = id;
            if (ConnectionStatusRules.IsToast(banner))
            {
                _toastHideAt = Time.unscaledTime + _toastSeconds;
            }

            SetRootActive(true);
            RefreshTemplate();
        }

        // After a toast: back to the countdown of a seat that is still reserved, else hidden.
        private void ShowNextAwaitingOrHide()
        {
            for (int i = 0; i < MaxPlayers; i++)
            {
                if (_awaiting[i])
                {
                    Show(ConnectionBanner.Reconnecting, (PlayerId)(i + 1));
                    return;
                }
            }

            _shown = ConnectionBanner.None;
            _shownPlayer = PlayerId.None;
            _template = null;
            SetRootActive(false);
        }

        /// <summary>Rebuilds the template of the shown banner (state, character or language change only) and writes it.</summary>
        private void RefreshTemplate()
        {
            if (_shown == ConnectionBanner.None)
            {
                return;
            }

            string format = GetFormat(_shown);
            if (format == null)
            {
                // Not resolved yet: the binding calls back when it is.
                _template = null;
                return;
            }

            string player = BuildPlayerText(_shownPlayer);
            _template = _shown == ConnectionBanner.Reconnecting
                ? string.Format(format, ConnectionStatusRules.EscapeForTemplate(player), SecondsPlaceholder)
                : string.Format(format, player);
            WriteText(true);
        }

        private void WriteText(bool force)
        {
            if (_label == null || _template == null)
            {
                return;
            }

            if (_shown != ConnectionBanner.Reconnecting)
            {
                if (force)
                {
                    _label.SetText(_template);
                }

                return;
            }

            int index = ToIndex(_shownPlayer);
            float left = index >= 0 ? _graceEnd[index] - Time.unscaledTime : 0f;
            int seconds = ConnectionStatusRules.DisplaySeconds(left);
            if (force || seconds != _shownSeconds)
            {
                _shownSeconds = seconds;
                _label.SetText(_template, seconds);
            }
        }

        private string GetFormat(ConnectionBanner banner)
        {
            switch (banner)
            {
                case ConnectionBanner.Reconnecting:
                    return _reconnectingBinding.Value;
                case ConnectionBanner.Returned:
                    return _returnedBinding.Value;
                case ConnectionBanner.Left:
                    return _leftBinding.Value;
                case ConnectionBanner.TimedOut:
                    return _timedOutBinding.Value;
                default:
                    return null;
            }
        }

        // Allocates: called on state, character or language changes only.
        private string BuildPlayerText(PlayerId id)
        {
            string label = id == PlayerId.Player2 ? _player2Binding.Value : _player1Binding.Value;
            if (string.IsNullOrEmpty(label))
            {
                label = id == PlayerId.Player2 ? "P2" : "P1";
            }

            CharacterDefinition character = _characters.Get(id);
            return character != null && !string.IsNullOrEmpty(character.DisplayName)
                ? label + CharacterSeparator + character.DisplayName
                : label;
        }

        private void SetRootActive(bool active)
        {
            if (_root != null && _root.activeSelf != active)
            {
                _root.SetActive(active);
            }
        }

        private static int ToIndex(PlayerId id)
        {
            int index = (int)id - 1;
            return index >= 0 && index < MaxPlayers ? index : -1;
        }

        private void OnValidate()
        {
            if (_reconnecting == null || _reconnecting.IsEmpty || _returned == null || _returned.IsEmpty
                || _left == null || _left.IsEmpty || _timedOut == null || _timedOut.IsEmpty
                || _player1Label == null || _player1Label.IsEmpty || _player2Label == null || _player2Label.IsEmpty)
            {
                Debug.LogWarning($"[{nameof(ConnectionStatusView)}] Empty localized string on '{name}'.", this);
            }

            if (_root == null || _label == null)
            {
                Debug.LogWarning($"[{nameof(ConnectionStatusView)}] Missing root or label on '{name}'.", this);
            }
            else if (_root == gameObject || transform.IsChildOf(_root.transform))
            {
                Debug.LogWarning($"[{nameof(ConnectionStatusView)}] Root on '{name}' contains this view: it could never show again.", this);
            }
        }
    }
}
