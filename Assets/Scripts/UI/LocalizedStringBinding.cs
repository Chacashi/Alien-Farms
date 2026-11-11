using System;
using UnityEngine.Localization;

namespace AlienFarms.UI
{
    /// <summary>
    /// Keeps the resolved value of a <see cref="LocalizedString"/> and notifies when it changes (load or locale change).
    /// <see cref="Value"/> stays null until the string resolves: callers must then leave the scene text untouched.
    /// Call <see cref="Enable"/> from OnEnable and <see cref="Disable"/> from OnDisable.
    /// </summary>
    public sealed class LocalizedStringBinding
    {
        private readonly LocalizedString _string;
        private readonly Action _onChanged;
        private readonly LocalizedString.ChangeHandler _handler;
        private bool _enabled;

        public LocalizedStringBinding(LocalizedString localizedString, Action onChanged)
        {
            _string = localizedString;
            _onChanged = onChanged;
            _handler = HandleStringChanged;
        }

        /// <summary>Resolved text for the selected locale, or null while unresolved / empty reference.</summary>
        public string Value { get; private set; }

        public void Enable()
        {
            if (_enabled || _string == null || _string.IsEmpty)
            {
                return;
            }

            _enabled = true;
            // May invoke the handler synchronously (preloaded tables + synchronous initialization).
            _string.StringChanged += _handler;
        }

        public void Disable()
        {
            if (!_enabled)
            {
                return;
            }

            _enabled = false;
            _string.StringChanged -= _handler;
        }

        private void HandleStringChanged(string value)
        {
            Value = value;
            _onChanged?.Invoke();
        }
    }
}
