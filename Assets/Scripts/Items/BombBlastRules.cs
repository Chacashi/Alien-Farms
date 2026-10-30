using AlienFarms.Core;

namespace AlienFarms.Items
{
    /// <summary>Pure rules of the stun bomb (who gets stunned, when it explodes). No Unity state, fully testable.</summary>
    public static class BombBlastRules
    {
        /// <summary>
        /// A target is stunned when it is a real player other than the thrower and within <paramref name="radius"/>
        /// (edge inclusive). A bomb without thrower (<see cref="PlayerId.None"/>) makes nobody immune.
        /// </summary>
        public static bool ShouldStun(PlayerId thrower, PlayerId target, float sqrDistanceToTarget, float radius)
        {
            if (target == PlayerId.None || target == thrower)
            {
                return false;
            }

            return sqrDistanceToTarget <= radius * radius;
        }

        /// <summary>Only an armed bomb explodes on contact, and never against its own thrower.</summary>
        public static bool ShouldDetonateOnContact(BombState state, bool isThrowerBody)
        {
            return state == BombState.Armed && !isThrowerBody;
        }

        /// <summary>True once an armed bomb has flown for <paramref name="maxArmedTime"/> seconds or more.</summary>
        public static bool HasTimedOut(float armedTime, float maxArmedTime)
        {
            return armedTime >= maxArmedTime;
        }
    }
}
