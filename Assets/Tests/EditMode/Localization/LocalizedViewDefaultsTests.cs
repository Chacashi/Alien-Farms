using System;
using System.Reflection;
using AlienFarms.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Localization;

namespace AlienFarms.Tests.Localization
{
    /// <summary>The default LocalizedString of every localized view field is set and resolves in the tables.</summary>
    public sealed class LocalizedViewDefaultsTests
    {
        private static readonly Type[] Views =
        {
            typeof(PauseMenuView),
            typeof(MatchResultsView),
            typeof(CountdownView),
            typeof(PlayerSlotStatusView),
            typeof(CharacterCardView),
            typeof(OnlinePanelView),
            typeof(TitleStatsView)
        };

        [TestCaseSource(nameof(Views))]
        public void LocalizedStringDefaults_AreSetAndResolve(Type viewType)
        {
            var host = new GameObject("LocalizedViewDefaultsTests") { hideFlags = HideFlags.HideAndDontSave };
            LogType previousFilter = Debug.unityLogger.filterLogType;
            try
            {
                // The bare host has no references: silence the views' OnValidate warnings (errors still show).
                Debug.unityLogger.filterLogType = LogType.Error;
                Component view = host.AddComponent(viewType);
                Debug.unityLogger.filterLogType = previousFilter;
                int found = 0;
                foreach (FieldInfo field in viewType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    if (field.FieldType != typeof(LocalizedString))
                    {
                        continue;
                    }

                    found++;
                    var localized = (LocalizedString)field.GetValue(view);
                    Assert.That(localized, Is.Not.Null, field.Name);
                    Assert.That(localized.IsEmpty, Is.False, field.Name);
                    string value = LocalizationTestUtility.ResolveSpanish(localized.TableReference, localized.TableEntryReference);
                    Assert.That(value, Is.Not.Null.And.Not.Empty, $"{viewType.Name}.{field.Name} does not resolve.");
                }

                Assert.That(found, Is.GreaterThan(0), $"{viewType.Name} has no LocalizedString fields.");
            }
            finally
            {
                Debug.unityLogger.filterLogType = previousFilter;
                UnityEngine.Object.DestroyImmediate(host);
            }
        }
    }
}
