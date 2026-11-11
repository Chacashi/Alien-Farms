using System.Collections.Generic;
using System.IO;
using AlienFarms.Editor;
using System.Text.RegularExpressions;
using AlienFarms.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;

namespace AlienFarms.Tests.Localization
{
    /// <summary>
    /// Every existing LocalizedText (prefabs and scenes) must point at an existing entry. Passes when there are none yet.
    /// Scenes are read as YAML so the test never opens or changes the editor's scenes.
    /// </summary>
    public sealed class LocalizedTextReferenceTests
    {
        private static readonly string[] ScenePaths = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Prototype.unity" };

        private static readonly Regex TableNameRegex = new Regex(@"m_TableCollectionName: *(.*)");
        private static readonly Regex KeyIdRegex = new Regex(@"m_KeyId: *(-?\d+)");
        private static readonly Regex KeyRegex = new Regex(@"m_Key: *(.*)");
        private static readonly Regex OverrideRegex = new Regex(
            @"propertyPath: _string\.m_TableEntryReference\.(m_Key|m_KeyId)\s*\n\s*value: *(.*)");

        [Test]
        public void LocalizedTextsInPrefabs_ResolveToExistingEntries()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (LocalizedText text in prefab.GetComponentsInChildren<LocalizedText>(true))
                {
                    Assert.That(text.String.IsEmpty, Is.False, $"{path}/{text.name}: empty reference.");
                    string value = LocalizationTestUtility.ResolveSpanish(text.String.TableReference, text.String.TableEntryReference);
                    Assert.That(value, Is.Not.Null.And.Not.Empty, $"{path}/{text.name}: unresolved entry.");
                }
            }
        }

        [Test]
        public void LocalizedTextsInScenes_ResolveToExistingEntries()
        {
            string scriptGuid = GetLocalizedTextScriptGuid();
            Assert.That(scriptGuid, Is.Not.Empty);

            foreach (string scenePath in ScenePaths)
            {
                string yaml = File.ReadAllText(scenePath);

                // Components added in the scene.
                foreach (string block in yaml.Split(new[] { "\n--- " }, System.StringSplitOptions.None))
                {
                    if (!block.Contains($"guid: {scriptGuid}") || !block.Contains("m_TableEntryReference"))
                    {
                        continue;
                    }

                    string table = TableNameRegex.Match(block).Groups[1].Value.Trim();
                    long keyId = long.Parse(KeyIdRegex.Match(block).Groups[1].Value);
                    string key = KeyRegex.Match(block).Groups[1].Value.Trim();
                    TableEntryReference entry = keyId != 0 ? keyId : (TableEntryReference)key;
                    string value = string.IsNullOrEmpty(table) ? null : LocalizationTestUtility.ResolveSpanish(table, entry);
                    Assert.That(value, Is.Not.Null.And.Not.Empty, $"{scenePath}: LocalizedText '{table}/{key}/{keyId}' does not resolve.");
                }

                // Prefab instance overrides of the entry (key name or id) must exist in some table.
                foreach (System.Text.RegularExpressions.Match match in OverrideRegex.Matches(yaml))
                {
                    string raw = match.Groups[2].Value.Trim();
                    TableEntryReference entry = match.Groups[1].Value == "m_KeyId" ? long.Parse(raw) : (TableEntryReference)raw;
                    if (entry.ReferenceType == TableEntryReference.Type.Empty)
                    {
                        continue;
                    }

                    bool found = false;
                    foreach (string table in LocalizationTestUtility.TableNames)
                    {
                        found |= !string.IsNullOrEmpty(LocalizationTestUtility.ResolveSpanish(table, entry));
                    }

                    Assert.That(found, Is.True, $"{scenePath}: override entry '{raw}' does not resolve.");
                }
            }
        }

        [Test]
        public void LocalizedTextsInPrefabs_AreNotOnViewDrivenTexts()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var viewDriven = new HashSet<TMP_Text>();
                ViewDrivenTextCollector.Collect(prefab, viewDriven);
                AssertNoLocalizedTextOn(viewDriven, prefab.GetComponentsInChildren<LocalizedText>(true), path);
            }
        }

        [Test]
        public void LocalizedTextsInScenes_AreNotOnViewDrivenTexts()
        {
            foreach (string scenePath in ScenePaths)
            {
                // Preview scene: loaded from disk without touching the editor's open scenes.
                // OnValidate warnings of unrelated components are muted while loading (errors still show).
                LogType previousFilter = Debug.unityLogger.filterLogType;
                Scene scene;
                try
                {
                    Debug.unityLogger.filterLogType = LogType.Error;
                    scene = EditorSceneManager.OpenPreviewScene(scenePath);
                }
                finally
                {
                    Debug.unityLogger.filterLogType = previousFilter;
                }

                try
                {
                    var viewDriven = new HashSet<TMP_Text>();
                    var localized = new List<LocalizedText>();
                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        ViewDrivenTextCollector.Collect(root, viewDriven);
                        localized.AddRange(root.GetComponentsInChildren<LocalizedText>(true));
                    }

                    AssertNoLocalizedTextOn(viewDriven, localized, scenePath);
                }
                finally
                {
                    EditorSceneManager.ClosePreviewScene(scene);
                }
            }
        }

        private static void AssertNoLocalizedTextOn(HashSet<TMP_Text> viewDriven, IEnumerable<LocalizedText> localizedTexts, string assetPath)
        {
            foreach (LocalizedText localized in localizedTexts)
            {
                bool clash = localized.TryGetComponent(out TMP_Text text) && viewDriven.Contains(text);
                Assert.That(clash, Is.False, $"{assetPath}: LocalizedText on '{localized.name}' sits on a text written by a view.");
            }
        }

        private static string GetLocalizedTextScriptGuid()
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:MonoScript {nameof(LocalizedText)}"))
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid));
                if (script != null && script.GetClass() == typeof(LocalizedText))
                {
                    return guid;
                }
            }

            return string.Empty;
        }
    }
}
