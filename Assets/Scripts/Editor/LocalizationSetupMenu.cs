using System.Collections.Generic;
using System.Text;
using AlienFarms.UI;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;

namespace AlienFarms.Editor
{
    /// <summary>
    /// Idempotent localization setup: creates whatever is missing (settings, es/en locales, UI/HowToPlay tables and
    /// their entries) and enforces the startup configuration. Never overwrites existing translations.
    /// Also offers a helper that adds <see cref="LocalizedText"/> to selected TMP texts by matching their Spanish text.
    /// </summary>
    public static class LocalizationSetupMenu
    {
        private const string MenuRoot = "Tools/Alien Farms/Localization/";
        private const string RootFolder = "Assets/Settings/Localization";
        private const string LocalesFolder = RootFolder + "/Locales";
        private const string TablesFolder = RootFolder + "/Tables";
        private const string SettingsPath = RootFolder + "/LocalizationSettings.asset";

        private const string StringFieldPath = "_string";
        private const string TableNamePath = "m_TableReference.m_TableCollectionName";
        private const string KeyIdPath = "m_TableEntryReference.m_KeyId";
        private const string KeyPath = "m_TableEntryReference.m_Key";

        private static readonly string[] TableNames = { LocalizationKeys.UiTable, LocalizationKeys.HowToPlayTable };

        [MenuItem(MenuRoot + "Setup")]
        public static void Setup()
        {
            EnsureFolder(LocalesFolder);
            EnsureFolder(TablesFolder);

            LocalizationSettings settings = EnsureSettings();
            Locale spanish = EnsureLocale(LocalizationKeys.SpanishCode, "Español", 0, "Spanish (es)");
            Locale english = EnsureLocale(LocalizationKeys.EnglishCode, "English", 1, "English (en)");
            ConfigureSettings(settings);

            var locales = new List<Locale> { spanish, english };
            int added = 0;
            foreach (string tableName in TableNames)
            {
                StringTableCollection collection = EnsureCollection(tableName, locales);
                added += SeedCollection(collection, tableName, spanish.Identifier, english.Identifier);
            }

            EnableAddressablesPlayerBuild();
            AssetDatabase.SaveAssets();
            Debug.Log($"[{nameof(LocalizationSetupMenu)}] Localization setup done ({added} entries added).");
        }

        [MenuItem(MenuRoot + "Bind Selected (by ES text match)")]
        public static void BindSelected()
        {
            Dictionary<string, (string table, string key, long keyId)> spanishIndex = BuildSpanishIndex(out List<string> ambiguous);
            if (spanishIndex == null)
            {
                Debug.LogError($"[{nameof(LocalizationSetupMenu)}] Run Setup first: string tables not found.");
                return;
            }

            HashSet<TMP_Text> viewDriven = CollectViewDrivenTexts(Selection.gameObjects);
            var notFound = new StringBuilder();
            int bound = 0;
            int skipped = 0;

            foreach (GameObject selected in Selection.gameObjects)
            {
                foreach (TMP_Text text in selected.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text.TryGetComponent(out LocalizedText _))
                    {
                        continue;
                    }

                    if (viewDriven.Contains(text))
                    {
                        skipped++;
                        Debug.LogWarning($"[{nameof(LocalizationSetupMenu)}] Skipped '{GetPath(text.transform)}': written by a view.", text);
                        continue;
                    }

                    string current = Normalize(text.text);
                    if (!spanishIndex.TryGetValue(current, out (string table, string key, long keyId) match))
                    {
                        notFound.Append("\n  ").Append(GetPath(text.transform)).Append(" = \"").Append(current).Append('"');
                        continue;
                    }

                    if (ambiguous.Contains(current))
                    {
                        Debug.LogWarning($"[{nameof(LocalizationSetupMenu)}] '{GetPath(text.transform)}' text \"{current}\" matches several keys; bound to {match.table}/{match.key}. Change it if needed.", text);
                    }

                    LocalizedText localized = Undo.AddComponent<LocalizedText>(text.gameObject);
                    SetLocalizedString(localized, match.table, match.key);
                    bound++;
                }
            }

            Debug.Log($"[{nameof(LocalizationSetupMenu)}] Bound {bound} text(s), skipped {skipped} view-driven.");
            if (notFound.Length > 0)
            {
                Debug.LogWarning($"[{nameof(LocalizationSetupMenu)}] No ES match for:{notFound}");
            }
        }

        [MenuItem(MenuRoot + "Bind Selected (by ES text match)", true)]
        private static bool ValidateBindSelected() => Selection.gameObjects.Length > 0;

        /// <summary>Points a LocalizedText at table/key through serialized properties (Undo + prefab overrides aware).</summary>
        public static void SetLocalizedString(LocalizedText target, string table, string key)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty localized = serialized.FindProperty(StringFieldPath);
            localized.FindPropertyRelative(TableNamePath).stringValue = table;
            localized.FindPropertyRelative(KeyIdPath).longValue = 0;
            localized.FindPropertyRelative(KeyPath).stringValue = key;
            serialized.ApplyModifiedProperties();
        }

        private static LocalizationSettings EnsureSettings()
        {
            LocalizationSettings settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (settings == null)
            {
                settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(SettingsPath);
                if (settings == null)
                {
                    settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                    settings.name = "LocalizationSettings";
                    AssetDatabase.CreateAsset(settings, SettingsPath);
                }

                LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            }

            LocalizationSettings.Instance = settings;
            return settings;
        }

        private static Locale EnsureLocale(string code, string displayName, ushort sortOrder, string fileName)
        {
            Locale locale = LocalizationEditorSettings.GetLocale(new LocaleIdentifier(code));
            if (locale != null)
            {
                return locale;
            }

            string path = $"{LocalesFolder}/{fileName}.asset";
            locale = AssetDatabase.LoadAssetAtPath<Locale>(path);
            if (locale == null)
            {
                locale = Locale.CreateLocale(new LocaleIdentifier(code));
                locale.LocaleName = displayName;
                locale.SortOrder = sortOrder;
                AssetDatabase.CreateAsset(locale, path);
            }

            LocalizationEditorSettings.AddLocale(locale);
            return locale;
        }

        private static void ConfigureSettings(LocalizationSettings settings)
        {
            var serialized = new SerializedObject(settings);
            serialized.FindProperty("m_ProjectLocaleIdentifier.m_Code").stringValue = LocalizationKeys.SpanishCode;
            serialized.FindProperty("m_InitializeSynchronously").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            List<IStartupLocaleSelector> selectors = settings.GetStartupLocaleSelectors();
            bool alreadyConfigured = selectors.Count == 2
                && selectors[0] is PlayerPrefLocaleSelector prefs && prefs.PlayerPreferenceKey == LocalizationKeys.LocalePlayerPrefsKey
                && selectors[1] is SpecificLocaleSelector specific && specific.LocaleId.Code == LocalizationKeys.SpanishCode;
            if (!alreadyConfigured)
            {
                selectors.Clear();
                selectors.Add(new PlayerPrefLocaleSelector { PlayerPreferenceKey = LocalizationKeys.LocalePlayerPrefsKey });
                selectors.Add(new SpecificLocaleSelector { LocaleId = new LocaleIdentifier(LocalizationKeys.SpanishCode) });
            }

            EditorUtility.SetDirty(settings);
        }

        private static StringTableCollection EnsureCollection(string tableName, List<Locale> locales)
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
            if (collection == null)
            {
                string folder = $"{TablesFolder}/{tableName}";
                EnsureFolder(folder);
                collection = LocalizationEditorSettings.CreateStringTableCollection(tableName, folder, locales);
            }

            foreach (Locale locale in locales)
            {
                if (!collection.ContainsTable(locale.Identifier))
                {
                    collection.AddNewTable(locale.Identifier);
                }
            }

            collection.SetPreloadTableFlag(true);
            return collection;
        }

        private static int SeedCollection(StringTableCollection collection, string tableName, LocaleIdentifier spanish, LocaleIdentifier english)
        {
            var spanishTable = (StringTable)collection.GetTable(spanish);
            var englishTable = (StringTable)collection.GetTable(english);
            int added = 0;

            foreach (LocalizationSeedEntry entry in LocalizationSeedData.Entries)
            {
                if (entry.Table != tableName)
                {
                    continue;
                }

                added += AddIfMissing(spanishTable, entry.Key, entry.Spanish);
                added += AddIfMissing(englishTable, entry.Key, entry.English);
            }

            EditorUtility.SetDirty(collection.SharedData);
            EditorUtility.SetDirty(spanishTable);
            EditorUtility.SetDirty(englishTable);
            return added;
        }

        private static int AddIfMissing(StringTable table, string key, string value)
        {
            StringTableEntry existing = table.GetEntry(key);
            if (existing != null && !string.IsNullOrEmpty(existing.Value))
            {
                return 0;
            }

            table.AddEntry(key, value);
            return 1;
        }

        private static void EnableAddressablesPlayerBuild()
        {
            AddressableAssetSettings addressables = AddressableAssetSettingsDefaultObject.Settings;
            if (addressables != null && addressables.BuildAddressablesWithPlayerBuild != AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer)
            {
                addressables.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;
                EditorUtility.SetDirty(addressables);
            }
        }

        private static Dictionary<string, (string table, string key, long keyId)> BuildSpanishIndex(out List<string> ambiguous)
        {
            ambiguous = new List<string>();
            var index = new Dictionary<string, (string, string, long)>();
            var spanish = new LocaleIdentifier(LocalizationKeys.SpanishCode);

            foreach (string tableName in TableNames)
            {
                StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
                if (collection == null || !(collection.GetTable(spanish) is StringTable table))
                {
                    return null;
                }

                foreach (SharedTableData.SharedTableEntry shared in collection.SharedData.Entries)
                {
                    StringTableEntry entry = table.GetEntry(shared.Id);
                    if (entry == null || string.IsNullOrEmpty(entry.Value))
                    {
                        continue;
                    }

                    string value = Normalize(entry.Value);
                    if (index.ContainsKey(value))
                    {
                        ambiguous.Add(value);
                        continue;
                    }

                    index.Add(value, (tableName, shared.Key, shared.Id));
                }
            }

            return index;
        }

        // TMP texts written by views. Scene objects: scan the whole scene (views may live on other roots).
        // Prefab assets / LoadPrefabContents (no valid scene): scan the selection's prefab root.
        private static HashSet<TMP_Text> CollectViewDrivenTexts(GameObject[] selection)
        {
            var result = new HashSet<TMP_Text>();
            var visitedScenes = new HashSet<Scene>();
            var visitedRoots = new HashSet<Transform>();
            foreach (GameObject selected in selection)
            {
                Scene scene = selected.scene;
                if (!scene.IsValid())
                {
                    Transform root = selected.transform.root;
                    if (visitedRoots.Add(root))
                    {
                        ViewDrivenTextCollector.Collect(root.gameObject, result);
                    }

                    continue;
                }

                if (!visitedScenes.Add(scene))
                {
                    continue;
                }

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    ViewDrivenTextCollector.Collect(root, result);
                }
            }

            return result;
        }

        private static string Normalize(string value) => value.Replace("\r\n", "\n").Trim();

        private static string GetPath(Transform transform)
        {
            var builder = new StringBuilder(transform.name);
            for (Transform parent = transform.parent; parent != null; parent = parent.parent)
            {
                builder.Insert(0, '/').Insert(0, parent.name);
            }

            return builder.ToString();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }
    }
}
