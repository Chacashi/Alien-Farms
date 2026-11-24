using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Networking.Data;
using AlienFarms.UI;
using UnityEngine;

namespace AlienFarms.Networking.Player
{
    /// <summary>
    /// Drives the <see cref="NameTagView"/> of a Player_Net avatar (online only; Player.prefab has no tag):
    /// <list type="bullet">
    /// <item>Name: the replicated display name, else the seat tag (<see cref="CarriableOwnerRules.SeatTag"/>).</item>
    /// <item>Color: the character's Primary theme color.</item>
    /// <item>Hidden over this machine's own avatar and while <see cref="NetworkSettings.ShowNameTags"/> is off.</item>
    /// <item>Debug line "client N" while <see cref="NetworkSettings.NameTagShowsClientId"/> is on.</item>
    /// <item>Faces <see cref="NetworkRuntimeAnchor.ViewCamera"/> (local avatar camera).</item>
    /// </list>
    /// Update only compares cached values (seat, character, owner client, settings, camera reference) and pushes what
    /// changed to the view: no allocation per frame. The name is refreshed on <see cref="NetworkPlayer.DisplayNameChanged"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkPlayer))]
    public sealed class NetworkNameTag : MonoBehaviour
    {
        private const int Unknown = int.MinValue;

        [Header("References")]
        [SerializeField, Tooltip("Network identity on this root.")]
        private NetworkPlayer _player;

        [SerializeField, Tooltip("Tag view on the NameTag child.")]
        private NameTagView _view;

        [SerializeField, Tooltip("Runtime anchor: provides the camera this machine views through.")]
        private NetworkRuntimeAnchor _anchor;

        [SerializeField, Tooltip("Network settings (ShowNameTags, NameTagShowsClientId). Without it the tag shows, without the debug line.")]
        private NetworkSettings _settings;

        [Header("Look")]
        [SerializeField, Tooltip("Tag color while the character is unknown.")]
        private Color _fallbackColor = Color.white;

        private Camera _shownCamera;
        private PlayerId _shownSeat;
        private int _shownCharacter = Unknown;
        private ulong _shownClientId;
        private bool _shownClientDetail;
        private bool _hasShownDetail;
        private bool _shownVisible;
        private bool _hasShownVisible;
        private bool _nameDirty = true;

        private void Awake()
        {
            if (_player == null)
            {
                TryGetComponent(out _player);
            }
        }

        private void OnEnable()
        {
            if (_player != null)
            {
                _player.DisplayNameChanged += HandleDisplayNameChanged;
            }

            _hasShownVisible = false;
            _nameDirty = true;
        }

        private void OnDisable()
        {
            if (_player != null)
            {
                _player.DisplayNameChanged -= HandleDisplayNameChanged;
            }
        }

        private void Update()
        {
            if (_player == null || _view == null)
            {
                return;
            }

            bool spawned = _player.IsSpawned;
            PlayerId seat = spawned ? _player.PlayerId : PlayerId.None;
            bool showTags = _settings == null || _settings.ShowNameTags;
            bool visible = NameTagRules.IsVisible(spawned, showTags, spawned && _player.IsOwner, seat);
            if (!_hasShownVisible || visible != _shownVisible)
            {
                _hasShownVisible = true;
                _shownVisible = visible;
                _view.SetVisible(visible);
                if (!visible)
                {
                    // Pushed again in full the next time it shows (respawn, setting turned back on).
                    ForgetShownState();
                }
            }

            if (!visible)
            {
                return;
            }

            Camera camera = _anchor != null ? _anchor.ViewCamera : null;
            if (!ReferenceEquals(camera, _shownCamera))
            {
                _shownCamera = camera;
                _view.SetViewCamera(camera);
            }

            if (_nameDirty || seat != _shownSeat)
            {
                _nameDirty = false;
                _shownSeat = seat;
                _view.SetName(NameTagRules.ResolveName(_player.DisplayNameText, seat));
            }

            int character = _player.CharacterIndex;
            if (character != _shownCharacter)
            {
                _shownCharacter = character;
                _view.SetColor(NameTagRules.ResolveColor(_player.Character, _fallbackColor));
            }

            bool showClient = _settings != null && _settings.NameTagShowsClientId;
            ulong clientId = _player.OwnerClientId;
            if (!_hasShownDetail || showClient != _shownClientDetail || (showClient && clientId != _shownClientId))
            {
                _hasShownDetail = true;
                _shownClientDetail = showClient;
                _shownClientId = clientId;
                _view.SetDetail(NameTagRules.ClientDetail(showClient, clientId));
            }
        }

        private void HandleDisplayNameChanged(NetworkPlayer player)
        {
            _nameDirty = true;
        }

        private void ForgetShownState()
        {
            _shownCamera = null;
            _shownSeat = PlayerId.None;
            _shownCharacter = Unknown;
            _hasShownDetail = false;
            _nameDirty = true;
        }

        private void OnValidate()
        {
            if (_player == null)
            {
                TryGetComponent(out _player);
            }

            if (_view == null)
            {
                Debug.LogWarning($"[{nameof(NetworkNameTag)}] Missing NameTagView on '{name}'.", this);
            }
            else if (_view.transform == transform)
            {
                Debug.LogWarning($"[{nameof(NetworkNameTag)}] The NameTagView on '{name}' must be on a child: the billboard would rotate the avatar.", this);
            }

            if (_anchor == null)
            {
                Debug.LogWarning($"[{nameof(NetworkNameTag)}] Missing NetworkRuntimeAnchor on '{name}'; the tag cannot face the camera.", this);
            }

            if (_settings == null)
            {
                Debug.LogWarning($"[{nameof(NetworkNameTag)}] Missing NetworkSettings on '{name}'.", this);
            }
        }
    }
}
