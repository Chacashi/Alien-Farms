using AlienFarms.Core;

namespace AlienFarms.Match
{
    /// <summary>Pure capture rule: decides whether a drop scores for its holder.</summary>
    public static class CaptureRules
    {
        /// <summary>
        /// A drop counts only when the holder released it voluntarily, during play, inside their own zone
        /// (by the actual release position). Steals and forced drops never count.
        /// </summary>
        public static bool CountsAsCapture(
            DropReason reason,
            PlayerId holder,
            PlayerId zoneOwner,
            bool releasedInsideZone,
            bool capturesEnabled)
        {
            return capturesEnabled
                && reason == DropReason.Released
                && holder != PlayerId.None
                && zoneOwner == holder
                && releasedInsideZone;
        }
    }
}
