using AlienFarms.Core;
using AlienFarms.Input;
using UnityEngine;

namespace AlienFarms.Player
{
    /// <summary>
    /// Routes a shared menu (pause, results) to a single owner: the owner navigates it with its own event system while
    /// every other joined player is blocked. Also applies the game pause to every player. Players joining while a menu
    /// is routed or the game is paused get the same state.
    /// Players are looked up through an <see cref="IPlayerRigLookup"/>: the serialized join coordinator unless another
    /// lookup is injected with <see cref="SetPlayers"/> before Awake (online: only the local avatar).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerMenuRouter : MonoBehaviour
    {
        private static readonly PlayerId[] Players = { PlayerId.Player1, PlayerId.Player2 };

        [Header("References")]
        [SerializeField, Tooltip("Registry of joined players (local mode); default lookup unless another one is injected.")]
        private PlayerJoinCoordinator _players;

        private IPlayerRigLookup _lookup;
        private PlayerId _menuOwner = PlayerId.None;
        private bool _paused;

        /// <summary>Player currently navigating a routed menu (None if no menu is routed).</summary>
        public PlayerId MenuOwner => _menuOwner;

        private void Awake()
        {
            // A lookup injected before Awake wins. Unity null check before boxing to the interface.
            if (_lookup == null && _players != null)
            {
                _lookup = _players;
            }
        }

        /// <summary>Replaces where players are looked up (e.g. the online local-avatar lookup). Call it before Awake.</summary>
        public void SetPlayers(IPlayerRigLookup lookup)
        {
            _lookup = lookup;
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerJoined += HandlePlayerJoined;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerJoined -= HandlePlayerJoined;
        }

        /// <summary>
        /// <paramref name="owner"/> enters the menu under <paramref name="root"/> with <paramref name="firstSelected"/>
        /// focused; the other players are blocked. Replaces any previously routed menu.
        /// Returns false (and changes nothing) when <paramref name="owner"/> has no joined rig with menu input.
        /// </summary>
        public bool RouteMenu(PlayerId owner, GameObject root, GameObject firstSelected)
        {
            if (!TryGetMenu(owner, out _))
            {
                return false;
            }

            _menuOwner = owner;
            for (int i = 0; i < Players.Length; i++)
            {
                if (!TryGetMenu(Players[i], out PlayerMenuInput menu))
                {
                    continue;
                }

                if (Players[i] == owner)
                {
                    menu.EnterMenu(root, firstSelected);
                }
                else
                {
                    menu.ExitMenu();
                    menu.SetBlocked(true);
                }
            }

            return true;
        }

        /// <summary>Every player leaves its menu and gets its input back.</summary>
        public void ReleaseMenus()
        {
            _menuOwner = PlayerId.None;
            for (int i = 0; i < Players.Length; i++)
            {
                if (TryGetMenu(Players[i], out PlayerMenuInput menu))
                {
                    menu.ExitMenu();
                    menu.SetBlocked(false);
                }
            }
        }

        /// <summary>Tells every player rig the game is (un)paused (keeps carried objects across the map switch).</summary>
        public void SetPlayersPaused(bool paused)
        {
            _paused = paused;
            if (_lookup == null)
            {
                return;
            }

            for (int i = 0; i < Players.Length; i++)
            {
                if (_lookup.TryGetRig(Players[i], out PlayerRig rig))
                {
                    rig.SetPaused(paused);
                }
            }
        }

        private void HandlePlayerJoined(PlayerId id)
        {
            if (_lookup == null || !_lookup.TryGetRig(id, out PlayerRig rig))
            {
                return;
            }

            if (_paused)
            {
                rig.SetPaused(true);
            }

            if (_menuOwner != PlayerId.None && _menuOwner != id && rig.Menu != null)
            {
                rig.Menu.SetBlocked(true);
            }
        }

        private bool TryGetMenu(PlayerId id, out PlayerMenuInput menu)
        {
            menu = null;
            if (_lookup == null || !_lookup.TryGetRig(id, out PlayerRig rig))
            {
                return false;
            }

            menu = rig.Menu;
            return menu != null;
        }

        private void OnValidate()
        {
            if (_players == null)
            {
                TryGetComponent(out _players);
            }

            if (_players == null)
            {
                Debug.LogWarning($"[{nameof(PlayerMenuRouter)}] Missing PlayerJoinCoordinator on '{name}'.", this);
            }
        }
    }
}
