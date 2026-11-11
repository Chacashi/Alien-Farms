using Unity.Collections;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Server-side record of a spectator: a connection admitted without a player seat or avatar
    /// (<see cref="ApprovalOutcome.Spectator"/>, only when <see cref="AlienFarms.Data.NetworkSettings.AllowSpectator"/>).
    /// </summary>
    public struct SpectatorSeat
    {
        public bool InUse;
        public ulong ClientId;
        public FixedString32Bytes DisplayName;
        public FixedString64Bytes UgsPlayerId;
    }
}
