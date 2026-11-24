using System;
using AlienFarms.Core;
using AlienFarms.Data;
using AlienFarms.Match;

namespace AlienFarms.Audio
{
    public sealed class MatchConnectionCueTracker
    {
        private readonly bool[] _known = new bool[2];
        private readonly PlayerConnectionStatus[] _last = new PlayerConnectionStatus[2];
        private MatchPhase _phase;
        private bool _suspended;
        private bool _seatReleasedWhileSuspended;

        public void SetPhase(MatchPhase phase)
        {
            _phase = phase;
            if (MatchSuspendRules.SuspendsIn(phase) && IsAnySeatReconnecting())
            {
                _suspended = true;
            }
        }

        public void Reset()
        {
            Array.Clear(_known, 0, _known.Length);
            Array.Clear(_last, 0, _last.Length);
            _phase = MatchPhase.None;
            _suspended = false;
            _seatReleasedWhileSuspended = false;
        }

        /// <summary>Tracks a seat transition and writes up to two cues into the supplied buffer.</summary>
        public int Apply(PlayerId id, PlayerConnectionStatus status, Span<UiSfxCue> output, bool isLocalSeat = false)
        {
            int index = (int)id - 1;
            if (index < 0 || index >= _known.Length)
            {
                return 0;
            }

            PlayerConnectionStatus previous = _last[index];
            _last[index] = status;
            if (!_known[index])
            {
                _known[index] = true;
                if (status == PlayerConnectionStatus.Reconnecting && MatchSuspendRules.SuspendsIn(_phase))
                {
                    _suspended = true;
                }

                return 0;
            }

            if (previous == status)
            {
                return 0;
            }

            switch (status)
            {
                case PlayerConnectionStatus.Reconnecting:
                    if (!_suspended && MatchSuspendRules.SuspendsIn(_phase))
                    {
                        _suspended = true;
                        output[0] = UiSfxCue.MatchSuspended;
                    }
                    else
                    {
                        output[0] = UiSfxCue.ConnectionLost;
                    }

                    return 1;

                case PlayerConnectionStatus.Connected:
                    if (previous != PlayerConnectionStatus.Reconnecting)
                    {
                        output[0] = UiSfxCue.PlayerJoined;
                        return 1;
                    }

                    int count = 0;
                    if (!isLocalSeat)
                    {
                        output[count++] = UiSfxCue.PlayerReconnected;
                    }

                    if (_suspended && !IsAnySeatReconnecting())
                    {
                        _suspended = false;
                        bool shouldResume = !_seatReleasedWhileSuspended;
                        _seatReleasedWhileSuspended = false;
                        if (shouldResume)
                        {
                            output[count++] = UiSfxCue.MatchResumed;
                        }
                    }

                    return count;

                case PlayerConnectionStatus.Left:
                case PlayerConnectionStatus.TimedOut:
                    if (!_suspended && previous == PlayerConnectionStatus.Reconnecting)
                    {
                        return 0;
                    }

                    if (_suspended)
                    {
                        _suspended = IsAnySeatReconnecting();
                        _seatReleasedWhileSuspended = _suspended;
                    }

                    output[0] = UiSfxCue.ConnectionLost;
                    return 1;

                default:
                    return 0;
            }
        }

        private bool IsAnySeatReconnecting()
        {
            return _last[0] == PlayerConnectionStatus.Reconnecting || _last[1] == PlayerConnectionStatus.Reconnecting;
        }
    }
}
