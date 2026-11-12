using AlienFarms.Core;
using AlienFarms.Match;
using AlienFarms.Player;
using UnityEngine;
using UnityEngine.UI;

namespace AlienFarms.UI
{
    /// <summary>
    /// Gives the results menu to the winner (Player 1 on a draw) once Results accepts input
    /// (<see cref="MatchManager.IsResultsInputReady"/>, the same clock that gates the rematch), so a button held at the end of
    /// the round does not click it. Handed to the other player if the owner loses its device; released when leaving Results.
    /// The rematch button (<c>_firstSelected</c>) looks disabled while the rematch is not ready or the match is suspended
    /// (online: a player's seat awaits its reconnection).
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

        // Mirrored match (online client, MatchManager disabled): the menu opens after the results input delay.
        private float _replicaReadyTime;

        // Rematch button (on _firstSelected); while in Results it is interactable only when IsRematchReady && !IsSuspended.
        private Selectable _rematchButton;
        private bool _inResults;

        // Bit (int)PlayerId set while that seat is Reconnecting (online presence; the match is suspended meanwhile).
        private int _awayMask;

        private void Awake()
        {
            if (_firstSelected != null)
            {
                _firstSelected.TryGetComponent(out _rematchButton);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnMatchEnded += HandleMatchEnded;
            GameEvents.OnMatchPhaseChanged += HandleMatchPhaseChanged;
            GameEvents.OnPlayerDeviceLost += HandlePlayerDeviceLost;
            GameEvents.OnPlayerDeviceRegained += HandlePlayerDeviceRegained;
            GameEvents.OnPlayerConnectionChanged += HandlePlayerConnectionChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnMatchEnded -= HandleMatchEnded;
            GameEvents.OnMatchPhaseChanged -= HandleMatchPhaseChanged;
            GameEvents.OnPlayerDeviceLost -= HandlePlayerDeviceLost;
            GameEvents.OnPlayerDeviceRegained -= HandlePlayerDeviceRegained;
            GameEvents.OnPlayerConnectionChanged -= HandlePlayerConnectionChanged;
            _pendingOwner = PlayerId.None;
            _awayMask = 0;
            LeaveResults();
            Release();
        }

        private void Update()
        {
            if (_inResults)
            {
                // Cheap: one bool compared per frame, the button only touched when it changes.
                SetRematchInteractable(_match != null && IsRematchReady && !IsSuspended);
            }

            if (_pendingOwner == PlayerId.None || _match == null || !IsResultsInputReady)
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
            float delay = _match != null && _match.Settings != null ? _match.Settings.ResultsInputDelay : 0f;
            _replicaReadyTime = Time.time + delay;
            _inResults = true;
            SetRematchInteractable(false);
        }

        /// <summary>The match is frozen: suspended by the server, or (online client) a seat awaits its player.</summary>
        private bool IsSuspended => _awayMask != 0 || (_match.isActiveAndEnabled && _match.IsSuspended);

        /// <summary>
        /// The menu can be routed (Results accepts input; also while suspended, so "back to menu" stays reachable). When
        /// the match does not run here (online client) the same delay, timed locally. A suspension that froze the match
        /// clock before its delay elapsed falls back to that local timer too.
        /// </summary>
        private bool IsResultsInputReady => _match.isActiveAndEnabled
            ? _match.IsResultsInputReady || (_match.IsSuspended && Time.time >= _replicaReadyTime)
            : Time.time >= _replicaReadyTime;

        /// <summary>The rematch button's gate: the match's own (never while suspended), or the local delay on a client.</summary>
        private bool IsRematchReady => _match.isActiveAndEnabled ? _match.IsRematchReady : Time.time >= _replicaReadyTime;

        private void HandleMatchPhaseChanged(MatchPhase previous, MatchPhase current)
        {
            if (previous != MatchPhase.Results)
            {
                return;
            }

            _pendingOwner = PlayerId.None;
            LeaveResults();
            Release();
        }

        private void HandlePlayerConnectionChanged(PlayerId playerId, PlayerConnectionStatus status, float graceLeft)
        {
            if (playerId == PlayerId.None)
            {
                return;
            }

            int bit = 1 << (int)playerId;
            // Only a reserved seat suspends the match; a released one (Left / TimedOut) ends it through the roster instead.
            _awayMask = status == PlayerConnectionStatus.Reconnecting ? _awayMask | bit : _awayMask & ~bit;
        }

        private void LeaveResults()
        {
            if (!_inResults)
            {
                return;
            }

            // Out of Results the button is back to its scene state (the next match end disables it again).
            _inResults = false;
            SetRematchInteractable(true);
        }

        private void SetRematchInteractable(bool interactable)
        {
            if (_rematchButton != null && _rematchButton.interactable != interactable)
            {
                _rematchButton.interactable = interactable;
            }
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
            else
            {
                if (!_firstSelected.TryGetComponent(out Selectable _))
                {
                    Debug.LogWarning($"[{nameof(ResultsMenuController)}] First selected '{_firstSelected.name}' on '{name}' has no Selectable (Button); the rematch button cannot be disabled while the rematch is not ready.", this);
                }

                if (!_firstSelected.transform.IsChildOf(_menuRoot.transform))
                {
                    Debug.LogWarning($"[{nameof(ResultsMenuController)}] First selected button on '{name}' is not under the results menu root.", this);
                }
            }
        }
    }
}
