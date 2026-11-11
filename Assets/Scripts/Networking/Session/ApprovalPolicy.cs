using System;
using AlienFarms.Data;

namespace AlienFarms.Networking.Session
{
    /// <summary>Session limits the <see cref="ApprovalRules"/> apply. Immutable; built from <see cref="NetworkSettings"/>.</summary>
    public readonly struct ApprovalPolicy
    {
        public ApprovalPolicy(int protocolVersion, int playerSeats, int maxSpectators, float reconnectGraceSeconds)
        {
            ProtocolVersion = protocolVersion;
            PlayerSeats = Math.Max(0, Math.Min(playerSeats, ServerSlotDirectory.Capacity));
            MaxSpectators = Math.Max(0, Math.Min(maxSpectators, ServerSlotDirectory.SpectatorCapacity));
            ReconnectGraceSeconds = Math.Max(0f, reconnectGraceSeconds);
        }

        /// <summary>Clients with another <see cref="ConnectionPayload.ProtocolVersion"/> are rejected.</summary>
        public int ProtocolVersion { get; }

        /// <summary>Player seats on offer (Player1 first), at most <see cref="ServerSlotDirectory.Capacity"/>.</summary>
        public int PlayerSeats { get; }

        /// <summary>Extra connections admitted as spectators once the seats are taken (0 = none).</summary>
        public int MaxSpectators { get; }

        /// <summary>Seconds a dropped player's seat stays reserved for its <see cref="PlayerSlot.UgsPlayerId"/>.</summary>
        public float ReconnectGraceSeconds { get; }

        /// <summary>
        /// Policy of <paramref name="settings"/>: two player seats and, only with <see cref="NetworkSettings.AllowSpectator"/>,
        /// the connections above them (<see cref="NetworkSettings.MaxPlayers"/> - 2) as spectators.
        /// </summary>
        public static ApprovalPolicy From(NetworkSettings settings)
        {
            int seats = Math.Min(settings.MaxPlayers, ServerSlotDirectory.Capacity);
            int spectators = settings.AllowSpectator ? settings.MaxPlayers - seats : 0;
            return new ApprovalPolicy(settings.ProtocolVersion, seats, spectators, settings.ReconnectGraceSeconds);
        }
    }
}
