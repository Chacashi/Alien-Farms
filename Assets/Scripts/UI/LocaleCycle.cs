namespace AlienFarms.UI
{
    /// <summary>Index math for cycling through a list of locales with wrap-around.</summary>
    public static class LocaleCycle
    {
        /// <summary>
        /// Index reached by moving <paramref name="step"/> positions from <paramref name="current"/> in a list of
        /// <paramref name="count"/> items, wrapping at both ends. Returns -1 when the list is empty.
        /// An out-of-range <paramref name="current"/> (e.g. -1 = not found) is treated as 0 before stepping.
        /// </summary>
        public static int Next(int current, int count, int step)
        {
            if (count <= 0)
            {
                return -1;
            }

            int start = current >= 0 && current < count ? current : 0;
            int result = (start + step) % count;
            return result < 0 ? result + count : result;
        }
    }
}
