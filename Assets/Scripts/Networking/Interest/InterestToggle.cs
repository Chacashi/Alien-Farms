using System;
using AlienFarms.Networking.Diagnostics;
using UnityEngine;

namespace AlienFarms.Networking.Interest
{
    public static class InterestToggle
    {
        private static readonly DebugViewSwitch s_switch = new DebugViewSwitch();

        public static event Action Changed
        {
            add => s_switch.Changed += value;
            remove => s_switch.Changed -= value;
        }

        public static bool IsEnabled(bool defaultEnabled) => s_switch.IsVisible(defaultEnabled);
        public static void Set(bool enabled) => s_switch.SetVisible(enabled);
        public static void Toggle(bool defaultEnabled) => s_switch.Toggle(defaultEnabled);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_switch.Reset();
    }
}
