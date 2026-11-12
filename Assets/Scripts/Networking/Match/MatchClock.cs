using System;
using AlienFarms.Core;

namespace AlienFarms.Networking.Match
{
    /// <summary>
    /// What a client shows from the replicated server times (0 messages per second for the timer): the whole seconds
    /// left of the countdown and of the round, rounded up like <c>MatchTimer.DisplaySeconds</c>.
    /// </summary>
    public static class MatchClock
    {
        // Large enough for any round; keeps the double-to-int cast safe if a time is garbage.
        private const double MaxSeconds = 1000000d;

        /// <summary>Countdown seconds left at <paramref name="now"/>; never below 1 (the server ends it instead of showing 0).</summary>
        public static int CountdownSecondsLeft(double countdownEndTime, double now)
        {
            return Math.Max(1, CeilSeconds(countdownEndTime - now));
        }

        /// <summary>
        /// Round seconds shown in <paramref name="phase"/>: counting down to <paramref name="roundEndTime"/> while
        /// playing, 0 on the results screen and the full duration before the round starts.
        /// </summary>
        public static int DisplaySeconds(MatchPhase phase, double roundEndTime, double now, float fullDuration)
        {
            switch (phase)
            {
                case MatchPhase.Playing:
                    return Math.Max(0, CeilSeconds(roundEndTime - now));
                case MatchPhase.Results:
                    return 0;
                default:
                    return Math.Max(0, CeilSeconds(fullDuration));
            }
        }

        /// <summary>
        /// Same as <see cref="DisplaySeconds(MatchPhase, double, double, float)"/>, but while playing with the clock frozen
        /// (<paramref name="frozenRemaining"/> &gt;= 0, the match suspended by a reconnection grace) it shows the frozen
        /// seconds, rounded up, instead of counting down. A negative value means the clock runs.
        /// </summary>
        public static int DisplaySeconds(MatchPhase phase, double roundEndTime, double now, float fullDuration, float frozenRemaining)
        {
            if (phase == MatchPhase.Playing && frozenRemaining >= 0f)
            {
                return Math.Max(0, CeilSeconds(frozenRemaining));
            }

            return DisplaySeconds(phase, roundEndTime, now, fullDuration);
        }

        /// <summary>
        /// True when a client derives countdown ticks in <paramref name="phase"/>: only in Countdown with the clock running.
        /// With the clock frozen (<paramref name="frozenRemaining"/> &gt;= 0) the host's countdown is suspended, so the
        /// number already shown stays; the server rewrites the countdown end time when it restarts the countdown.
        /// </summary>
        public static bool TicksCountdown(MatchPhase phase, float frozenRemaining)
        {
            return phase == MatchPhase.Countdown && frozenRemaining < 0f;
        }

        private static int CeilSeconds(double seconds)
        {
            if (double.IsNaN(seconds))
            {
                return 0;
            }

            return (int)Math.Ceiling(Math.Max(-MaxSeconds, Math.Min(MaxSeconds, seconds)));
        }
    }
}
