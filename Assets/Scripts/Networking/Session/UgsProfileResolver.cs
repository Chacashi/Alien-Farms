using System;
using System.Collections.Generic;
using System.Text;

namespace AlienFarms.Networking.Session
{
    /// <summary>
    /// Picks the Unity Authentication profile of this process so every Multiplayer Play Mode instance (and every build
    /// started on the same PC) signs in as a different anonymous player. Editor PlayerPrefs are shared by the MPPM
    /// clones, so without a profile they would all reuse one session token and one PlayerId. Pure, so it is tested.
    /// Priority: an explicit <c>-ugs-profile &lt;name&gt;</c> argument, then the MPPM <c>-name PlayerN</c> argument of a
    /// virtual player; the main editor (and a build without arguments) keeps the default profile (null).
    /// </summary>
    public static class UgsProfileResolver
    {
        /// <summary>Command-line argument that forces a profile (e.g. two builds on one PC).</summary>
        public const string ProfileArgument = "-ugs-profile";

        /// <summary>Argument MPPM passes to each instance: Player1 (main editor) to Player4.</summary>
        public const string MppmNameArgument = "-name";

        /// <summary>Unity Authentication accepts [a-zA-Z0-9_-] up to 30 characters.</summary>
        public const int MaxProfileLength = 30;

        /// <summary>Profile for this process, or null for the default one.</summary>
        /// <param name="commandLineArgs">Process arguments (Environment.GetCommandLineArgs()).</param>
        /// <param name="isMainEditor">True on the MPPM main editor; its <c>-name</c> is ignored so it keeps the default profile.</param>
        public static string Resolve(IReadOnlyList<string> commandLineArgs, bool isMainEditor)
        {
            string explicitProfile = Sanitize(FindArgumentValue(commandLineArgs, ProfileArgument));
            if (explicitProfile != null)
            {
                return explicitProfile;
            }

            if (isMainEditor)
            {
                return null;
            }

            return Sanitize(FindArgumentValue(commandLineArgs, MppmNameArgument));
        }

        /// <summary>
        /// Keeps the characters Authentication accepts (lower-cased), truncated to <see cref="MaxProfileLength"/>.
        /// Null when nothing valid remains.
        /// </summary>
        public static string Sanitize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            var builder = new StringBuilder(Math.Min(raw.Length, MaxProfileLength));
            for (int i = 0; i < raw.Length && builder.Length < MaxProfileLength; i++)
            {
                char c = raw[i];
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_' || c == '-')
                {
                    builder.Append(c);
                }
                else if (c >= 'A' && c <= 'Z')
                {
                    builder.Append((char)(c + ('a' - 'A')));
                }
            }

            return builder.Length > 0 ? builder.ToString() : null;
        }

        /// <summary>Value following <paramref name="argument"/> (case-insensitive), or null when absent or last.</summary>
        public static string FindArgumentValue(IReadOnlyList<string> args, string argument)
        {
            if (args == null)
            {
                return null;
            }

            for (int i = 0; i < args.Count - 1; i++)
            {
                if (string.Equals(args[i], argument, StringComparison.OrdinalIgnoreCase))
                {
                    string value = args[i + 1];
                    return value != null && value.StartsWith("-", StringComparison.Ordinal) ? null : value;
                }
            }

            return null;
        }
    }
}
