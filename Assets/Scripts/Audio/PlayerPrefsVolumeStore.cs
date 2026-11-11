using UnityEngine;

namespace AlienFarms.Audio
{
    /// <summary><see cref="IVolumeStore"/> backed by <see cref="PlayerPrefs"/> (bools stored as 0/1 ints).</summary>
    public sealed class PlayerPrefsVolumeStore : IVolumeStore
    {
        public float GetFloat(string key, float defaultValue) => PlayerPrefs.GetFloat(key, defaultValue);

        public void SetFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);

        public bool GetBool(string key, bool defaultValue) => PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) != 0;

        public void SetBool(string key, bool value) => PlayerPrefs.SetInt(key, value ? 1 : 0);

        public void Save() => PlayerPrefs.Save();
    }
}
