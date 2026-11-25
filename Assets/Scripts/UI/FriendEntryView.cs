using AlienFarms.Core;
using AlienFarms.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AlienFarms.UI
{
    [DisallowMultipleComponent]
    public sealed class FriendEntryView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _displayName;
        [SerializeField] private Image _presenceDot;
        [SerializeField] private TMP_Text _presence;
        [SerializeField] private TMP_Text _activity;
        [SerializeField] private Button _invite;
        [SerializeField] private Color _onlineColor = new Color(0.25f, 0.8f, 0.4f);
        [SerializeField] private Color _busyColor = new Color(0.9f, 0.3f, 0.3f);
        [SerializeField] private Color _awayColor = new Color(0.95f, 0.7f, 0.2f);
        [SerializeField] private Color _offlineColor = Color.gray;

        private readonly FriendsLocalizedText _presenceText = new();
        private readonly FriendsLocalizedText _activityText = new();
        private FriendsChannel _friends;
        private FriendEntry _entry;

        internal void Bind(FriendsChannel friends, FriendEntry entry)
        {
            _friends = friends;
            _entry = entry;
            if (isActiveAndEnabled)
            {
                Refresh();
            }
        }

        private bool HasReferences => _displayName != null && _presenceDot != null && _presence != null && _activity != null && _invite != null;

        private void OnValidate()
        {
            if (!HasReferences)
            {
                Debug.LogWarning($"[{nameof(FriendEntryView)}] Missing references on '{name}'.", this);
            }
        }

        private void OnEnable()
        {
            if (!HasReferences)
            {
                return;
            }

            _invite.onClick.AddListener(Invite);
            Refresh();
        }

        private void OnDisable()
        {
            if (!HasReferences)
            {
                return;
            }

            _invite.onClick.RemoveListener(Invite);
            _presenceText.Clear();
            _activityText.Clear();
        }

        private void Update()
        {
            if (_invite != null)
            {
                RefreshInvite();
            }
        }

        private void Refresh()
        {
            if (!HasReferences)
            {
                return;
            }

            _displayName.richText = false;
            _displayName.text = string.IsNullOrWhiteSpace(_entry.DisplayName) ? _entry.UgsPlayerId : _entry.DisplayName;
            var presence = _entry.Availability switch
            {
                FriendAvailability.Online => (LocalizationKeys.FriendsPresenceOnline, _onlineColor),
                FriendAvailability.Busy => (LocalizationKeys.FriendsPresenceBusy, _busyColor),
                FriendAvailability.Away => (LocalizationKeys.FriendsPresenceAway, _awayColor),
                FriendAvailability.Invisible => (LocalizationKeys.FriendsPresenceInvisible, _offlineColor),
                FriendAvailability.Offline => (LocalizationKeys.FriendsPresenceOffline, _offlineColor),
                _ => (LocalizationKeys.FriendsPresenceUnknown, _offlineColor)
            };
            _presenceDot.color = presence.Item2;
            _presenceText.Show(_presence, presence.Item1);
            _activityText.Show(_activity, _entry.ActivityKey switch
            {
                FriendActivityKeys.Hub => LocalizationKeys.FriendsActivityHub,
                FriendActivityKeys.Room => LocalizationKeys.FriendsActivityRoom,
                FriendActivityKeys.Match => LocalizationKeys.FriendsActivityMatch,
                _ => null
            });
            RefreshInvite();
        }

        private void RefreshInvite()
        {
            bool eligible = _friends != null && _friends.CanInvite(_entry);
            if (_invite.interactable != eligible)
            {
                _invite.interactable = eligible;
            }
        }

        private void Invite() => _friends?.RequestInvite(_entry.UgsPlayerId);
    }
}
