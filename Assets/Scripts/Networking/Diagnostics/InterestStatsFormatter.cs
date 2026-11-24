using System;
using System.Globalization;

namespace AlienFarms.Networking.Diagnostics
{
    public static class InterestStatsFormatter
    {
        public const int BufferSize = 384;

        public static int Write(char[] buffer, in InterestStatsSample sample)
        {
            Span<char> output = buffer;
            int offset = 0;
            if (sample.IsHost)
            {
                Append(output, ref offset, sample.IsActive ? "AOI ON " : "AOI OFF ");
                AppendRadius(output, ref offset, sample.EnterRadius);
                Append(output, ref offset, "/");
                AppendRadius(output, ref offset, sample.ExitRadius);
                Append(output, ref offset, " m \u00b7 cada ");
                AppendNumber(output, ref offset, sample.ReviewIntervalTicks);
                Append(output, ref offset, " ticks");
            }
            else
            {
                Append(output, ref offset, "AOI: servidor");
            }

            Append(output, ref offset, "\nConocidos ");
            AppendNumber(output, ref offset, sample.Known.Total);
            Append(output, ref offset, "  anim ");
            AppendNumber(output, ref offset, sample.Known.Animals);
            Append(output, ref offset, " \u00b7 bombas ");
            AppendNumber(output, ref offset, sample.Known.Bombs);
            Append(output, ref offset, " \u00b7 otros ");
            AppendNumber(output, ref offset, sample.Known.Other);

            if (sample.IsHost)
            {
                Append(output, ref offset, "\nP2 ve  anim ");
                AppendNumber(output, ref offset, sample.Remote.VisibleAnimals);
                Append(output, ref offset, "/");
                AppendNumber(output, ref offset, sample.TotalAnimals);
                Append(output, ref offset, " \u00b7 bombas ");
                AppendNumber(output, ref offset, sample.Remote.VisibleBombs);
                Append(output, ref offset, "/");
                AppendNumber(output, ref offset, sample.TotalBombs);
                Append(output, ref offset, "\nAltas ");
                AppendNumber(output, ref offset, sample.Remote.Shows);
                Append(output, ref offset, "  Bajas ");
                AppendNumber(output, ref offset, sample.Remote.Hides);
                Append(output, ref offset, "  \u00b7 ventana ");
                AppendNumber(output, ref offset, sample.WindowSeconds);
                Append(output, ref offset, " s");
            }

            Append(output, ref offset, sample.IsTx ? "\nTx ventana " : "\nRx ventana ");
            AppendNumber(output, ref offset, sample.TrafficKilobytes);
            Append(output, ref offset, " KB \u00b7 ");
            AppendNumber(output, ref offset, sample.TrafficKilobytesPerSecondTenths / 10);
            Append(output, ref offset, ".");
            AppendNumber(output, ref offset, sample.TrafficKilobytesPerSecondTenths % 10);
            Append(output, ref offset, " KB/s");

            return offset;
        }

        private static void Append(Span<char> output, ref int offset, ReadOnlySpan<char> value)
        {
            Span<char> remaining = output.Slice(offset);
            int length = Math.Min(remaining.Length, value.Length);
            value.Slice(0, length).CopyTo(remaining);
            offset += length;
        }

        private static void AppendNumber(Span<char> output, ref int offset, long value)
        {
            Span<char> digits = stackalloc char[20];
            if (value.TryFormat(digits, out int length, provider: CultureInfo.InvariantCulture))
            {
                Append(output, ref offset, digits.Slice(0, length));
            }
        }

        private static void AppendRadius(Span<char> output, ref int offset, float value)
        {
            Span<char> digits = stackalloc char[32];
            if (value.TryFormat(digits, out int length, "G4", CultureInfo.InvariantCulture))
            {
                Append(output, ref offset, digits.Slice(0, length));
            }
        }
    }
}
