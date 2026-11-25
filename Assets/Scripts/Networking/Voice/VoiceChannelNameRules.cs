using System.Text.RegularExpressions;

namespace AlienFarms.Networking.Voice
{
    public static class VoiceChannelNameRules
    {
        public const int MaxLength = 200;

        // S14 uses a subset of the SDK alphabet and omits '%'.
        private static readonly Regex InvalidCharacters = new Regex("[^A-Za-z0-9_.!()+=~-]");

        public static string Build(string prefix, string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return null;
            }

            string id = InvalidCharacters.Replace(sessionId, string.Empty);
            if (id.Length == 0)
            {
                return null;
            }

            string name = InvalidCharacters.Replace(prefix ?? string.Empty, string.Empty) + id;
            return name.Length <= MaxLength ? name : name.Substring(0, MaxLength);
        }
    }
}
