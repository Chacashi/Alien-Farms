using System;
using AlienFarms.Data;

namespace AlienFarms.Networking.Session
{
    /// <summary>Session limits the <see cref="ApprovalRules"/> apply. Immutable; built from <see cref="NetworkSettings"/>.</summary>
    public readonly struct ApprovalPolicy
    {
        public ApprovalPolicy(int protocolVersion, int playerSeats, float reconnectGraceSeconds)
        {
            ProtocolVersion = protocolVersion;
            PlayerSeats = Math.Max(0, Math.Min(playerSeats, ServerSlotDirectory.Capacity));
            ReconnectGraceSeconds = Math.Max(0f, reconnectGraceSeconds);
        }

        /// <summary>Clients with another <see cref="ConnectionPayload.ProtocolVersion"/> are rejected.</summary>
        public int ProtocolVersion { get; }

        /// <summary>Player seats on offer (Player1 first), at most <see cref="ServerSlotDirectory.Capacity"/>.</summary>
        public int PlayerSeats { get; }

        /// <summary>Seconds a dropped player's seat stays reserved for its <see cref="PlayerSlot.UgsPlayerId"/>.</summary>
        public float ReconnectGraceSeconds { get; }

        /// <summary>Policy for the configured protocol, player seats and reconnection grace.</summary>
        public static ApprovalPolicy From(NetworkSettings settings)
        {
            int seats = Math.Min(settings.MaxPlayers, ServerSlotDirectory.Capacity);
            return new ApprovalPolicy(settings.ProtocolVersion, seats, settings.ReconnectGraceSeconds);
        }
    }
}
