using System;
using AlienFarms.Core;
using UnityEngine;

namespace AlienFarms.Data
{
    /// <summary>
    /// Runtime channel between the online menu (requests, views) and the session service on the NetworkRoot (state).
    /// The menu only calls the <c>Request*</c> methods and reads the state; the service listens to the requests and is the
    /// only writer. Primitive data only, so it lives in the runtime assembly and the menu never references networking.
    /// Nothing is serialized: state from a previous play session is discarded through a generation counter.
    /// </summary>
    [CreateAssetMenu(fileName = "OnlineSessionChannel", menuName = "Alien Farms/Networking/Online Session Channel (Runtime)")]
    public sealed class OnlineSessionChannel : ScriptableObject
    {
        /// <summary>Two players plus one spectator.</summary>
        public const int MaxParticipants = 3;

        private static int s_generation;

        [NonSerialized] private readonly OnlineParticipant[] _participants = new OnlineParticipant[MaxParticipants];
        [NonSerialized] private int _generation = -1;
        [NonSerialized] private OnlineSessionStatus _status;
        [NonSerialized] private string _code;
        [NonSerialized] private bool _isHost;
        [NonSerialized] private string _localPlayerId;
        [NonSerialized] private string _lastErrorKey;
        [NonSerialized] private int _participantCount;

        /// <summary>Raised after any state change (status, code, participants, error).</summary>
        public event Action Changed;

        /// <summary>Service side: sign in to Unity Gaming Services.</summary>
        public event Action SignInRequested;

        /// <summary>Service side: create a session (this machine hosts).</summary>
        public event Action CreateRequested;

        /// <summary>Service side: join the session with the given (normalized) code.</summary>
        public event Action<string> JoinRequested;

        /// <summary>Service side: leave the current session.</summary>
        public event Action LeaveRequested;

        public OnlineSessionStatus Status => IsCurrent ? _status : OnlineSessionStatus.SignedOut;

        /// <summary>Join code of the current session (null when in none).</summary>
        public string Code => IsCurrent ? _code : null;

        /// <summary>True when this machine created the current session.</summary>
        public bool IsHost => IsCurrent && _isHost;

        /// <summary>Unity Authentication id of this machine (null when signed out).</summary>
        public string LocalPlayerId => IsCurrent ? _localPlayerId : null;

        /// <summary>Localization key of the last failure (<see cref="OnlineSessionErrorKeys"/>), null when none.</summary>
        public string LastErrorKey => IsCurrent ? _lastErrorKey : null;

        public bool HasError => !string.IsNullOrEmpty(LastErrorKey);

        public int ParticipantCount => IsCurrent ? _participantCount : 0;

        private bool IsCurrent => _generation == s_generation;

        private void OnEnable()
        {
            // Referenced from the MainMenu and the persistent root: keep the runtime state across scene loads.
            hideFlags |= HideFlags.DontUnloadUnusedAsset;
        }

        public OnlineParticipant GetParticipant(int index)
        {
            if ((uint)index >= (uint)ParticipantCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _participants[index];
        }

        // ---- Requests (menu side) ----

        /// <summary>Asks the service to sign in. Idempotent: true when already signed in or signing in; false when no service is listening.</summary>
        public bool RequestSignIn()
        {
            if (!OnlineSessionStatusRules.CanSignIn(Status))
            {
                return true;
            }

            return Dispatch(SignInRequested);
        }

        /// <summary>Asks the service to create a session. False (with an error key) when not allowed now.</summary>
        public bool RequestCreate()
        {
            if (!OnlineSessionStatusRules.CanStartSession(Status))
            {
                return Reject(Status == OnlineSessionStatus.InSession ? OnlineSessionErrorKeys.AlreadyInSession : OnlineSessionErrorKeys.Busy);
            }

            return Dispatch(CreateRequested);
        }

        /// <summary>Asks the service to join by code. The code is normalized first; a malformed one is rejected here.</summary>
        public bool RequestJoin(string code)
        {
            if (!OnlineSessionStatusRules.CanStartSession(Status))
            {
                return Reject(Status == OnlineSessionStatus.InSession ? OnlineSessionErrorKeys.AlreadyInSession : OnlineSessionErrorKeys.Busy);
            }

            if (!SessionCodeFormat.TryNormalize(code, out string normalized))
            {
                return Reject(OnlineSessionErrorKeys.InvalidCode);
            }

            if (JoinRequested == null)
            {
                return Reject(OnlineSessionErrorKeys.NotConfigured);
            }

            ClearError();
            JoinRequested.Invoke(normalized);
            return true;
        }

        /// <summary>Asks the service to leave the current (or starting) session. False when there is none.</summary>
        public bool RequestLeave()
        {
            if (!OnlineSessionStatusRules.CanLeave(Status))
            {
                return false;
            }

            return Dispatch(LeaveRequested);
        }

        // ---- State (service side) ----

        /// <summary>Moves to <paramref name="status"/> when the transition is allowed; raises <see cref="Changed"/>.</summary>
        public bool TrySetStatus(OnlineSessionStatus status)
        {
            EnsureCurrent();
            if (!OnlineSessionStatusRules.CanTransition(_status, status))
            {
                return false;
            }

            _status = status;
            if (status == OnlineSessionStatus.SignedOut)
            {
                _localPlayerId = null;
                ClearSessionData();
            }
            else if (status == OnlineSessionStatus.Ready)
            {
                ClearSessionData();
            }

            Changed?.Invoke();
            return true;
        }

        public void SetLocalPlayerId(string playerId)
        {
            EnsureCurrent();
            if (string.Equals(_localPlayerId, playerId, StringComparison.Ordinal))
            {
                return;
            }

            _localPlayerId = playerId;
            Changed?.Invoke();
        }

        /// <summary>Code and role of the session this machine is in.</summary>
        public void SetSession(string code, bool isHost)
        {
            EnsureCurrent();
            if (string.Equals(_code, code, StringComparison.Ordinal) && _isHost == isHost)
            {
                return;
            }

            _code = code;
            _isHost = isHost;
            Changed?.Invoke();
        }

        /// <summary>Replaces the participant list with the first <paramref name="count"/> entries (at most <see cref="MaxParticipants"/>).</summary>
        public void SetParticipants(OnlineParticipant[] participants, int count)
        {
            EnsureCurrent();
            if (participants == null)
            {
                count = 0;
            }

            count = Mathf.Clamp(count, 0, Mathf.Min(MaxParticipants, participants?.Length ?? 0));
            bool changed = count != _participantCount;
            for (int i = 0; i < count; i++)
            {
                if (!changed && !_participants[i].Equals(participants[i]))
                {
                    changed = true;
                }

                _participants[i] = participants[i];
            }

            for (int i = count; i < MaxParticipants; i++)
            {
                _participants[i] = default;
            }

            _participantCount = count;
            if (changed)
            {
                Changed?.Invoke();
            }
        }

        /// <summary>Records a failure as a localization key (<see cref="OnlineSessionErrorKeys"/>).</summary>
        public void SetError(string errorKey)
        {
            EnsureCurrent();
            if (string.Equals(_lastErrorKey, errorKey, StringComparison.Ordinal))
            {
                return;
            }

            _lastErrorKey = errorKey;
            Changed?.Invoke();
        }

        public void ClearError()
        {
            SetError(null);
        }

        private bool Dispatch(Action request)
        {
            if (request == null)
            {
                return Reject(OnlineSessionErrorKeys.NotConfigured);
            }

            ClearError();
            request.Invoke();
            return true;
        }

        private bool Reject(string errorKey)
        {
            SetError(errorKey);
            return false;
        }

        private void ClearSessionData()
        {
            _code = null;
            _isHost = false;
            for (int i = 0; i < MaxParticipants; i++)
            {
                _participants[i] = default;
            }

            _participantCount = 0;
        }

        private void EnsureCurrent()
        {
            if (IsCurrent)
            {
                return;
            }

            _generation = s_generation;
            _status = OnlineSessionStatus.SignedOut;
            _localPlayerId = null;
            _lastErrorKey = null;
            ClearSessionData();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_generation++;
        }
    }
}
