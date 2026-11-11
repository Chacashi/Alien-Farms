using System.Globalization;
using System.Text;

namespace AlienFarms.Core
{
    /// <summary>
    /// Cleans a player name that came from another machine (connection payload, session player property) before it is
    /// stored or shown: drops control and invisible format characters (zero-width, bidi overrides) and the rich text
    /// brackets '&lt;' / '&gt;' (TextMeshPro would parse tags), trims surrounding white space and caps it at
    /// <see cref="MaxLength"/> characters (never splitting a surrogate pair). A name that is empty afterwards stays empty
    /// (callers show their own placeholder). Pure.
    /// </summary>
    public static class DisplayNameSanitizer
    {
        /// <summary>Longest name kept, in UTF-16 characters (the connection payload already holds at most 29 UTF-8 bytes).</summary>
        public const int MaxLength = 32;

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
            if (clean.Length > MaxLength)
            {
                int cut = char.IsHighSurrogate(clean[MaxLength - 1]) ? MaxLength - 1 : MaxLength;
                clean = clean.Substring(0, cut).TrimEnd();
            }

            return clean;
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

        private static bool IsAllowed(char c)
        {
            return c != '<'
                && c != '>'
                && !char.IsControl(c)
                && char.GetUnicodeCategory(c) != UnicodeCategory.Format;
        }
    }
}
