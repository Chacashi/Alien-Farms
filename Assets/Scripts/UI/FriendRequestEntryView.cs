using AlienFarms.Core;
using AlienFarms.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AlienFarms.UI
{
    [DisallowMultipleComponent]
    public sealed class FriendRequestEntryView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _displayName;
        [SerializeField] private Button _accept;
        [SerializeField] private Button _decline;
        [SerializeField] private Button _cancel;

        private FriendsChannel _friends;
        private FriendRequestEntry _entry;

        internal void Bind(FriendsChannel friends, FriendRequestEntry entry)
        {
            _friends = friends;
            _entry = entry;
            Refresh();
        }

        private bool HasReferences => _displayName != null && _accept != null && _decline != null && _cancel != null;

        private void OnValidate()
        {
            if (!HasReferences)
            {
                Debug.LogWarning($"[{nameof(FriendRequestEntryView)}] Missing references on '{name}'.", this);
            }
        }

        private void OnEnable()
        {
            if (!HasReferences)
            {
                return;
            }

            _accept.onClick.AddListener(Accept);
            _decline.onClick.AddListener(Decline);
            _cancel.onClick.AddListener(Cancel);
            Refresh();
        }

        private void OnDisable()
        {
            if (!HasReferences)
            {
                return;
            }

            _accept.onClick.RemoveListener(Accept);
            _decline.onClick.RemoveListener(Decline);
            _cancel.onClick.RemoveListener(Cancel);
        }

        private void Refresh()
        {
            if (!HasReferences)
            {
                return;
            }

            _displayName.richText = false;
            _displayName.text = string.IsNullOrWhiteSpace(_entry.DisplayName) ? _entry.UgsPlayerId : _entry.DisplayName;
            _accept.gameObject.SetActive(_entry.Incoming);
            _decline.gameObject.SetActive(_entry.Incoming);
            _cancel.gameObject.SetActive(!_entry.Incoming);
            bool ready = _friends != null && _friends.Status == FriendsServiceStatus.Ready;
            _accept.interactable = ready;
            _decline.interactable = ready;
            _cancel.interactable = ready;
        }

        private void Accept() => _friends?.RequestAcceptRequest(_entry.UgsPlayerId);
        private void Decline() => _friends?.RequestDeclineRequest(_entry.UgsPlayerId);
        private void Cancel() => _friends?.RequestCancelOutgoing(_entry.UgsPlayerId);
    }
}
