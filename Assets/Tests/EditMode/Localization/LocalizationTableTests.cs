using System.Reflection;
using AlienFarms.UI;
using NUnit.Framework;
using UnityEngine.Localization.Tables;

namespace AlienFarms.Tests.Localization
{
    public sealed class LocalizationTableTests
    {
        private static readonly string[] ParityTokens = { "{0}", "{1}", "<color", "</color>", "\n" };

        [Test]
        public void EveryKey_HasNonEmptySpanishAndEnglishValue()
        {
            int count = 0;
            foreach ((string table, string key, long id) in LocalizationTestUtility.AllKeys())
            {
                count++;
                Assert.That(LocalizationTestUtility.GetTable(table, LocalizationTestUtility.Spanish).GetEntry(id)?.Value, Is.Not.Null.And.Not.Empty, $"{table}/{key} es");
                Assert.That(LocalizationTestUtility.GetTable(table, LocalizationTestUtility.English).GetEntry(id)?.Value, Is.Not.Null.And.Not.Empty, $"{table}/{key} en");
            }

            Assert.That(count, Is.GreaterThan(0), "No keys found: run the localization setup.");
        }

        [Test]
        public void SpanishAndEnglish_KeepSamePlaceholdersTagsAndLineBreaks()
        {
            foreach ((string table, string key, long id) in LocalizationTestUtility.AllKeys())
            {
                string spanish = LocalizationTestUtility.GetTable(table, LocalizationTestUtility.Spanish).GetEntry(id)?.Value ?? string.Empty;
                string english = LocalizationTestUtility.GetTable(table, LocalizationTestUtility.English).GetEntry(id)?.Value ?? string.Empty;
                foreach (string token in ParityTokens)
                {
                    Assert.That(Count(english, token), Is.EqualTo(Count(spanish, token)), $"{table}/{key}: '{token.Replace("\n", "\\n")}' count differs.");
                }
            }
        }

        [Test]
        public void EveryLocalizationKeysConstant_ExistsInItsTable()
        {
            foreach (FieldInfo field in typeof(LocalizationKeys).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (!field.IsLiteral || field.FieldType != typeof(string))
                {
                    continue;
                }

                string value = (string)field.GetRawConstantValue();
                if (!value.Contains("."))
                {
                    // Table names and locale codes; the PlayerPrefs key has a dot but is filtered below.
                    continue;
                }

                if (value == LocalizationKeys.LocalePlayerPrefsKey)
                {
                    continue;
                }

                string table = value.StartsWith("howto.") ? LocalizationKeys.HowToPlayTable : LocalizationKeys.UiTable;
                Assert.That(LocalizationTestUtility.GetCollection(table).SharedData.GetEntry(value), Is.Not.Null, $"{field.Name} = {value} missing in {table}.");
            }
        }

        [Test]
        public void NoEntry_IsSmart()
        {
            foreach ((string table, string key, long id) in LocalizationTestUtility.AllKeys())
            {
                foreach (var locale in new[] { LocalizationTestUtility.Spanish, LocalizationTestUtility.English })
                {
                    StringTableEntry entry = LocalizationTestUtility.GetTable(table, locale).GetEntry(id);
                    Assert.That(entry == null || !entry.IsSmart, Is.True, $"{table}/{key} ({locale.Code}) is Smart.");
                }
            }
        }

        private static int Count(string text, string token)
        {
            int count = 0;
            for (int index = text.IndexOf(token, System.StringComparison.Ordinal); index >= 0; index = text.IndexOf(token, index + token.Length, System.StringComparison.Ordinal))
            {
                count++;
            }

            return count;
        }
    }
}
