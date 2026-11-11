namespace AlienFarms.Audio
{
    /// <summary>Key/value persistence for volume preferences (PlayerPrefs at runtime, a fake in tests).</summary>
    public interface IVolumeStore
    {
        float GetFloat(string key, float defaultValue);

        void SetFloat(string key, float value);

        bool GetBool(string key, bool defaultValue);

        void SetBool(string key, bool value);

        /// <summary>Flushes pending writes to disk.</summary>
        void Save();
    }
}
