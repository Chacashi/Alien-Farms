using System;
using AlienFarms.Core;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Pure rules of reclaiming a lobby membership this player still holds (a join by code refused as 'already in a
    /// session'): only the session whose code is the one asked for is reconnected to, and only a session known to be
    /// another one is left (leaving one's own session would release the seat as Left).
    /// </summary>
    public static class MembershipRules
    {
        /// <summary>
        /// Relation of the joined session's <paramref name="sessionCode"/> to <paramref name="joinCode"/>, both compared
        /// normalized (<see cref="SessionCodeFormat"/>). An empty or malformed code on either side is
        /// <see cref="MembershipMatch.Unknown"/>: never a match, never a reason to leave.
        /// </summary>
        public static MembershipMatch Classify(string sessionCode, string joinCode)
        {
            if (!SessionCodeFormat.TryNormalize(sessionCode, out string session)
                || !SessionCodeFormat.TryNormalize(joinCode, out string join))
            {
                return MembershipMatch.Unknown;
            }

            return string.Equals(session, join, StringComparison.Ordinal) ? MembershipMatch.Same : MembershipMatch.Other;
        }
    }
}
