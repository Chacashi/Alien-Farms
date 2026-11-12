using AlienFarms.Core;
using UnityEngine;

namespace AlienFarms.UI
{
    /// <summary>
    /// Pure rules of <see cref="ConnectionStatusView"/>: which banner a seat's <see cref="PlayerConnectionStatus"/> shows,
    /// which localization key it uses and how the grace countdown is displayed. No allocation.
    /// </summary>
    public static class ConnectionStatusRules
    {
        /// <param name="status">New status of the seat.</param>
        /// <param name="graceLeft">Seconds of grace left (only meaningful while reconnecting).</param>
        /// <param name="wasReconnecting">The seat was reserved before this change (Connected then means it came back).</param>
        public static ConnectionBanner Resolve(PlayerConnectionStatus status, float graceLeft, bool wasReconnecting)
        {
            switch (status)
            {
                case PlayerConnectionStatus.Reconnecting:
                    return ConnectionBanner.Reconnecting;
                case PlayerConnectionStatus.Left:
                    return ConnectionBanner.Left;
                case PlayerConnectionStatus.TimedOut:
                    return ConnectionBanner.TimedOut;
                default:
                    // A late instance also publishes Connected for every held seat: only a return is news.
                    return wasReconnecting ? ConnectionBanner.Returned : ConnectionBanner.None;
            }
        }

        /// <summary>Toasts hide on their own after a while; the countdown stays until the seat changes again.</summary>
        public static bool IsToast(ConnectionBanner banner)
        {
            return banner == ConnectionBanner.Returned
                || banner == ConnectionBanner.Left
                || banner == ConnectionBanner.TimedOut;
        }

        /// <summary>Localization key of <paramref name="banner"/> (null for <see cref="ConnectionBanner.None"/>).</summary>
        public static string Key(ConnectionBanner banner)
        {
            switch (banner)
            {
                case ConnectionBanner.Reconnecting:
                    return LocalizationKeys.OnlineStatusReconnecting;
                case ConnectionBanner.Returned:
                    return LocalizationKeys.OnlineStatusReturned;
                case ConnectionBanner.Left:
                    return LocalizationKeys.OnlineStatusLeft;
                case ConnectionBanner.TimedOut:
                    return LocalizationKeys.OnlineStatusTimedOut;
                default:
                    return null;
            }
        }

        /// <summary>Whole seconds shown for <paramref name="graceLeft"/>: rounded up, never negative.</summary>
        public static int DisplaySeconds(float graceLeft)
        {
            return graceLeft > 0f ? Mathf.CeilToInt(graceLeft) : 0;
        }

        /// <summary>Whether the seat is still reserved after <paramref name="status"/> (the next Connected is a return).</summary>
        public static bool IsAwaiting(PlayerConnectionStatus status)
        {
            return status == PlayerConnectionStatus.Reconnecting;
        }

        /// <summary>
        /// Makes <paramref name="text"/> safe to embed in a TextMeshPro <c>SetText(format, value)</c> template: braces
        /// would be read as placeholders, so they become parentheses. Returns the same instance when nothing changes.
        /// </summary>
        public static string EscapeForTemplate(string text)
        {
            if (string.IsNullOrEmpty(text) || (text.IndexOf('{') < 0 && text.IndexOf('}') < 0))
            {
                return text ?? string.Empty;
            }

            return text.Replace('{', '(').Replace('}', ')');
        }
    }
}
