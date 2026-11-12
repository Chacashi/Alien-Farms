namespace AlienFarms.Networking.Player
{
    /// <summary>
    /// Pure rule of the default character of a seat that reached the gameplay scene without a pick (e.g. a new client
    /// taking a seat freed mid-match): the seat's own index, or the next free one when the other seat already uses it.
    /// </summary>
    public static class CharacterPickRules
    {
        /// <summary>
        /// Character for seat <paramref name="seatIndex"/> (0 = Player 1) given the character the other seat uses
        /// (<paramref name="otherCharacter"/>, negative = none) and the roster size. With a single character both share
        /// it; an empty roster returns 0 (the caller then has no definition to apply).
        /// </summary>
        public static int Pick(int seatIndex, int otherCharacter, int rosterCount)
        {
            if (rosterCount <= 1)
            {
                return 0;
            }

            int pick = seatIndex > 0 ? seatIndex % rosterCount : 0;
            if (pick == otherCharacter)
            {
                pick = (pick + 1) % rosterCount;
            }

            return pick;
        }
    }
}
