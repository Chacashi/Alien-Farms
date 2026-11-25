using AlienFarms.Core;
using NUnit.Framework;

namespace AlienFarms.Tests
{
    public sealed class VoiceAttenuationModelTests
    {
        [TestCase(1f, 1f)]
        [TestCase(5f, 0.2f)]
        [TestCase(15f, 0.0667f)]
        [TestCase(30f, 0.0333f)]
        [TestCase(35f, 0f)]
        public void Gain_InverseMatchesReferenceTable(float distance, float expected)
        {
            float gain = VoiceAttenuationModel.Gain(distance, 30f, 1f, 1f, VoiceFadeModel.InverseByDistance);
            Assert.That(gain, Is.EqualTo(expected).Within(0.0001f));
        }

        [TestCase(0.2f, -14f)]
        [TestCase(1f / 15f, -23.5f)]
        public void ToDecibels_MatchesReferenceTable(float gain, float expected)
        {
            Assert.That(VoiceAttenuationModel.ToDecibels(gain), Is.EqualTo(expected).Within(0.05f));
        }

        [Test]
        public void ToDecibels_ZeroGainIsSilence()
        {
            Assert.That(VoiceAttenuationModel.ToDecibels(0f), Is.EqualTo(float.NegativeInfinity));
        }

        [TestCase(VoiceFadeModel.LinearByDistance)]
        [TestCase(VoiceFadeModel.ExponentialByDistance)]
        public void Gain_DecreasesWithDistance(VoiceFadeModel model)
        {
            float previous = VoiceAttenuationModel.Gain(1f, 30f, 1f, 1f, model);
            Assert.That(previous, Is.EqualTo(1f));
            for (int distance = 2; distance <= 30; distance++)
            {
                float current = VoiceAttenuationModel.Gain(distance, 30f, 1f, 1f, model);
                Assert.That(current, Is.InRange(0f, previous));
                previous = current;
            }

            Assert.That(previous, Is.LessThan(1f));
            Assert.That(VoiceAttenuationModel.Gain(35f, 30f, 1f, 1f, model), Is.Zero);
        }
    }
}
