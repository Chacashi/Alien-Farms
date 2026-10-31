namespace AlienFarms.Input
{
    /// <summary>
    /// Plain C# helper for a component that may be enabled at once but must only be disabled later (e.g. an
    /// EventSystem that cannot be disabled from inside its own Update). A later enable request cancels a pending disable.
    /// </summary>
    public sealed class DeferredDisableGate
    {
        public bool IsDisablePending { get; private set; }

        /// <summary>
        /// Records the wanted state. Returns true when the caller must enable the component right now;
        /// a disable is only marked pending, to be applied with <see cref="ConsumePendingDisable"/>.
        /// </summary>
        public bool Request(bool active, bool isCurrentlyEnabled)
        {
            if (active)
            {
                IsDisablePending = false;
                return !isCurrentlyEnabled;
            }

            IsDisablePending = isCurrentlyEnabled;
            return false;
        }

        /// <summary>Returns true (once) when a pending disable must be applied now.</summary>
        public bool ConsumePendingDisable()
        {
            bool pending = IsDisablePending;
            IsDisablePending = false;
            return pending;
        }
    }
}
