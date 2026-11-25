using AlienFarms.Core;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class VoiceVolumeRulesTests
    {
        [TestCase(-1f, -50)]
        [TestCase(0f, -50)]
        [TestCase(0.5f, 0)]
        [TestCase(1f, 50)]
        [TestCase(2f, 50)]
        public void ToVivox_ClampsAndMapsSlider(float slider, int expected)
        {
            Assert.That(VoiceVolumeRules.ToVivox(slider), Is.EqualTo(expected));
        }

        [TestCase(-100, -50, 0f)]
        [TestCase(0, 0, 0.5f)]
        [TestCase(100, 50, 1f)]
        public void ClampAndToSlider_RespectVolumeLimits(int volume, int clamped, float slider)
        {
            Assert.That(VoiceVolumeRules.Clamp(volume), Is.EqualTo(clamped));
            Assert.That(VoiceVolumeRules.ToSlider(volume), Is.EqualTo(slider));
        }

        [Test]
        public void Conversion_RoundTripsEverySdkVolume()
        {
            for (int volume = -50; volume <= 50; volume++)
            {
                Assert.That(VoiceVolumeRules.ToVivox(VoiceVolumeRules.ToSlider(volume)), Is.EqualTo(volume));
            }
        }
    }
}
