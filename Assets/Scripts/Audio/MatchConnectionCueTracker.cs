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

        public void SetPhase(MatchPhase phase) => _phase = phase;

        public void Reset()
        {
            Array.Clear(_known, 0, _known.Length);
            Array.Clear(_last, 0, _last.Length);
            _phase = MatchPhase.None;
            _suspended = false;
        }

        /// <summary>Tracks a seat transition and writes up to two cues into the supplied buffer.</summary>
        public int Apply(PlayerId id, PlayerConnectionStatus status, Span<UiSfxCue> output)
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

                    output[0] = UiSfxCue.PlayerReconnected;
                    if (_suspended && !IsAnySeatReconnecting())
                    {
                        _suspended = false;
                        output[1] = UiSfxCue.MatchResumed;
                        return 2;
                    }

                    return 1;

                case PlayerConnectionStatus.Left:
                case PlayerConnectionStatus.TimedOut:
                    if (!IsAnySeatReconnecting())
                    {
                        _suspended = false;
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
