namespace AlienFarms.Core
{
    /// <summary>
    /// Local check of a typed session join code before calling the service: trims, upper-cases and accepts only the
    /// characters the service issues. Session codes are lobby codes; like Relay join codes (documented in
    /// com.unity.services.multiplayer, Relay JoinRequest) they are case-insensitive, 6 to 12 characters long and use
    /// only <see cref="Alphabet"/> (no vowels nor look-alike characters such as 0/O, 1/I, 5/S, Z).
    /// </summary>
    public static class SessionCodeFormat
    {
        public const int MinLength = 6;
        public const int MaxLength = 12;

        /// <summary>Every character a service join code can contain (upper case).</summary>
        public const string Alphabet = "6789BCDFGHJKLMNPQRTW";

        /// <summary>True when <paramref name="raw"/> is a plausible code; <paramref name="normalized"/> is trimmed and upper-case.</summary>
        public static bool TryNormalize(string raw, out string normalized)
        {
            normalized = null;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            string trimmed = raw.Trim();
            if (trimmed.Length < MinLength || trimmed.Length > MaxLength)
            {
                return false;
            }

            string upper = trimmed.ToUpperInvariant();
            for (int i = 0; i < upper.Length; i++)
            {
                if (Alphabet.IndexOf(upper[i]) < 0)
                {
                    return false;
                }
            }

            normalized = upper;
            return true;
        }
    }
}
