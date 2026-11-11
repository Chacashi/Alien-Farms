using System.Collections.Generic;
using AlienFarms.Audio;

namespace AlienFarms.Tests
{
    /// <summary>In-memory <see cref="IVolumeStore"/> that records writes and saves.</summary>
    public sealed class FakeVolumeStore : IVolumeStore
    {
        public Dictionary<string, float> Floats { get; } = new Dictionary<string, float>();
        public Dictionary<string, bool> Bools { get; } = new Dictionary<string, bool>();

        public int SaveCount { get; private set; }

        public float GetFloat(string key, float defaultValue) => Floats.TryGetValue(key, out float value) ? value : defaultValue;

        public void SetFloat(string key, float value) => Floats[key] = value;

        public bool GetBool(string key, bool defaultValue) => Bools.TryGetValue(key, out bool value) ? value : defaultValue;

        public void SetBool(string key, bool value) => Bools[key] = value;

        public void Save() => SaveCount++;
    }
}
