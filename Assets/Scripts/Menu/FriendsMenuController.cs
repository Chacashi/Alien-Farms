using AlienFarms.Core;
using AlienFarms.Data;
using UnityEngine;

namespace AlienFarms.Menu
{
    [DisallowMultipleComponent]
    public sealed class FriendsMenuController : MonoBehaviour
    {
        [SerializeField] private FriendsChannel _friends;
        [SerializeField] private OnlineSessionChannel _onlineSession;
        [SerializeField] private MenuFlowController _menuFlow;

        internal UnityEngine.EventSystems.EventSystem MenuEventSystem =>
            _menuFlow != null ? _menuFlow.MenuEventSystem : null;
        internal bool CanShowInvite => !InMatch && _menuFlow != null &&
            _menuFlow.Phase is MenuPhase.Title or MenuPhase.SubMenu or MenuPhase.Online;

        private SessionInviteOffer? _pendingJoin;
        private MatchPhase _matchPhase;
        private bool InMatch => _matchPhase is MatchPhase.Countdown or MatchPhase.Playing or MatchPhase.Results;

        private void OnValidate()
        {
            if (_friends == null || _onlineSession == null || _menuFlow == null)
            {
                Debug.LogWarning($"[{nameof(FriendsMenuController)}] Missing references on '{name}'.", this);
            }
        }

        private void OnEnable() => GameEvents.OnMatchPhaseChanged += HandleMatchPhaseChanged;

        private void OnDisable()
        {
            GameEvents.OnMatchPhaseChanged -= HandleMatchPhaseChanged;
            _matchPhase = MatchPhase.None;
            _pendingJoin = null;
        }

        private void HandleMatchPhaseChanged(MatchPhase previous, MatchPhase current) => _matchPhase = current;

        public void AcceptInvite()
        {
            if (_friends == null || _onlineSession == null || _menuFlow == null ||
                !_friends.ConsumeInvite(out SessionInviteOffer offer))
            {
                return;
            }

            var decision = InviteAcceptRules.Decide(_onlineSession.Status, InMatch, _onlineSession.HasRejoinOffer,
                Time.realtimeSinceStartupAsDouble >= offer.ExpiresAtRealtime);
            if (decision.Next == InviteAcceptRules.Action.Blocked)
            {
                _onlineSession.SetError(decision.ErrorKey);
                return;
            }

            if (_pendingJoin.HasValue)
            {
                _onlineSession.SetError(OnlineSessionErrorKeys.Busy);
                return;
            }

            MenuPhase phase = _menuFlow.Phase;
            if (phase is MenuPhase.Title or MenuPhase.SubMenu)
            {
                _menuFlow.PlayOnline();
            }

            if (_menuFlow.Phase != MenuPhase.Online)
            {
                _onlineSession.SetError(OnlineSessionErrorKeys.Busy);
                return;
            }

            _pendingJoin = offer;
            if (decision.Next == InviteAcceptRules.Action.LeaveThenJoin)
            {
                if (!_onlineSession.RequestLeave())
                {
                    _pendingJoin = null;
                }

                return;
            }

            if (phase == MenuPhase.Online)
            {
                if (_onlineSession.Status == OnlineSessionStatus.SignedOut)
                {
                    _onlineSession.RequestSignIn();
                }

                AdvanceJoin();
            }
        }

        public void DeclineInvite() => _friends?.DeclineInvite();

        private void LateUpdate()
        {
            if (_friends != null && _onlineSession != null && _friends.HasInvite &&
                Time.realtimeSinceStartupAsDouble >= _friends.PendingInvite.ExpiresAtRealtime)
            {
                _friends.DeclineInvite();
                _onlineSession.SetError(OnlineSessionErrorKeys.InviteExpired);
            }

            AdvanceJoin();
        }

        private void AdvanceJoin()
        {
            if (!_pendingJoin.HasValue)
            {
                return;
            }

            SessionInviteOffer offer = _pendingJoin.Value;
            InviteJoinSequence.Step step = InviteJoinSequence.Decide(_menuFlow.Phase, _onlineSession.Status,
                _onlineSession.HasRejoinOffer, InMatch, offer.ExpiresAtRealtime, Time.realtimeSinceStartupAsDouble);
            switch (step)
            {
                case InviteJoinSequence.Step.Wait:
                    return;
                case InviteJoinSequence.Step.DeclineRejoin:
                    if (!_onlineSession.RequestDeclineRejoin() || _onlineSession.HasRejoinOffer)
                    {
                        _pendingJoin = null;
                    }

                    return;
                case InviteJoinSequence.Step.Join:
                    _pendingJoin = null;
                    _onlineSession.RequestJoin(offer.Code);
                    return;
                case InviteJoinSequence.Step.Expire:
                    _onlineSession.SetError(OnlineSessionErrorKeys.InviteExpired);
                    break;
            }

            _pendingJoin = null;
        }
    }
}
