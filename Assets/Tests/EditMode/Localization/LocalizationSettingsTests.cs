using System.Collections.Generic;
using AlienFarms.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization.Settings;

namespace AlienFarms.Tests.Localization
{
    public sealed class LocalizationSettingsTests
    {
        private static LocalizationSettings Settings
        {
            get
            {
                LocalizationSettings settings = LocalizationEditorSettings.ActiveLocalizationSettings;
                Assert.That(settings, Is.Not.Null, "No active LocalizationSettings: run Tools/Alien Farms/Localization/Setup.");
                return settings;
            }
        }

        [Test]
        public void Locales_AreSpanishAndEnglish_WithSpanishAsProjectLocale()
        {
            Assert.That(LocalizationEditorSettings.GetLocale(LocalizationTestUtility.Spanish), Is.Not.Null);
            Assert.That(LocalizationEditorSettings.GetLocale(LocalizationTestUtility.English), Is.Not.Null);

            var serialized = new SerializedObject(Settings);
            Assert.That(serialized.FindProperty("m_ProjectLocaleIdentifier.m_Code").stringValue, Is.EqualTo(LocalizationKeys.SpanishCode));
        }

        [Test]
        public void Settings_InitializeSynchronously()
        {
            var serialized = new SerializedObject(Settings);
            Assert.That(serialized.FindProperty("m_InitializeSynchronously").boolValue, Is.True);
        }

        [Test]
        public void StartupSelectors_ArePlayerPrefsThenSpecificSpanish_WithoutSystem()
        {
            List<IStartupLocaleSelector> selectors = Settings.GetStartupLocaleSelectors();

            Assert.That(selectors.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(selectors[0], Is.TypeOf<PlayerPrefLocaleSelector>());
            Assert.That(((PlayerPrefLocaleSelector)selectors[0]).PlayerPreferenceKey, Is.EqualTo(LocalizationKeys.LocalePlayerPrefsKey));
            Assert.That(selectors[1], Is.TypeOf<SpecificLocaleSelector>());
            Assert.That(((SpecificLocaleSelector)selectors[1]).LocaleId.Code, Is.EqualTo(LocalizationKeys.SpanishCode));
            foreach (IStartupLocaleSelector selector in selectors)
            {
                Assert.That(selector, Is.Not.TypeOf<SystemLocaleSelector>());
            }
        }

        [Test]
        public void Tables_ArePreloaded()
        {
            foreach (string tableName in LocalizationTestUtility.TableNames)
            {
                Assert.That(LocalizationTestUtility.GetCollection(tableName), Is.Not.Null, tableName);
                Assert.That(LocalizationEditorSettings.GetPreloadTableFlag(LocalizationTestUtility.GetTable(tableName, LocalizationTestUtility.Spanish)), Is.True, $"{tableName} es");
                Assert.That(LocalizationEditorSettings.GetPreloadTableFlag(LocalizationTestUtility.GetTable(tableName, LocalizationTestUtility.English)), Is.True, $"{tableName} en");
            }
        }
    }
}
