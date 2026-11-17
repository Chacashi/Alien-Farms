using System;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Who is a player and who the spectator in the online session's member list (<see cref="OnlineSessionService"/>).
    /// The host names the spectator in the session property <see cref="SpectatorProperty"/> (UGS id of the member its
    /// approval admitted without a seat), so the role survives a player leaving: the spectator never moves up to a seat
    /// just because it is now second in join order. Until the host has written it (or when it could not), the position
    /// decides as before: host first, then the first <c>seatCapacity - 1</c> other members in join order. No allocation.
    /// </summary>
    public static class ParticipantRoleRules
    {
        /// <summary>Session property (host-written, members-only) holding the spectator's UGS player id; absent or <see cref="NoSpectator"/> when none.</summary>
        public const string SpectatorProperty = "spectator";

        /// <summary>Value of <see cref="SpectatorProperty"/> meaning nobody (the key is never deleted: SDK 2.4.1 fails on a null value).</summary>
        public const string NoSpectator = "";

        /// <summary>The named spectator's UGS id, or null when <paramref name="value"/> names nobody (null or empty).</summary>
        public static string ReadSpectatorId(string value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }

        /// <summary>
        /// Fills <paramref name="isSpectator"/> for <paramref name="ugsIds"/> (host first, then join order;
        /// <paramref name="isHost"/> flags the host). The host is always a player; the member named by
        /// <paramref name="namedSpectatorId"/> is always the spectator and takes no seat; the rest are players while seats
        /// remain, spectators after.
        /// </summary>
        public static void Classify(ReadOnlySpan<string> ugsIds, ReadOnlySpan<bool> isHost, string namedSpectatorId,
            int seatCapacity, Span<bool> isSpectator)
        {
            int count = Math.Min(ugsIds.Length, Math.Min(isHost.Length, isSpectator.Length));
            bool hasNamed = !string.IsNullOrEmpty(namedSpectatorId);
            int seated = 0;
            for (int i = 0; i < count; i++)
            {
                bool named = hasNamed && string.Equals(ugsIds[i], namedSpectatorId, StringComparison.Ordinal);
                bool spectator = !isHost[i] && (named || seated >= seatCapacity);
                isSpectator[i] = spectator;
                if (!spectator)
                {
                    seated++;
                }
            }
        }
    }
}
