using AlienFarms.Animals;
using AlienFarms.Core;

namespace AlienFarms.Match
{
    /// <summary>
    /// Pure scoring rules. The score is zone occupancy: an animal counts for a zone's owner while it is captured there,
    /// whoever released it. Carried and airborne animals never count.
    /// </summary>
    public static class CaptureRules
    {
        /// <summary>True when an animal in <paramref name="state"/> counts for the zone of <paramref name="zoneOwner"/>.</summary>
        public static bool CountsForZone(AnimalState state, PlayerId zoneOwner)
        {
            return state == AnimalState.Captured
                && (zoneOwner == PlayerId.Player1 || zoneOwner == PlayerId.Player2);
        }

        /// <summary>Score changes are tracked in every phase but only published while the round is being played.</summary>
        public static bool PublishesScore(MatchPhase phase) => phase == MatchPhase.Playing;
    }
}
