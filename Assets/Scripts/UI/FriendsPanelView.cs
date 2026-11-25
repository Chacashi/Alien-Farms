using System;
using AlienFarms.Core;
using AlienFarms.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AlienFarms.UI
{
    [DisallowMultipleComponent]
    public sealed class FriendsPanelView : MonoBehaviour
    {
        [SerializeField] private FriendsChannel _friends;
        [SerializeField] private TMP_Text _localPlayerId;
        [SerializeField] private Button _copyId;
        [SerializeField] private TMP_InputField _friendId;
        [SerializeField] private Button _addFriend;
        [SerializeField] private Button _refresh;
        [SerializeField] private FriendEntryView[] _friendRows = Array.Empty<FriendEntryView>();
        [SerializeField] private FriendRequestEntryView[] _requestRows = Array.Empty<FriendRequestEntryView>();
        [SerializeField] private TMP_Text _feedback;

        private readonly FriendsLocalizedText _feedbackText = new();

        private bool HasReferences => _friends != null && _localPlayerId != null && _copyId != null &&
            _friendId != null && _addFriend != null && _feedback != null;

        private void OnValidate()
        {
            if (!HasReferences)
            {
                Debug.LogWarning($"[{nameof(FriendsPanelView)}] Missing references on '{name}'.", this);
            }
        }

        private void OnEnable()
        {
            if (!HasReferences)
            {
                return;
            }

            _friends.Changed += Refresh;
            _copyId.onClick.AddListener(CopyId);
            _friendId.onValueChanged.AddListener(HandleInputChanged);
            _addFriend.onClick.AddListener(AddFriend);
            if (_refresh != null)
            {
                _refresh.onClick.AddListener(RefreshList);
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (!HasReferences)
            {
                return;
            }

            _friends.Changed -= Refresh;
            _copyId.onClick.RemoveListener(CopyId);
            _friendId.onValueChanged.RemoveListener(HandleInputChanged);
            _addFriend.onClick.RemoveListener(AddFriend);
            if (_refresh != null)
            {
                _refresh.onClick.RemoveListener(RefreshList);
            }

            _feedbackText.Clear();
        }

        private void Refresh()
        {
            bool ready = _friends.Status == FriendsServiceStatus.Ready;
            _localPlayerId.text = _friends.LocalPlayerId ?? string.Empty;
            _copyId.interactable = !string.IsNullOrEmpty(_friends.LocalPlayerId);
            _friendId.interactable = ready;
            HandleInputChanged(_friendId.text);
            if (_refresh != null)
            {
                _refresh.interactable = ready;
            }

            for (int i = 0; i < _friendRows.Length; i++)
            {
                FriendEntryView row = _friendRows[i];
                if (row != null)
                {
                    bool visible = i < _friends.FriendCount;
                    if (visible)
                    {
                        row.Bind(_friends, _friends.GetFriend(i));
                    }

                    row.gameObject.SetActive(visible);
                }
            }

            for (int i = 0; i < _requestRows.Length; i++)
            {
                FriendRequestEntryView row = _requestRows[i];
                if (row != null)
                {
                    bool visible = i < _friends.RequestCount;
                    if (visible)
                    {
                        row.Bind(_friends, _friends.GetRequest(i));
                    }

                    row.gameObject.SetActive(visible);
                }
            }

            string fallback = _friends.Status switch
            {
                FriendsServiceStatus.Off => LocalizationKeys.FriendsErrorNotEnabled,
                FriendsServiceStatus.Initializing => LocalizationKeys.FriendsStatusInitializing,
                FriendsServiceStatus.Error => LocalizationKeys.FriendsErrorUnknown,
                _ => null
            };
            _feedbackText.Show(_feedback, _friends.LastErrorKey ?? _friends.LastInfoKey ?? fallback);
        }

        private void HandleInputChanged(string value) => _addFriend.interactable =
            _friends.Status == FriendsServiceStatus.Ready && !string.IsNullOrWhiteSpace(value);

        private void CopyId() => GUIUtility.systemCopyBuffer = _friends.LocalPlayerId;
        private void AddFriend() => _friends.RequestAddFriend(_friendId.text);
        private void RefreshList() => _friends.RequestRefresh();
    }
}
