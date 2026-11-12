using AlienFarms.Networking.Pooling;

namespace AlienFarms.Networking.Diagnostics
{
    /// <summary>
    /// Writes a <see cref="NetworkStatsSample"/> into a reusable char buffer (no allocation; shown with
    /// <c>TMP_Text.SetCharArray</c>). Pure, so the layout is unit tested. Output truncates if the buffer is too small.
    /// <code>
    /// Pool  activos/en pool/creados
    /// Animales  12/78/90
    /// Bombas  1/2/3
    /// Rx  150 pkt/s  cola 2.4  max 41/1024
    /// Frame max  612 ms
    /// </code>
    /// </summary>
    public static class NetworkStatsFormatter
    {
        /// <summary>Buffer length that always fits the full text.</summary>
        public const int BufferSize = 256;

        private const string Header = "Pool  activos/en pool/creados";
        private const string AnimalsLabel = "\nAnimales  ";
        private const string BombsLabel = "\nBombas  ";
        private const string RxLabel = "\nRx  ";
        private const string RxUnit = " pkt/s  cola ";
        private const string MaxLabel = "  max ";
        private const string FrameLabel = "\nFrame max  ";
        private const string FrameUnit = " ms";

        /// <summary>Writes the sample from index 0 and returns the length written.</summary>
        public static int Write(char[] buffer, in NetworkStatsSample sample)
        {
            if (buffer == null)
            {
                return 0;
            }

            int length = 0;
            Append(buffer, ref length, Header);
            Append(buffer, ref length, AnimalsLabel);
            AppendPool(buffer, ref length, sample.Animals);
            Append(buffer, ref length, BombsLabel);
            AppendPool(buffer, ref length, sample.Bombs);

            if (sample.HasTransport)
            {
                Append(buffer, ref length, RxLabel);
                Append(buffer, ref length, sample.RxPacketsPerSecond);
                Append(buffer, ref length, RxUnit);
                AppendTenths(buffer, ref length, sample.RxQueueMeanTenths);
                Append(buffer, ref length, MaxLabel);
                Append(buffer, ref length, sample.RxQueueMax);
                Append(buffer, ref length, '/');
                Append(buffer, ref length, sample.QueueCapacity);
                Append(buffer, ref length, FrameLabel);
                Append(buffer, ref length, sample.LongestFrameMs);
                Append(buffer, ref length, FrameUnit);
            }

            return length;
        }

        private static void AppendPool(char[] buffer, ref int length, NetworkPoolStats stats)
        {
            Append(buffer, ref length, stats.Active);
            Append(buffer, ref length, '/');
            Append(buffer, ref length, stats.Inactive);
            Append(buffer, ref length, '/');
            Append(buffer, ref length, stats.Created);
        }

        private static void AppendTenths(char[] buffer, ref int length, int tenths)
        {
            // long: -int.MinValue does not fit in an int.
            long remaining = tenths;
            if (remaining < 0)
            {
                Append(buffer, ref length, '-');
                remaining = -remaining;
            }

            AppendDigits(buffer, ref length, remaining / 10);
            Append(buffer, ref length, '.');
            Append(buffer, ref length, (char)('0' + remaining % 10));
        }

        private static void Append(char[] buffer, ref int length, string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                Append(buffer, ref length, text[i]);
            }
        }

        private static void Append(char[] buffer, ref int length, char character)
        {
            if (length < buffer.Length)
            {
                buffer[length++] = character;
            }
        }

        private static void Append(char[] buffer, ref int length, int value)
        {
            // long: -int.MinValue does not fit in an int.
            long remaining = value;
            if (remaining < 0)
            {
                Append(buffer, ref length, '-');
                remaining = -remaining;
            }

            AppendDigits(buffer, ref length, remaining);
        }

        /// <summary>Digits of a non-negative value.</summary>
        private static void AppendDigits(char[] buffer, ref int length, long remaining)
        {
            long divisor = 1;
            while (remaining / divisor >= 10)
            {
                divisor *= 10;
            }

            while (divisor > 0)
            {
                Append(buffer, ref length, (char)('0' + remaining / divisor % 10));
                divisor /= 10;
            }
        }
    }
}
