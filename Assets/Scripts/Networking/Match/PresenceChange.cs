using AlienFarms.Core;

namespace AlienFarms.Networking.Match
{
    /// <summary>
    /// One <see cref="GameEvents.OnPlayerConnectionChanged"/> to raise, as found by <see cref="SeatPresenceRules"/>.
    /// </summary>
    public readonly struct PresenceChange
    {
        public readonly PlayerId Id;
        public readonly PlayerConnectionStatus Status;

        /// <summary>Seconds of grace left (only while <see cref="PlayerConnectionStatus.Reconnecting"/>; 0 otherwise).</summary>
        public readonly float GraceLeft;

        public PresenceChange(PlayerId id, PlayerConnectionStatus status, float graceLeft)
        {
            Id = id;
            Status = status;
            GraceLeft = graceLeft;
        }
    }
}
