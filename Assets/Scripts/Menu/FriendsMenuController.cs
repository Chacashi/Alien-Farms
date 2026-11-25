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

        internal string ToastErrorKey => _friends != null && _friends.LastErrorKey == _toastErrorKey
            ? _toastErrorKey : null;

        private SessionInviteOffer? _pendingJoin;
        private string _toastErrorKey;
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
            DismissToastError();
            _matchPhase = MatchPhase.None;
            _pendingJoin = null;
        }

        private void HandleMatchPhaseChanged(MatchPhase previous, MatchPhase current) => _matchPhase = current;

        public void AcceptInvite()
        {
            if (_friends == null || _onlineSession == null || _menuFlow == null ||
                !_friends.HasInvite)
            {
                return;
            }

            SessionInviteOffer offer = _friends.PendingInvite;
            var decision = InviteAcceptRules.Decide(_onlineSession.Status, InMatch, _onlineSession.HasRejoinOffer,
                Time.realtimeSinceStartupAsDouble >= offer.ExpiresAtRealtime);
            if (decision.Next == InviteAcceptRules.Action.Blocked)
            {
                ReportAcceptError(decision.ErrorKey);
                return;
            }

            if (_pendingJoin.HasValue)
            {
                ReportAcceptError(OnlineSessionErrorKeys.Busy);
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
            if (!_friends.ConsumeInvite(out _))
            {
                _pendingJoin = null;
                return;
            }

            DismissToastError();
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

        public void DeclineInvite()
        {
            if (ToastErrorKey != null)
            {
                DismissToastError();
                return;
            }

            _friends?.DeclineInvite();
        }

        private void ReportAcceptError(string key, bool showToast = false)
        {
            if (showToast || _menuFlow.Phase is MenuPhase.Title or MenuPhase.SubMenu)
            {
                _toastErrorKey = key;
                _friends.SetLastErrorKey(key);
            }
            else
            {
                _onlineSession.SetError(key);
            }
        }

        private void DismissToastError()
        {
            bool wasVisible = ToastErrorKey != null;
            _toastErrorKey = null;
            if (wasVisible)
            {
                _friends.SetLastErrorKey(null);
            }
        }

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
                case InviteJoinSequence.Step.Cancel:
                    Debug.Log($"[{nameof(FriendsMenuController)}] Invite join canceled in {_menuFlow.Phase} " +
                        $"with session status {_onlineSession.Status} (in match: {InMatch}).", this);
                    ReportAcceptError(OnlineSessionErrorKeys.Busy, showToast: true);
                    if (!CanShowInvite)
                    {
                        _onlineSession.SetError(OnlineSessionErrorKeys.Busy);
                    }

                    break;
                case InviteJoinSequence.Step.Expire:
                    _onlineSession.SetError(OnlineSessionErrorKeys.InviteExpired);
                    break;
            }

            _pendingJoin = null;
        }
    }
}
