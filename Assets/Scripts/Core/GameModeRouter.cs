using AlienFarms.Data;
using AlienFarms.Match;
using AlienFarms.Player;
using UnityEngine;

namespace AlienFarms.Core
{
    /// <summary>
    /// Composition root of the gameplay scene: reads <see cref="MatchSession.Mode"/> and activates the local or the online
    /// mode root, binds the match to that mode's <see cref="IPlayerRoster"/> and configures the pause.
    /// Both mode roots must be saved inactive: activating one here runs its children's Awake synchronously, before any
    /// other Awake of the scene, so role setters (spawners, obstacle field) apply before their Awake/Start.
    /// The online roster lives in the networking assembly; it is wired as a MonoBehaviour implementing IPlayerRoster, so
    /// this assembly never references networking types.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class GameModeRouter : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField, Tooltip("Runtime session written by the menus (Mode). Missing = local mode.")]
        private MatchSession _session;

        [Header("Mode roots (saved inactive)")]
        [SerializeField, Tooltip("Local split-screen root (PlayerInputManager, PlayerJoinCoordinator, LobbyCamera).")]
        private GameObject _localModeRoot;

        [SerializeField, Tooltip("Online root (NetworkPlayerRoster, OnlineMatchBootstrap...).")]
        private GameObject _onlineModeRoot;

        [Header("References")]
        [SerializeField, Tooltip("Match driven by the selected roster.")]
        private MatchManager _match;

        [SerializeField, Tooltip("Pause; online it only opens the menu (no time freeze).")]
        private PauseController _pause;

        [SerializeField, Tooltip("Online roster: a component under the online root implementing IPlayerRoster (NetworkPlayerRoster).")]
        private MonoBehaviour _onlineRoster;

        [SerializeField, Tooltip("Menu router shared by both modes (lives outside the mode roots).")]
        private PlayerMenuRouter _menuRouter;

        [SerializeField, Tooltip("Online menu players: a component under the online root implementing IPlayerRigLookup (LocalPlayerRigLookup).")]
        private MonoBehaviour _onlineMenuPlayers;

        private GameMode _mode = GameMode.Local;

        /// <summary>Mode applied in Awake.</summary>
        public GameMode Mode => _mode;

        private void Awake()
        {
            _mode = _session != null ? _session.Mode : GameMode.Local;
            bool online = _mode == GameMode.Online;

            // Deactivate the unused root first so its components never run alongside the selected one.
            SetRootActive(online ? _localModeRoot : _onlineModeRoot, false);

            if (online)
            {
                ConfigureOnline();
            }

            SetRootActive(online ? _onlineModeRoot : _localModeRoot, true);
        }

        private void ConfigureOnline()
        {
            if (_onlineRoster is IPlayerRoster roster)
            {
                if (_match != null)
                {
                    _match.Bind(roster);
                }
            }
            else
            {
                Debug.LogError($"[{nameof(GameModeRouter)}] Online roster on '{name}' is missing or does not implement {nameof(IPlayerRoster)}.", this);
            }

            if (_pause != null)
            {
                _pause.SetFreezesTime(false);
            }

            // Online the shared menus only ever route to the avatar this machine owns.
            if (_menuRouter != null)
            {
                if (_onlineMenuPlayers is IPlayerRigLookup lookup)
                {
                    _menuRouter.SetPlayers(lookup);
                }
                else
                {
                    Debug.LogError($"[{nameof(GameModeRouter)}] Online menu players on '{name}' are missing or do not implement {nameof(IPlayerRigLookup)}.", this);
                }
            }
        }

        private static void SetRootActive(GameObject root, bool active)
        {
            if (root != null && root.activeSelf != active)
            {
                root.SetActive(active);
            }
        }

        private void OnValidate()
        {
            if (_session == null)
            {
                Debug.LogWarning($"[{nameof(GameModeRouter)}] Missing MatchSession on '{name}'; the scene always runs in local mode.", this);
            }

            if (_localModeRoot == null || _onlineModeRoot == null)
            {
                Debug.LogWarning($"[{nameof(GameModeRouter)}] Missing local or online mode root on '{name}'.", this);
            }

            if (_match == null)
            {
                Debug.LogWarning($"[{nameof(GameModeRouter)}] Missing MatchManager on '{name}'.", this);
            }

            if (_pause == null)
            {
                Debug.LogWarning($"[{nameof(GameModeRouter)}] Missing PauseController on '{name}'.", this);
            }

            if (_menuRouter == null)
            {
                Debug.LogWarning($"[{nameof(GameModeRouter)}] Missing PlayerMenuRouter on '{name}'; online pause and results menus cannot route.", this);
            }

            if (_onlineMenuPlayers != null && !(_onlineMenuPlayers is IPlayerRigLookup))
            {
                Debug.LogWarning($"[{nameof(GameModeRouter)}] Online menu players '{_onlineMenuPlayers.GetType().Name}' on '{name}' do not implement {nameof(IPlayerRigLookup)}.", this);
            }

            if (_onlineRoster == null)
            {
                Debug.LogWarning($"[{nameof(GameModeRouter)}] Missing online roster on '{name}'.", this);
            }
            else if (!(_onlineRoster is IPlayerRoster))
            {
                Debug.LogWarning($"[{nameof(GameModeRouter)}] Online roster '{_onlineRoster.GetType().Name}' on '{name}' does not implement {nameof(IPlayerRoster)}.", this);
            }
        }
    }
}
