using AlienFarms.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.Audio;

namespace AlienFarms.Tests
{
    /// <summary>Guards the mixer asset against renamed groups or exposed parameters (they are matched by string).</summary>
    public sealed class AlienFarmsMixerAssetTests
    {
        private const string MixerPath = "Assets/Audio/AlienFarmsMixer.mixer";

        private AudioMixer _mixer;

        [SetUp]
        public void SetUp()
        {
            _mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        }

        [Test]
        public void Mixer_Exists()
        {
            Assert.IsNotNull(_mixer, $"Missing mixer at {MixerPath}.");
        }

        [TestCase("Master")]
        [TestCase("Master/Music")]
        [TestCase("Master/SFX")]
        public void Group_ExistsUnderMaster(string groupPath)
        {
            Assume.That(_mixer, Is.Not.Null);

            AudioMixerGroup[] groups = _mixer.FindMatchingGroups(groupPath);
            Assert.IsNotEmpty(groups, $"Group '{groupPath}' not found.");
        }

        [Test]
        public void ExposedParameters_MatchCodeNames()
        {
            Assume.That(_mixer, Is.Not.Null);

            // Serialized names, independent of the mixer's runtime state.
            var serialized = new SerializedObject(_mixer);
            SerializedProperty exposed = serialized.FindProperty("m_ExposedParameters");
            Assert.IsNotNull(exposed);

            var names = new string[exposed.arraySize];
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = exposed.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
            }

            for (int i = 0; i < AudioMixerParameters.ChannelCount; i++)
            {
                string expected = AudioMixerParameters.GetVolumeParameter((VolumeChannel)i);
                CollectionAssert.Contains(names, expected);
            }
        }

        [Test]
        public void ExposedParameters_AreReadable()
        {
            Assume.That(_mixer, Is.Not.Null);

            for (int i = 0; i < AudioMixerParameters.ChannelCount; i++)
            {
                string parameter = AudioMixerParameters.GetVolumeParameter((VolumeChannel)i);
                Assert.IsTrue(_mixer.GetFloat(parameter, out _), $"'{parameter}' is not exposed.");
            }
        }
    }
}
