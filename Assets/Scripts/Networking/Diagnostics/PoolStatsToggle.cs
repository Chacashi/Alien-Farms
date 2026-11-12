using System;
using UnityEngine;

namespace AlienFarms.Networking.Diagnostics
{
    /// <summary>
    /// Machine-local switch of the pool / transport stats view (<see cref="PoolStatsView"/>). Without an override it
    /// follows <c>NetworkSettings.ShowPoolStats</c>; the debug overlay (F1–F4, task 3.3) calls <see cref="Toggle"/> /
    /// <see cref="SetVisible"/>. Same pattern as <see cref="OwnershipLabelToggle"/>. Reset on play-mode entry.
    /// </summary>
    public static class PoolStatsToggle
    {
        private static readonly DebugViewSwitch s_switch = new DebugViewSwitch();

        /// <summary>Raised when the override changes; listeners re-read <see cref="IsVisible"/>.</summary>
        public static event Action Changed
        {
            add => s_switch.Changed += value;
            remove => s_switch.Changed -= value;
        }

        public static bool HasOverride => s_switch.HasOverride;

        /// <summary>Effective visibility: the override if any, otherwise <paramref name="defaultVisible"/> (from the settings).</summary>
        public static bool IsVisible(bool defaultVisible) => s_switch.IsVisible(defaultVisible);

        public static void SetVisible(bool visible) => s_switch.SetVisible(visible);

        /// <summary>Flips the effective visibility (<paramref name="defaultVisible"/> = the settings' default).</summary>
        public static void Toggle(bool defaultVisible) => s_switch.Toggle(defaultVisible);

        /// <summary>Back to the settings' default.</summary>
        public static void ClearOverride() => s_switch.ClearOverride();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_switch.Reset();
        }
    }
}
