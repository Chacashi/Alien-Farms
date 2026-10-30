using UnityEngine;

namespace AlienFarms.Animals
{
    /// <summary>Pure rule that decides what an airborne animal does this physics step.</summary>
    public static class AnimalLanding
    {
        private const float Epsilon = 0.0001f;

        /// <summary>
        /// Stuck on scenery: touching Environment, almost still, neither grounded nor on a player (e.g. wedged on a
        /// fence edge or a rounded top the ground probe does not see).
        /// </summary>
        public static bool IsStuckOnEnvironment(bool touchingEnvironment, bool grounded, bool onPlayer, float speedSqr, float maxStuckSpeed)
        {
            return touchingEnvironment && !grounded && !onPlayer && speedSqr <= maxStuckSpeed * maxStuckSpeed;
        }

        /// <summary>
        /// Stuck timer: while not <paramref name="stuck"/> it restarts at <paramref name="now"/>. True once the animal has
        /// been stuck for <paramref name="maxStuckTime"/> and fewer than <paramref name="maxPushes"/> pushes were used this
        /// flight; the caller pushes it off and restarts the timer. Out of pushes (wedged), the air timeout takes over.
        /// </summary>
        public static bool ShouldPushOff(bool stuck, float now, ref float stuckSince, float maxStuckTime, int pushesUsed, int maxPushes)
        {
            if (!stuck)
            {
                stuckSince = now;
                return false;
            }

            return pushesUsed < maxPushes && now - stuckSince >= maxStuckTime;
        }

        /// <summary>
        /// Planar unit direction to push a stuck animal off scenery: from the contact point towards the body center,
        /// else along the contact normal, else <paramref name="fallback"/>, else back (-Z).
        /// </summary>
        public static Vector3 PushOffDirection(Vector3 bodyCenter, Vector3 contactPoint, Vector3 contactNormal, Vector3 fallback)
        {
            Vector3 direction = new Vector3(bodyCenter.x - contactPoint.x, 0f, bodyCenter.z - contactPoint.z);
            if (direction.sqrMagnitude < Epsilon)
            {
                direction = new Vector3(contactNormal.x, 0f, contactNormal.z);
            }

            if (direction.sqrMagnitude < Epsilon)
            {
                direction = new Vector3(fallback.x, 0f, fallback.z);
            }

            return direction.sqrMagnitude < Epsilon ? Vector3.back : direction.normalized;
        }

        /// <summary>
        /// Keeps the airborne timers in step with player contact: contact start stamps <paramref name="onPlayerSince"/>;
        /// leaving the player restarts <paramref name="airborneSince"/> (a fresh fall, so it is never despawned the
        /// instant it gets free).
        /// </summary>
        public static void TrackPlayerContact(
            bool onPlayer, bool wasOnPlayer, float now, ref float airborneSince, ref float onPlayerSince)
        {
            if (onPlayer && !wasOnPlayer)
            {
                onPlayerSince = now;
            }
            else if (!onPlayer && wasOnPlayer)
            {
                airborneSince = now;
            }
        }

        /// <summary>
        /// Despawn when below <paramref name="killHeight"/> (fell off the map). Land after at least
        /// <paramref name="minAirTime"/> when grounded and not moving up faster than <paramref name="maxLandingUpSpeed"/>,
        /// or at the timeout when grounded. While resting on a player (ground wins): relocate once
        /// <paramref name="onPlayerTime"/> reaches <paramref name="maxOnPlayerTime"/>, otherwise slide off when settled;
        /// the air timeout does not apply on a player (bounded by the relocation instead). Despawn when
        /// <paramref name="maxAirTime"/> is reached without ground below (stuck or lost). Otherwise keep flying.
        /// </summary>
        public static AirborneOutcome Evaluate(
            float airTime,
            float verticalSpeed,
            bool grounded,
            bool onPlayer,
            float onPlayerTime,
            float maxOnPlayerTime,
            float height,
            float killHeight,
            float minAirTime,
            float maxAirTime,
            float maxLandingUpSpeed)
        {
            if (height < killHeight)
            {
                return AirborneOutcome.Despawn;
            }

            bool settled = airTime >= minAirTime && verticalSpeed <= maxLandingUpSpeed;
            if (grounded && (settled || airTime >= maxAirTime))
            {
                return AirborneOutcome.Land;
            }

            if (onPlayer)
            {
                if (onPlayerTime >= maxOnPlayerTime)
                {
                    return AirborneOutcome.Relocate;
                }

                return settled ? AirborneOutcome.SlideOff : AirborneOutcome.Keep;
            }

            return airTime >= maxAirTime ? AirborneOutcome.Despawn : AirborneOutcome.Keep;
        }
    }
}
