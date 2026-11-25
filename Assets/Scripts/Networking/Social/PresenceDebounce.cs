using AlienFarms.Core;

namespace AlienFarms.Networking.Social
{
    public sealed class PresenceDebounce
    {
        private (FriendAvailability Availability, string ActivityState)? _desired;
        private bool _pending;
        private double _sendAt;

        public void Schedule((FriendAvailability Availability, string ActivityState) desired, double now, double delay)
        {
            if (_desired == desired)
            {
                return;
            }

            _desired = desired;
            _pending = true;
            _sendAt = now + delay;
        }

        public bool TryTake(double now, out (FriendAvailability Availability, string ActivityState) desired)
        {
            desired = _desired.GetValueOrDefault();
            if (!_pending || now < _sendAt)
            {
                return false;
            }

            _pending = false;
            return true;
        }

        public void Retry((FriendAvailability Availability, string ActivityState) failed, double sendAt)
        {
            if (_desired == failed && !_pending)
            {
                _pending = true;
                _sendAt = sendAt;
            }
        }

        public void Reset()
        {
            _desired = null;
            _pending = false;
        }
    }
}
