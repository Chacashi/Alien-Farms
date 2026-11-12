using System;

namespace AlienFarms.Networking.Diagnostics
{
    /// <summary>
    /// Machine-local visibility switch of one debug view: without an override the view follows its default (a
    /// <c>NetworkSettings</c> flag); the debug overlay (F1–F4, task 3.3) sets or flips the override and listeners
    /// re-read <see cref="IsVisible"/> on <see cref="Changed"/>. Pure C#; the static facades
    /// (<see cref="OwnershipLabelToggle"/>, <see cref="PoolStatsToggle"/>) own one each and reset it on play-mode entry.
    /// </summary>
    public sealed class DebugViewSwitch
    {
        private bool _hasOverride;
        private bool _visible;

        /// <summary>Raised when the override changes; listeners re-read <see cref="IsVisible"/>.</summary>
        public event Action Changed;

        public bool HasOverride => _hasOverride;

        /// <summary>Effective visibility: the override if any, otherwise <paramref name="defaultVisible"/>.</summary>
        public bool IsVisible(bool defaultVisible) => _hasOverride ? _visible : defaultVisible;

        public void SetVisible(bool visible)
        {
            if (_hasOverride && _visible == visible)
            {
                return;
            }

            _hasOverride = true;
            _visible = visible;
            Changed?.Invoke();
        }

        /// <summary>Flips the effective visibility (<paramref name="defaultVisible"/> = the settings' default).</summary>
        public void Toggle(bool defaultVisible)
        {
            SetVisible(!IsVisible(defaultVisible));
        }

        /// <summary>Back to the default.</summary>
        public void ClearOverride()
        {
            if (!_hasOverride)
            {
                return;
            }

            _hasOverride = false;
            Changed?.Invoke();
        }

        /// <summary>Drops the override and every listener (play-mode entry without domain reload).</summary>
        public void Reset()
        {
            _hasOverride = false;
            _visible = false;
            Changed = null;
        }
    }
}
