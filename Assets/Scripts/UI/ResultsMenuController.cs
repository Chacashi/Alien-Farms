using AlienFarms.Core;
using AlienFarms.Match;
using AlienFarms.Player;
using UnityEngine;

namespace AlienFarms.UI
{
    /// <summary>
    /// Gives the results menu to the winner (Player 1 on a draw) once the match accepts a rematch
    /// (<see cref="MatchManager.IsRematchReady"/>, the same clock that gates the rematch), so a button held at the end of
    /// the round does not click it. Handed to the other player if the owner loses its device; released when leaving Results.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResultsMenuController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Match whose rematch readiness gates the menu.")]
        private MatchManager _match;

        [SerializeField, Tooltip("Routes the results menu to its owner and blocks the other player.")]
        private PlayerMenuRouter _menus;

        [SerializeField, Tooltip("Results panel root the owner's event system is scoped to (ResultsPanelRoot).")]
        private GameObject _menuRoot;

        [SerializeField, Tooltip("Button focused when the menu is routed (BTN_Rematch).")]
        private GameObject _firstSelected;

        // Owner waiting for the rematch to become ready (None when nothing is pending).
        private PlayerId _pendingOwner = PlayerId.None;
        private PlayerId _owner = PlayerId.None;
        private bool _routed;

        private void OnEnable()
        {
            GameEvents.OnMatchEnded += HandleMatchEnded;
            GameEvents.OnMatchPhaseChanged += HandleMatchPhaseChanged;
            GameEvents.OnPlayerDeviceLost += HandlePlayerDeviceLost;
            GameEvents.OnPlayerDeviceRegained += HandlePlayerDeviceRegained;
        }

        private void OnDisable()
        {
            GameEvents.OnMatchEnded -= HandleMatchEnded;
            GameEvents.OnMatchPhaseChanged -= HandleMatchPhaseChanged;
            GameEvents.OnPlayerDeviceLost -= HandlePlayerDeviceLost;
            GameEvents.OnPlayerDeviceRegained -= HandlePlayerDeviceRegained;
            _pendingOwner = PlayerId.None;
            Release();
        }

        private void Update()
        {
            if (_pendingOwner == PlayerId.None || _match == null || !_match.IsRematchReady)
            {
                return;
            }

            PlayerId owner = _pendingOwner;
            _pendingOwner = PlayerId.None;
            if (!Route(owner))
            {
                Route(owner.Other());
            }
        }

        private void HandleMatchEnded(MatchResult result)
        {
            _pendingOwner = result.IsDraw ? PlayerId.Player1 : result.Winner;
        }

        private void HandleMatchPhaseChanged(MatchPhase previous, MatchPhase current)
        {
            if (previous != MatchPhase.Results)
            {
                return;
            }

            _pendingOwner = PlayerId.None;
            Release();
        }

        private void HandlePlayerDeviceLost(PlayerId playerId)
        {
            if (_pendingOwner == playerId)
            {
                // Not routed yet: the other player will get the menu when it becomes ready.
                _pendingOwner = playerId.Other();
                return;
            }

            if (_routed && playerId == _owner)
            {
                Route(playerId.Other());
            }
        }

        private void HandlePlayerDeviceRegained(PlayerId playerId)
        {
            if (_routed && playerId == _owner)
            {
                Route(playerId);
            }
        }

        private bool Route(PlayerId owner)
        {
            if (_menus == null || !_menus.RouteMenu(owner, _menuRoot, _firstSelected))
            {
                return false;
            }

            _owner = owner;
            _routed = true;
            return true;
        }

        private void Release()
        {
            if (!_routed)
            {
                return;
            }

            _routed = false;
            _owner = PlayerId.None;
            if (_menus != null)
            {
                _menus.ReleaseMenus();
            }
        }

        private void OnValidate()
        {
            if (_match == null || _menus == null)
            {
                Debug.LogWarning($"[{nameof(ResultsMenuController)}] Missing MatchManager or PlayerMenuRouter on '{name}'.", this);
            }

            if (_menuRoot == null || _firstSelected == null)
            {
                Debug.LogWarning($"[{nameof(ResultsMenuController)}] Missing results menu root or first selected button on '{name}'.", this);
            }
            else if (!_firstSelected.transform.IsChildOf(_menuRoot.transform))
            {
                Debug.LogWarning($"[{nameof(ResultsMenuController)}] First selected button on '{name}' is not under the results menu root.", this);
            }
        }
    }
}
