using System;
using System.Globalization;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AlienFarms.UI
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class InviteToastView : MonoBehaviour
    {
        [SerializeField] private FriendsChannel _friends;
        [SerializeField] private FriendsMenuController _controller;
        [SerializeField, Tooltip("Child containing the toast. Keep this view outside it.")]
        private GameObject _content;
        [SerializeField] private TMP_Text _message;
        [SerializeField] private TMP_Text _countdown;
        [SerializeField] private Button _accept;
        [SerializeField] private Button _decline;

        private readonly FriendsLocalizedText _messageText = new();
        private readonly char[] _countdownBuffer = new char[16];
        private EventSystem _focusedSystem;
        private GameObject _previousSelection;
        private GameObject _previousRoot;
        private int _shownSeconds = -1;

        private bool HasReferences => _friends != null && _controller != null && _content != null &&
            _message != null && _countdown != null && _accept != null && _decline != null;

        private void OnValidate()
        {
            if (!HasReferences || _content == gameObject)
            {
                Debug.LogWarning($"[{nameof(InviteToastView)}] Missing references or invalid content on '{name}'.", this);
            }
        }

        private void OnEnable()
        {
            if (!HasReferences || _content == gameObject)
            {
                return;
            }

            _message.richText = false;
            _friends.Changed += Refresh;
            _accept.onClick.AddListener(_controller.AcceptInvite);
            _decline.onClick.AddListener(_controller.DeclineInvite);
            Refresh();
        }

        private void OnDisable()
        {
            if (!HasReferences || _content == gameObject)
            {
                return;
            }

            _friends.Changed -= Refresh;
            _accept.onClick.RemoveListener(_controller.AcceptInvite);
            _decline.onClick.RemoveListener(_controller.DeclineInvite);
            Hide();
        }

        private bool ShouldShow => _controller.CanShowInvite &&
            (_friends.HasInvite || _controller.ToastErrorKey != null);

        private void Refresh()
        {
            if (!ShouldShow)
            {
                Hide();
                return;
            }

            string errorKey = _controller.ToastErrorKey;
            _accept.interactable = errorKey == null;
            if (errorKey != null)
            {
                _messageText.Show(_message, errorKey);
                _countdown.SetCharArray(_countdownBuffer, 0, 0);
                _shownSeconds = -1;
                _content.SetActive(true);
                return;
            }

            SessionInviteOffer offer = _friends.PendingInvite;
            string sender = string.IsNullOrWhiteSpace(offer.FromName) ? offer.FromUgsPlayerId : offer.FromName;
            _messageText.Show(_message, LocalizationKeys.FriendsLabelInviteOffer, sender);
            _content.SetActive(true);
            RefreshCountdown(offer);
        }

        private void LateUpdate()
        {
            if (!HasReferences || _content == gameObject)
            {
                return;
            }

            if (_content.activeSelf != ShouldShow)
            {
                Refresh();
            }

            if (_content.activeSelf)
            {
                if (_controller.ToastErrorKey == null)
                {
                    RefreshCountdown(_friends.PendingInvite);
                }

                Focus();
            }
        }

        private void RefreshCountdown(SessionInviteOffer offer)
        {
            int seconds = (int)Math.Clamp(Math.Ceiling(offer.ExpiresAtRealtime - Time.realtimeSinceStartupAsDouble),
                0d, int.MaxValue);
            if (_shownSeconds == seconds)
            {
                return;
            }

            _shownSeconds = seconds;
            seconds.TryFormat(_countdownBuffer.AsSpan(), out int length, provider: CultureInfo.InvariantCulture);
            _countdown.SetCharArray(_countdownBuffer, 0, length);
        }

        private void Focus()
        {
            EventSystem eventSystem = _controller.MenuEventSystem;
            if (eventSystem == null && EventSystem.current is not MultiplayerEventSystem)
            {
                eventSystem = EventSystem.current;
            }
            if (_focusedSystem != eventSystem)
            {
                ReleaseFocus();
            }

            if (eventSystem == null || !eventSystem.isActiveAndEnabled || eventSystem.alreadySelecting)
            {
                return;
            }

            _focusedSystem = eventSystem;
            if (eventSystem is MultiplayerEventSystem multiplayer && multiplayer.playerRoot != _content)
            {
                _previousRoot = multiplayer.playerRoot;
                multiplayer.playerRoot = _content;
            }

            GameObject selected = eventSystem.currentSelectedGameObject;
            if (!OwnsSelection(selected))
            {
                _previousSelection = selected;
                eventSystem.SetSelectedGameObject(_accept.interactable ? _accept.gameObject : _decline.gameObject);
            }
            else if (!_accept.interactable && selected == _accept.gameObject)
            {
                eventSystem.SetSelectedGameObject(_decline.gameObject);
            }
        }

        private bool OwnsSelection(GameObject selected) =>
            selected != null && selected.transform.IsChildOf(_content.transform);

        private void ReleaseFocus()
        {
            if (_focusedSystem != null)
            {
                if (_focusedSystem is MultiplayerEventSystem multiplayer && multiplayer.playerRoot == _content)
                {
                    multiplayer.playerRoot = _previousRoot;
                }

                if (!_focusedSystem.alreadySelecting && OwnsSelection(_focusedSystem.currentSelectedGameObject))
                {
                    _focusedSystem.SetSelectedGameObject(
                        _previousSelection != null && _previousSelection.activeInHierarchy ? _previousSelection : null);
                }
            }

            _focusedSystem = null;
            _previousRoot = null;
            _previousSelection = null;
        }

        private void Hide()
        {
            ReleaseFocus();
            _content.SetActive(false);
            _messageText.Clear();
            _shownSeconds = -1;
        }
    }
}
