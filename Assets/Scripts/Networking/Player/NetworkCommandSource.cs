using System;
using AlienFarms.Core;
using UnityEngine;

namespace AlienFarms.Networking.Player
{
    /// <summary>
    /// <see cref="IPlayerCommandSource"/> fed with network commands (server side of a remote player). Holds the last
    /// applied command and raises <see cref="GrabStarted"/> / <see cref="GrabCanceled"/> on the Grab bit's edges. Look is
    /// always zero: the view arrives as an absolute yaw/pitch and is applied to the camera with SetDrivenView.
    /// </summary>
    public sealed class NetworkCommandSource : IPlayerCommandSource
    {
        private Vector2 _move;
        private bool _sprint;
        private bool _grab;
        private bool _aim;
        private bool _paused;

        public event Action GrabStarted;
        public event Action GrabCanceled;

        public Vector2 Move => _move;

        public Vector2 Look => Vector2.zero;

        public bool IsLookPointerDelta => false;

        public bool IsSprintHeld => _sprint;

        public bool IsGrabHeld => _grab;

        public bool IsAimHeld => _aim;

        /// <summary>The last applied command came from a paused owner (see <see cref="PlayerInputCommand.IsPaused"/>).</summary>
        public bool IsPaused => _paused;

        /// <summary>Makes <paramref name="command"/> the current input; Grab edges are raised after the state is updated.</summary>
        public void Apply(in PlayerInputCommand command)
        {
            bool wasGrabbing = _grab;
            _move = command.Move;
            _sprint = command.IsSprintHeld;
            _aim = command.IsAimHeld;
            _paused = command.IsPaused;
            _grab = command.IsGrabHeld;

            if (_grab && !wasGrabbing)
            {
                GrabStarted?.Invoke();
            }
            else if (!_grab && wasGrabbing)
            {
                GrabCanceled?.Invoke();
            }
        }

        /// <summary>Back to no input, without raising edges.</summary>
        public void Reset()
        {
            _move = Vector2.zero;
            _sprint = false;
            _grab = false;
            _aim = false;
            _paused = false;
        }
    }
}
