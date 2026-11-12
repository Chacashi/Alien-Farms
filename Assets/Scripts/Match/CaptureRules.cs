using AlienFarms.Animals;
using AlienFarms.Core;

namespace AlienFarms.Match
{
    /// <summary>
    /// Pure scoring rules. The score is zone occupancy: an animal counts for a zone's owner while it is captured there,
    /// whoever released it. Carried and airborne animals never count. The round's bookkeeping (score and pen contents)
    /// only changes while playing: from the moment the phase leaves Playing it is frozen, so the published result,
    /// the replicated score board and the match's own score are the same final value.
    /// </summary>
    public static class CaptureRules
    {
        /// <summary>True when an animal in <paramref name="state"/> counts for the zone of <paramref name="zoneOwner"/>.</summary>
        public static bool CountsForZone(AnimalState state, PlayerId zoneOwner)
        {
            return state == AnimalState.Captured
                && (zoneOwner == PlayerId.Player1 || zoneOwner == PlayerId.Player2);
        }

        /// <summary>
        /// True when a capture (<paramref name="state"/> in <paramref name="zoneOwner"/>'s zone) changes the score in
        /// <paramref name="phase"/>. An animal landing after the round ended (thrown in the last second, or dropped when
        /// the results freeze the players) does not count.
        /// </summary>
        public static bool CountsForZone(AnimalState state, PlayerId zoneOwner, MatchPhase phase)
        {
            return TracksScore(phase) && CountsForZone(state, zoneOwner);
        }

        /// <summary>
        /// Captures and exits only change the score (and pen contents) while the round is being played.
        /// Design decision: outside Playing an animal can be Captured inside a pen without counting (free roam while
        /// waiting for players, an animal landing after the round ended in Results). Anything that shows a pen's
        /// contents (e.g. the pen HUD of task 2.6) must read the score's bookkeeping / pen inventory events, not the
        /// animals physically in the pen. The countdown's reset clears both before the next round.
        /// </summary>
        public static bool TracksScore(MatchPhase phase) => phase == MatchPhase.Playing;

        /// <summary>
        /// Score changes are only published while the round is being played. Defined from <see cref="TracksScore"/> so
        /// both can never diverge (a tracked change that is not published would desync the HUD and the result).
        /// </summary>
        public static bool PublishesScore(MatchPhase phase) => TracksScore(phase);
    }
}
