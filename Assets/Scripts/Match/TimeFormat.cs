namespace AlienFarms.Match
{
    /// <summary>Allocation-free time formatting into reusable char buffers.</summary>
    public static class TimeFormat
    {
        /// <summary>Minimum buffer length accepted by <see cref="WriteMinutesSeconds"/>.</summary>
        public const int MinBufferLength = 6;

        /// <summary>
        /// Writes <paramref name="totalSeconds"/> as "m:ss" (minutes unpadded, e.g. "5:00", "10:05"; negatives as "0:00")
        /// into <paramref name="buffer"/> and returns the number of chars written. Buffer must hold at least
        /// <see cref="MinBufferLength"/> chars (enough for up to 999 minutes).
        /// </summary>
        public static int WriteMinutesSeconds(int totalSeconds, char[] buffer)
        {
            if (totalSeconds < 0)
            {
                totalSeconds = 0;
            }

            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            int length = 0;
            if (minutes >= 100)
            {
                buffer[length++] = (char)('0' + minutes / 100 % 10);
            }

            if (minutes >= 10)
            {
                buffer[length++] = (char)('0' + minutes / 10 % 10);
            }

            buffer[length++] = (char)('0' + minutes % 10);
            buffer[length++] = ':';
            buffer[length++] = (char)('0' + seconds / 10);
            buffer[length++] = (char)('0' + seconds % 10);
            return length;
        }
    }
}
