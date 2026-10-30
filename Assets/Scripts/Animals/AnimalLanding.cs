namespace AlienFarms.Animals
{
    /// <summary>Pure rule that decides what an airborne animal does this physics step.</summary>
    public static class AnimalLanding
    {
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
