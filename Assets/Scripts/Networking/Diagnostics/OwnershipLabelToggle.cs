using System;
using UnityEngine;

namespace AlienFarms.Networking.Diagnostics
{
    /// <summary>
    /// Machine-local switch of the ownership debug labels (not replicated, no PlayerId: it is a debug view of this
    /// screen). Without an override the labels follow <c>NetworkSettings.ShowOwnershipLabels</c>; the debug overlay
    /// (F1–F4, task 3.3) calls <see cref="Toggle"/> / <see cref="SetVisible"/> and every label refreshes through
    /// <see cref="Changed"/>. Reset on play-mode entry (no domain reload).
    /// </summary>
    public static class OwnershipLabelToggle
    {
        private static bool s_hasOverride;
        private static bool s_visible;

        /// <summary>Raised when the override changes; listeners re-read <see cref="IsVisible"/>.</summary>
        public static event Action Changed;

        public static bool HasOverride => s_hasOverride;

        /// <summary>Effective visibility: the override if any, otherwise <paramref name="defaultVisible"/> (from the settings).</summary>
        public static bool IsVisible(bool defaultVisible) => s_hasOverride ? s_visible : defaultVisible;

        public static void SetVisible(bool visible)
        {
            if (s_hasOverride && s_visible == visible)
            {
                return;
            }

            s_hasOverride = true;
            s_visible = visible;
            Changed?.Invoke();
        }

        /// <summary>Flips the effective visibility (<paramref name="defaultVisible"/> = the settings' default).</summary>
        public static void Toggle(bool defaultVisible)
        {
            SetVisible(!IsVisible(defaultVisible));
        }

        /// <summary>Back to the settings' default.</summary>
        public static void ClearOverride()
        {
            if (!s_hasOverride)
            {
                return;
            }

            s_hasOverride = false;
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_hasOverride = false;
            s_visible = false;
            Changed = null;
        }
    }
}
