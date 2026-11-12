namespace AlienFarms.Networking.Session
{
    /// <summary>What the server does with the seat of a client that disconnected (see <see cref="SeatGraceRules.OnDisconnect"/>).</summary>
    public enum SeatDropOutcome : byte
    {
        /// <summary>Freed at once without notice (grace off, e.g. the MainMenu room).</summary>
        Released = 0,

        /// <summary>The client announced it was leaving ("AF.Leaving"): freed at once and reported as Left.</summary>
        Left = 1,

        /// <summary>Lost without notice while the grace is on: kept for its player (MarkDisconnected).</summary>
        Reserved = 2
    }
}
