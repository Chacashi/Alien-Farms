using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace AlienFarms.UI
{
    /// <summary>
    /// Options row that cycles the game language. Left/Right (stick, arrows) and Submit change the locale; Up/Down
    /// navigation stays with the Selectable (Explicit, left/right empty). Arrow buttons call <see cref="Previous"/> /
    /// <see cref="Next"/> from their onClick. The choice is global and persisted by the PlayerPrefs startup selector.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Selectable))]
    public sealed class LanguageSelector : MonoBehaviour, IMoveHandler, ISubmitHandler
    {
        [Header("References")]
        [SerializeField, Tooltip("Shows the selected language name (from the Locale).")]
        private TMP_Text _valueLabel;

        private Selectable _selectable;
        private Action<Locale> _localeChanged;

        private void Awake()
        {
            TryGetComponent(out _selectable);
            _localeChanged = HandleLocaleChanged;
        }

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += _localeChanged;
            if (LocalizationSettings.InitializationOperation.IsDone)
            {
                RefreshLabel(LocalizationSettings.SelectedLocale);
            }
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= _localeChanged;
        }

        /// <summary>Selects the next locale (wraps).</summary>
        public void Next() => Step(1);

        /// <summary>Selects the previous locale (wraps).</summary>
        public void Previous() => Step(-1);

        public void OnMove(AxisEventData eventData)
        {
            switch (eventData.moveDir)
            {
                case MoveDirection.Left:
                    Step(-1);
                    eventData.Use();
                    break;
                case MoveDirection.Right:
                    Step(1);
                    eventData.Use();
                    break;
            }
        }

        public void OnSubmit(BaseEventData eventData) => Next();

        private static void Step(int step)
        {
            if (!LocalizationSettings.InitializationOperation.IsDone)
            {
                return;
            }

            List<Locale> locales = LocalizationSettings.AvailableLocales.Locales;
            int current = locales.IndexOf(LocalizationSettings.SelectedLocale);
            int next = LocaleCycle.Next(current, locales.Count, step);
            if (next < 0 || next == current)
            {
                return;
            }

            LocalizationSettings.SelectedLocale = locales[next];
        }

        private void HandleLocaleChanged(Locale locale) => RefreshLabel(locale);

        private void RefreshLabel(Locale locale)
        {
            if (_valueLabel != null && locale != null)
            {
                _valueLabel.SetText(locale.LocaleName);
            }
        }

        private void OnValidate()
        {
            if (_valueLabel == null)
            {
                Debug.LogWarning($"[{nameof(LanguageSelector)}] Missing value label on '{name}'.", this);
            }

            if (_selectable == null)
            {
                TryGetComponent(out _selectable);
            }

            // Left/Right change the language: the Selectable must not navigate horizontally.
            Navigation navigation = _selectable != null ? _selectable.navigation : default;
            if (_selectable != null && (navigation.mode != Navigation.Mode.Explicit
                || navigation.selectOnLeft != null || navigation.selectOnRight != null))
            {
                Debug.LogWarning($"[{nameof(LanguageSelector)}] Selectable on '{name}' must use Explicit navigation with Left/Right empty.", this);
            }
        }
    }
}
