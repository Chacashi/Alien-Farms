using AlienFarms.Core;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Name sent in the connection payload and the session: the one typed on this device, else the generated UGS name,
    /// else the serialized fallback tagged with this process's instance (MPPM clones share PlayerPrefs and, without UGS,
    /// would all be "Player"). Pure; every candidate is sanitized (<see cref="DisplayNameSanitizer"/>).
    /// </summary>
    public static class DisplayNameResolver
    {
        public const string DefaultName = "Player";

        /// <param name="typedName">Name from the online channel (may be empty).</param>
        /// <param name="ugsName">UGS player name (null when not signed in or not fetched).</param>
        /// <param name="fallbackName">Serialized fallback (e.g. "Player").</param>
        /// <param name="instanceTag">This process's MPPM / profile tag (null on the main editor or a plain build).</param>
        public static string Resolve(string typedName, string ugsName, string fallbackName, string instanceTag)
        {
            string typed = DisplayNameSanitizer.Sanitize(typedName);
            if (typed.Length > 0)
            {
                return typed;
            }

            string ugs = DisplayNameSanitizer.Sanitize(ugsName);
            if (ugs.Length > 0)
            {
                return ugs;
            }

            string fallback = DisplayNameSanitizer.Sanitize(fallbackName);
            if (fallback.Length == 0)
            {
                fallback = DefaultName;
            }

            string tag = DisplayNameSanitizer.Sanitize(instanceTag);
            return tag.Length > 0 ? DisplayNameSanitizer.Sanitize(fallback + " " + tag) : fallback;
        }
    }
}
