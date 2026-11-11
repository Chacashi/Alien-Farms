namespace AlienFarms.Editor
{
    /// <summary>One string table entry seeded by <see cref="LocalizationSetupMenu"/>.</summary>
    public readonly struct LocalizationSeedEntry
    {
        public LocalizationSeedEntry(string table, string key, string spanish, string english)
        {
            Table = table;
            Key = key;
            Spanish = spanish;
            English = english;
        }

        public string Table { get; }
        public string Key { get; }
        public string Spanish { get; }
        public string English { get; }
    }
}
