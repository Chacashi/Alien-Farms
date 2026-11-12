using System.Globalization;
using System.Text;

namespace AlienFarms.Core
{
    /// <summary>
    /// Cleans a player name that came from another machine (connection payload, session player property) before it is
    /// stored or shown: drops control and invisible format characters (zero-width, bidi overrides) and the rich text
    /// brackets '&lt;' / '&gt;' (TextMeshPro would parse tags), trims surrounding white space and caps it at
    /// <see cref="MaxLength"/> characters (never splitting a surrogate pair). A name that is empty afterwards stays empty
    /// (callers show their own placeholder). Every cut (<see cref="MaxLength"/> characters, <see cref="FitToPayload"/>
    /// bytes) keeps a trailing UGS discriminator ("Name#1234") and shortens the part before it. Pure.
    /// </summary>
    public static class DisplayNameSanitizer
    {
        /// <summary>Longest name kept, in UTF-16 characters (the connection payload already holds at most 29 UTF-8 bytes).</summary>
        public const int MaxLength = 32;

        /// <summary>
        /// Longest name the player may type (input field limit). Below <see cref="MaxLength"/> so a typed name with
        /// accents (2 UTF-8 bytes each) still fits the connection payload's 29 bytes, and stays readable in a name tag.
        /// </summary>
        public const int MaxTypedLength = 14;

        /// <summary>
        /// Most UTF-8 bytes of a name on the wire: the connection payload and the name NetworkVariables hold it in a
        /// FixedString32Bytes (29 bytes; checked by tests).
        /// </summary>
        public const int MaxPayloadBytes = 29;

        // Discriminator of a generated UGS name: '#' followed by digits at the end ("ExceptionalDenyingLightbulb#78609").
        private const char DiscriminatorMark = '#';

        /// <summary>
        /// <paramref name="name"/> cut to <see cref="MaxPayloadBytes"/> UTF-8 bytes on a code point, keeping its trailing
        /// discriminator (see <see cref="Fit"/>). Never null. Returns the same instance when it already fits.
        /// </summary>
        public static string FitToPayload(string name)
        {
            return Fit(name, MaxPayloadBytes, true);
        }

        /// <summary>
        /// Cuts <paramref name="name"/> to <paramref name="budget"/> UTF-8 bytes (<paramref name="inUtf8Bytes"/>) or UTF-16
        /// characters, never splitting a surrogate pair. A trailing "#digits" discriminator that fits is always kept: the
        /// part before it is shortened (and trimmed) instead, unless nothing of that part would be left (then it is a plain
        /// cut). Never null; the same instance when nothing is cut.
        /// </summary>
        public static string Fit(string name, int budget, bool inUtf8Bytes)
        {
            if (string.IsNullOrEmpty(name) || budget <= 0)
            {
                return string.Empty;
            }

            if (Measure(name, 0, name.Length, inUtf8Bytes) <= budget)
            {
                return name;
            }

            int mark = FindDiscriminator(name);
            if (mark > 0)
            {
                int suffixSize = Measure(name, mark, name.Length, inUtf8Bytes);
                if (suffixSize < budget)
                {
                    string head = name.Substring(0, TakeLength(name, mark, budget - suffixSize, inUtf8Bytes)).TrimEnd();
                    if (head.Length > 0)
                    {
                        return head + name.Substring(mark);
                    }

                    // No room left for any of the name: a bare "#1234" would say nothing, cut plainly instead.
                }
            }

            return name.Substring(0, TakeLength(name, name.Length, budget, inUtf8Bytes)).TrimEnd();
        }

        /// <summary>The cleaned <paramref name="name"/>; never null. Returns the same instance when nothing changes.</summary>
        public static string Sanitize(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            if (IsClean(name))
            {
                return name;
            }

            var builder = new StringBuilder(name.Length);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (IsAllowed(c))
                {
                    builder.Append(c);
                }
            }

            string clean = builder.ToString().Trim();
            return clean.Length > MaxLength ? Fit(clean, MaxLength, false) : clean;
        }

        /// <summary>True when <see cref="Sanitize"/> would return <paramref name="name"/> unchanged.</summary>
        public static bool IsClean(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return true;
            }

            if (name.Length > MaxLength || char.IsWhiteSpace(name[0]) || char.IsWhiteSpace(name[name.Length - 1]))
            {
                return false;
            }

            for (int i = 0; i < name.Length; i++)
            {
                if (!IsAllowed(name[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Index of the '#' of a trailing "#digits" discriminator after at least one character; -1 when none.
        private static int FindDiscriminator(string name)
        {
            int i = name.Length - 1;
            while (i >= 0 && name[i] >= '0' && name[i] <= '9')
            {
                i--;
            }

            bool hasDigits = i < name.Length - 1;
            return hasDigits && i > 0 && name[i] == DiscriminatorMark ? i : -1;
        }

        // Size of name[start..end) in UTF-8 bytes or UTF-16 characters.
        private static int Measure(string name, int start, int end, bool inUtf8Bytes)
        {
            if (!inUtf8Bytes)
            {
                return end - start;
            }

            int size = 0;
            for (int i = start; i < end; i += CodePointChars(name, i, end))
            {
                size += CodePointBytes(name, i, end);
            }

            return size;
        }

        // Longest prefix of name[0..end) (in chars) whose size fits the budget, ending on a code point.
        private static int TakeLength(string name, int end, int budget, bool inUtf8Bytes)
        {
            int size = 0;
            int i = 0;
            while (i < end)
            {
                int chars = CodePointChars(name, i, end);
                int width = inUtf8Bytes ? CodePointBytes(name, i, end) : chars;
                if (size + width > budget)
                {
                    break;
                }

                size += width;
                i += chars;
            }

            return i;
        }

        private static int CodePointChars(string name, int i, int end)
        {
            return char.IsHighSurrogate(name[i]) && i + 1 < end && char.IsLowSurrogate(name[i + 1]) ? 2 : 1;
        }

        // UTF-8 width of the code point at i (a lone surrogate is encoded as U+FFFD: 3 bytes).
        private static int CodePointBytes(string name, int i, int end)
        {
            char c = name[i];
            if (c < 0x80)
            {
                return 1;
            }

            if (c < 0x800)
            {
                return 2;
            }

            return CodePointChars(name, i, end) == 2 ? 4 : 3;
        }

        private static bool IsAllowed(char c)
        {
            return c != '<'
                && c != '>'
                && !char.IsControl(c)
                && char.GetUnicodeCategory(c) != UnicodeCategory.Format;
        }
    }
}
