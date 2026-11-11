using System;
using AlienFarms.Audio;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class AudioMixerParametersTests
    {
        [TestCase(VolumeChannel.Master, "MasterVolume")]
        [TestCase(VolumeChannel.Music, "MusicVolume")]
        [TestCase(VolumeChannel.Sfx, "SfxVolume")]
        public void GetVolumeParameter_MatchesExposedName(VolumeChannel channel, string expected)
        {
            Assert.AreEqual(expected, AudioMixerParameters.GetVolumeParameter(channel));
        }

        [Test]
        public void ChannelCount_MatchesEnum()
        {
            Assert.AreEqual(Enum.GetValues(typeof(VolumeChannel)).Length, AudioMixerParameters.ChannelCount);
        }

        [Test]
        public void Names_AreUnique()
        {
            CollectionAssert.AllItemsAreUnique(new[]
            {
                AudioMixerParameters.MasterVolume,
                AudioMixerParameters.MusicVolume,
                AudioMixerParameters.SfxVolume
            });
        }

        [Test]
        public void InvalidChannel_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => AudioMixerParameters.GetVolumeParameter((VolumeChannel)7));
        }
    }
}
