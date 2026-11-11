namespace AlienFarms.Networking.Player
{
    /// <summary>
    /// Server-side jitter buffer of one player's commands, consumed one per physics step.
    /// <list type="bullet">
    /// <item>Waits until <see cref="TargetDepth"/> commands are queued before playing.</item>
    /// <item>Drops duplicates and commands at or before the last consumed one (signed sequence distance, so the
    /// 32-bit sequence may wrap around; 0 is never a command).</item>
    /// <item>Skips sequence gaps that later packets prove lost.</item>
    /// <item>Trims the queue past <see cref="TargetDepth"/> + <see cref="MaxExtraDepth"/>; a Grab held in a trimmed
    /// command is carried into the next one so a press is never lost.</item>
    /// <item>Repeats the last command when it runs dry.</item>
    /// <item>Resynchronizes to a command a little beyond the window, but ignores absurd jumps
    /// (&gt; <see cref="MaxResyncDistance"/>).</item>
    /// </list>
    /// Fixed ring, no allocations.
    /// </summary>
    public sealed class InputCommandBuffer
    {
        public const int Capacity = 32;
        public const int MaxExtraDepth = 2;

        /// <summary>Largest forward jump accepted as a resync: about 10 s of 50 Hz commands.</summary>
        public const int MaxResyncDistance = 500;

        private readonly PlayerInputCommand[] _commands = new PlayerInputCommand[Capacity];
        private readonly bool[] _filled = new bool[Capacity];
        private readonly int _targetDepth;
        private PlayerInputCommand _last;
        private uint _lastConsumedSeq;
        private int _pendingCount;
        private bool _primed;
        private bool _synced;

        public InputCommandBuffer(int targetDepth)
        {
            _targetDepth = targetDepth < 0 ? 0 : (targetDepth > Capacity / 2 ? Capacity / 2 : targetDepth);
        }

        public int TargetDepth => _targetDepth;

        /// <summary>Commands queued and not consumed yet.</summary>
        public int PendingCount => _pendingCount;

        /// <summary>Sequence of the last command handed out fresh by <see cref="Consume"/> (0 = none yet). Sent back as the ack.</summary>
        public uint LastConsumedSeq => _synced && _last.Seq != 0 ? _lastConsumedSeq : 0u;

        /// <summary>
        /// Queues <paramref name="command"/>. False for sequence 0, duplicates, stale commands and absurd jumps. The first
        /// command received anchors the sequence; one moderately past the window resynchronizes the buffer to it.
        /// </summary>
        public bool Enqueue(in PlayerInputCommand command)
        {
            uint seq = command.Seq;
            if (seq == 0)
            {
                return false;
            }

            if (!_synced)
            {
                // Anchor half a window back so commands that arrive just after it (reordered) are still accepted.
                Resync(unchecked(seq - (uint)(Capacity / 2)));
                _synced = true;
            }

            int distance = Distance(seq);
            if (distance <= 0 || distance > MaxResyncDistance)
            {
                return false;
            }

            if (distance > Capacity)
            {
                Resync(seq - 1);
            }

            int index = (int)(seq % Capacity);
            if (_filled[index])
            {
                // Within (last, last + Capacity] every sequence owns its slot, so a filled slot is this same command.
                return false;
            }

            _commands[index] = command;
            _filled[index] = true;
            _pendingCount++;
            return true;
        }

        /// <summary>
        /// Command to apply this physics step. <paramref name="fresh"/> is false when the last command is repeated (not
        /// primed yet or starved); it is default before any command arrived.
        /// </summary>
        public PlayerInputCommand Consume(out bool fresh)
        {
            fresh = false;

            if (!_primed)
            {
                if (_pendingCount == 0 || _pendingCount < _targetDepth)
                {
                    return _last;
                }

                _primed = true;
            }

            // Too much queued (client clock ahead or a burst after a stall): drop the oldest to cut the latency back,
            // keeping any Grab they held.
            byte carriedGrab = 0;
            while (_pendingCount > _targetDepth + MaxExtraDepth && TryTakeNext(out PlayerInputCommand dropped))
            {
                carriedGrab |= (byte)(dropped.Buttons & PlayerInputCommand.GrabBit);
            }

            if (!TryTakeNext(out PlayerInputCommand command))
            {
                // Starved: repeat the last command and rebuild the cushion before playing again.
                _primed = false;
                return _last;
            }

            command.Buttons |= carriedGrab;
            _last = command;
            fresh = true;
            return command;
        }

        public void Clear()
        {
            Resync(0);
            _synced = false;
            _last = default;
        }

        // Signed distance from the last consumed sequence: positive = ahead, robust to the 32-bit wraparound.
        private int Distance(uint seq) => unchecked((int)(seq - _lastConsumedSeq));

        private bool TryTakeNext(out PlayerInputCommand command)
        {
            // The next sequence, or the first later one present: a gap behind a later packet is lost for good
            // (every packet also carries its predecessors).
            for (int offset = 1; offset <= Capacity; offset++)
            {
                uint seq = unchecked(_lastConsumedSeq + (uint)offset);
                int index = (int)(seq % Capacity);
                if (!_filled[index] || _commands[index].Seq != seq)
                {
                    continue;
                }

                command = _commands[index];
                _filled[index] = false;
                _pendingCount--;
                _lastConsumedSeq = seq;
                return true;
            }

            command = default;
            return false;
        }

        private void Resync(uint lastConsumedSeq)
        {
            for (int i = 0; i < Capacity; i++)
            {
                _filled[i] = false;
            }

            _pendingCount = 0;
            _primed = false;
            _lastConsumedSeq = lastConsumedSeq;
        }
    }
}
