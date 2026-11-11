using System.Collections.Generic;
using AlienFarms.UI;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace AlienFarms.Tests.Localization
{
    /// <summary>Shared lookups over the project's string table collections.</summary>
    internal static class LocalizationTestUtility
    {
        public static readonly string[] TableNames = { LocalizationKeys.UiTable, LocalizationKeys.HowToPlayTable };
        public static readonly LocaleIdentifier Spanish = new LocaleIdentifier(LocalizationKeys.SpanishCode);
        public static readonly LocaleIdentifier English = new LocaleIdentifier(LocalizationKeys.EnglishCode);

        public static StringTableCollection GetCollection(TableReference table) =>
            LocalizationEditorSettings.GetStringTableCollection(table);

        public static StringTable GetTable(string tableName, LocaleIdentifier locale) =>
            GetCollection(tableName)?.GetTable(locale) as StringTable;

        /// <summary>All (table, key) pairs of both collections.</summary>
        public static IEnumerable<(string table, string key, long id)> AllKeys()
        {
            foreach (string tableName in TableNames)
            {
                StringTableCollection collection = GetCollection(tableName);
                if (collection == null)
                {
                    continue;
                }

                foreach (SharedTableData.SharedTableEntry entry in collection.SharedData.Entries)
                {
                    yield return (tableName, entry.Key, entry.Id);
                }
            }
        }

        /// <summary>Spanish value referenced by table + entry (name or id), or null if it does not resolve.</summary>
        public static string ResolveSpanish(TableReference table, TableEntryReference entry)
        {
            StringTableCollection collection = GetCollection(table);
            if (collection == null || !(collection.GetTable(Spanish) is StringTable spanish))
            {
                return null;
            }

            SharedTableData.SharedTableEntry shared = entry.ReferenceType == TableEntryReference.Type.Id
                ? collection.SharedData.GetEntry(entry.KeyId)
                : collection.SharedData.GetEntry(entry.Key);
            return shared == null ? null : spanish.GetEntry(shared.Id)?.Value;
        }
    }
}
